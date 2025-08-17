using System;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiMeshing : MonoBehaviour
{
    [HideInInspector] public GG.Graph activeGraph;     // set from your controller
    [HideInInspector] public GG.NodeLabel[] cellLabels; // computed by mapper

    [Header("Source")]
    public VoronoiLayoutGenerator generator;

    public bool combineIntoSingleMesh = false;

    [Header("Materials")]
    public Material materialA;
    public Material materialB;
    public Material materialC;
    public Material materialStart;
    public Material materialBoss;
    public Material materialCorridor;
    public Material materialDefault;


    [Header("Parent")]
    public Transform cellsParent; // create an empty under your root and assign it

    [Header("Volume")]
    [Tooltip("Height (Y) of each Voronoi cell prism")]
    public float cellHeight = 1f;


    [ContextMenu("Rebuild From Layout")]
    public void Rebuild()
    {
        if (generator == null || generator.Cells == null || generator.Cells.Count == 0)
        {
            Debug.LogWarning("[VoronoiMeshing] No layout. Run VoronoiLayoutGenerator.Generate() first.");
            return;
        }
        if (cellsParent == null)
        {
            var go = new GameObject("VoronoiCells");
            go.transform.SetParent(transform, false);
            cellsParent = go.transform;
        }

        Clear();

        if (combineIntoSingleMesh) BuildCombined(generator.Cells);
        else BuildPerCell(generator.Cells);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        if (cellsParent == null) return;
        for (int i = cellsParent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            DestroyImmediate(cellsParent.GetChild(i).gameObject);
#else
            Destroy(cellsParent.GetChild(i).gameObject);
#endif
        }
        var mf = cellsParent.GetComponent<MeshFilter>();
        var mr = cellsParent.GetComponent<MeshRenderer>();
        if (mf) mf.sharedMesh = null;
        if (mr) mr.sharedMaterial = null;
    }

    // ---------- build helpers ----------
    void BuildCombined(IReadOnlyList<VoronoiLayoutGenerator.Cell> cells)
    {
        var allVerts = new List<Vector3>();
        var allTris = new List<int>();
        int baseIndex = 0;

        foreach (var cell in cells)
        {
            var poly = cell.polygon; if (poly == null || poly.Count < 3) continue;

            for (int i = 0; i < poly.Count; i++)
                allVerts.Add(new Vector3(poly[i].x, 0f, poly[i].y));

            var c = PolygonCentroid(poly);
            int cIdx = allVerts.Count; allVerts.Add(new Vector3(c.x, 0f, c.y));

            for (int i = 0; i < poly.Count; i++)
            {
                int a = baseIndex + i;
                int b = baseIndex + ((i + 1) % poly.Count);
                allTris.Add(cIdx); allTris.Add(b); allTris.Add(a); // keep winding consistent
            }
            baseIndex = allVerts.Count;
        }

        if (allVerts.Count == 0) return;

        var mesh = new Mesh();
        mesh.indexFormat = (allVerts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(allVerts);
        mesh.SetTriangles(allTris, 0, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var go = EnsureChild(cellsParent, "VoronoiCombined", true);
        var mf = go.GetComponent<MeshFilter>();
        var mr = go.GetComponent<MeshRenderer>();
        mf.sharedMesh = mesh;
       // mr.sharedMaterial = cellMaterial;
    }

    // +Z is "up" for our 2D poly (x,z). Positive signed area => CCW.
    static bool IsCCW(List<Vector2> poly)
    {
        double area2 = 0; // twice the signed area
        int n = poly.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var pi = poly[i];
            var pj = poly[j];
            // cross of edges (pj -> pi) with x/z treated as x/y in 2D
            area2 += (pj.x * pi.y) - (pi.x * pj.y);
        }
        return area2 > 0; // CCW if positive
    }

    Mesh BuildExtrudedCellMesh(List<Vector2> poly, float height)
    {
        int n = poly.Count;
        if (n < 3) return null;

        // Keep your convention: if poly is CCW, flip it (so we work with CW here)
        if (IsCCW(poly)) poly.Reverse();

        // ---------- CAP VERTICES (shared) ----------
        var verts = new List<Vector3>(n * 2);
        var uvs = new List<Vector2>(n * 2);

        // planar bounds for cap UVs
        float minX = poly[0].x, maxX = poly[0].x, minZ = poly[0].y, maxZ = poly[0].y;
        for (int i = 1; i < n; i++)
        {
            var p = poly[i];
            if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x;
            if (p.y < minZ) minZ = p.y; if (p.y > maxZ) maxZ = p.y;
        }
        float invW = Mathf.Approximately(maxX - minX, 0f) ? 1f : 1f / (maxX - minX);
        float invH = Mathf.Approximately(maxZ - minZ, 0f) ? 1f : 1f / (maxZ - minZ);

        // bottom loop
        for (int i = 0; i < n; i++)
        {
            var p = poly[i];
            verts.Add(new Vector3(p.x, 0f, p.y));
            uvs.Add(new Vector2((p.x - minX) * invW, (p.y - minZ) * invH));
        }
        // top loop
        for (int i = 0; i < n; i++)
        {
            var p = poly[i];
            verts.Add(new Vector3(p.x, height, p.y));
            uvs.Add(new Vector2((p.x - minX) * invW, (p.y - minZ) * invH));
        }

        var tris = new List<int>(n * 12);

        // ---------- CAPS ----------
        // bottom (faces downward): reverse winding relative to top
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(0); tris.Add(i + 1); tris.Add(i);
        }
        // top (faces upward): CCW fan on top loop (offset = n)
        int top = n;
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(top + 0); tris.Add(top + i); tris.Add(top + i + 1);
        }

        // ---------- SIDES (with duplicated vertices for proper UVs) ----------
        // compute cumulative perimeter (for u tiling)
        var perimLen = new float[n + 1];
        perimLen[0] = 0f;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            perimLen[i + 1] = perimLen[i] + Vector2.Distance(poly[i], poly[j]);
        }
        float totalLen = perimLen[n] <= 0f ? 1f : perimLen[n];

        bool isCCWNow = IsCCW(poly); // after the optional reverse above

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;

            float u0 = perimLen[i] / totalLen;
            float u1 = perimLen[i + 1] / totalLen;

            // side quad vertices (duplicated so sides get clean UVs)
            int b0 = verts.Count; verts.Add(new Vector3(poly[i].x, 0f, poly[i].y)); uvs.Add(new Vector2(u0, 0f));
            int b1 = verts.Count; verts.Add(new Vector3(poly[j].x, 0f, poly[j].y)); uvs.Add(new Vector2(u1, 0f));
            int t0 = verts.Count; verts.Add(new Vector3(poly[i].x, height, poly[i].y)); uvs.Add(new Vector2(u0, 1f));
            int t1 = verts.Count; verts.Add(new Vector3(poly[j].x, height, poly[j].y)); uvs.Add(new Vector2(u1, 1f));

            // outward-facing winding for side quads
            if (isCCWNow)
            {
                // outward for CCW
                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);
            }
            else
            {
                // outward for CW
                tris.Add(b0); tris.Add(t1); tris.Add(t0);
                tris.Add(b0); tris.Add(b1); tris.Add(t1);
            }
        }

        // ---------- build mesh ----------
        var mesh = new Mesh();
        mesh.indexFormat = (verts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0, true);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    Mesh BuildExtrudedCellMeshgg(List<Vector2> poly, float height)
    {
        int n = poly.Count;
        if (n < 3) return null;

        // 1) Ensure CCW for consistent normals
        if (IsCCW(poly)) poly.Reverse();

        // ---------- CAP VERTICES (shared) ----------
        var verts = new List<Vector3>(n * 2);
        var uvs = new List<Vector2>(n * 2);

        // planar bounds for cap UVs
        float minX = poly[0].x, maxX = poly[0].x, minZ = poly[0].y, maxZ = poly[0].y;
        for (int i = 1; i < n; i++) { var p = poly[i]; if (p.x < minX) minX = p.x; if (p.x > maxX) maxX = p.x; if (p.y < minZ) minZ = p.y; if (p.y > maxZ) maxZ = p.y; }
        float invW = Mathf.Approximately(maxX - minX, 0f) ? 1f : 1f / (maxX - minX);
        float invH = Mathf.Approximately(maxZ - minZ, 0f) ? 1f : 1f / (maxZ - minZ);

        // bottom loop
        for (int i = 0; i < n; i++)
        {
            var p = poly[i];
            verts.Add(new Vector3(p.x, 0f, p.y));
            uvs.Add(new Vector2((p.x - minX) * invW, (p.y - minZ) * invH));
        }
        // top loop
        for (int i = 0; i < n; i++)
        {
            var p = poly[i];
            verts.Add(new Vector3(p.x, height, p.y));
            uvs.Add(new Vector2((p.x - minX) * invW, (p.y - minZ) * invH));
        }

        var tris = new List<int>(n * 12);

        // ---------- CAPS ----------
        // bottom (faces downward): reverse winding relative to top
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(0); tris.Add(i + 1); tris.Add(i);
        }
        // top (faces upward): CCW fan on top loop (offset = n)
        int top = n;
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(top + 0); tris.Add(top + i); tris.Add(top + i + 1);
        }

        // ---------- SIDES (with duplicated vertices for proper UVs) ----------
        // compute cumulative perimeter (for u tiling)
        var perimLen = new float[n + 1];
        perimLen[0] = 0f;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            perimLen[i + 1] = perimLen[i] + Vector2.Distance(poly[i], poly[j]);
        }
        float totalLen = perimLen[n] <= 0f ? 1f : perimLen[n];

        // for each edge, create 4 duplicated side-verts with their own UVs
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;

            float u0 = perimLen[i] / totalLen;
            float u1 = perimLen[i + 1] / totalLen;

            // side quad vertices (duplicated)
            int b0 = verts.Count; verts.Add(new Vector3(poly[i].x, 0f, poly[i].y)); uvs.Add(new Vector2(u0, 0f));
            int b1 = verts.Count; verts.Add(new Vector3(poly[j].x, 0f, poly[j].y)); uvs.Add(new Vector2(u1, 0f));
            int t0 = verts.Count; verts.Add(new Vector3(poly[i].x, height, poly[i].y)); uvs.Add(new Vector2(u0, 1f));
            int t1 = verts.Count; verts.Add(new Vector3(poly[j].x, height, poly[j].y)); uvs.Add(new Vector2(u1, 1f));

            // two triangles (outward) using CCW order
            tris.Add(b0); tris.Add(t0); tris.Add(t1);
            tris.Add(b0); tris.Add(t1); tris.Add(b1);
        }

        // ---------- build mesh ----------
        var mesh = new Mesh();
        mesh.indexFormat = (verts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0, true);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }


    Mesh BuildExtrudedCellMesh4(List<Vector2> poly, float height)
    {
        int n = poly.Count;
        if (n < 3) return null;

        // Ensure CCW so top cap uses CCW (upwards normals)
        if (!IsCCW(poly)) poly = new List<Vector2>(poly); // copy only if needed
        if (IsCCW(poly)) poly.Reverse(); // revert to CCW if initial copy didn’t happen
                                          // (simpler: just reverse when !IsCCW)

        // vertices: bottom loop then top loop
        var verts = new List<Vector3>(n * 2 + 2);
        for (int i = 0; i < n; i++) verts.Add(new Vector3(poly[i].x, 0f, poly[i].y));
        for (int i = 0; i < n; i++) verts.Add(new Vector3(poly[i].x, height, poly[i].y));

        // (optional) simple UVs skipped here for brevity

        var tris = new List<int>(n * 12);

        // ----- BOTTOM CAP (faces downward): reverse winding relative to top -----
        // fan around bottom[0]: (0, i+1, i) gives normals down if poly is CCW
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(0); tris.Add(i + 1); tris.Add(i);
        }

        // ----- TOP CAP (faces upward): CCW fan on top loop -----
        int top = n; // first top-vertex index
        for (int i = 1; i < n - 1; i++)
        {
            tris.Add(top + 0); tris.Add(top + i); tris.Add(top + i + 1);
        }

        // ----- SIDES -----
        // For CCW polygon, this gives outward-facing side normals
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            int b0 = i, b1 = j;
            int t0 = top + i, t1 = top + j;

            tris.Add(b0); tris.Add(t0); tris.Add(t1);
            tris.Add(b0); tris.Add(t1); tris.Add(b1);
        }

        var mesh = new Mesh();
        mesh.indexFormat = (verts.Count > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0, true);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    void BuildPerCell(IReadOnlyList<VoronoiLayoutGenerator.Cell> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            var poly = cells[i].polygon;
            if (poly == null || poly.Count < 3) continue;

            var mesh = BuildExtrudedCellMesh(poly, cellHeight);

            var go = new GameObject($"Cell_{i}");
            go.transform.SetParent(cellsParent, false);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();

            var label = (cellLabels != null && i < cellLabels.Length)
                ? cellLabels[i]
                : GG.NodeLabel.None;

            mr.sharedMaterial = GetMaterialForLabel(label);
        }
    }

    Material GetMaterialForLabel(GG.NodeLabel label)
    {
        switch (label)
        {
            case GG.NodeLabel.A:
                return materialA != null ? materialA : materialDefault;
            case GG.NodeLabel.B:
                return materialB != null ? materialB : materialDefault;
            case GG.NodeLabel.C:
                return materialC != null ? materialC : materialDefault;
            case GG.NodeLabel.Start:
                return materialStart != null ? materialStart : materialDefault;
            case GG.NodeLabel.Boss:
                return materialBoss != null ? materialBoss : materialDefault;
            case GG.NodeLabel.Corridor:
                return materialCorridor != null ? materialCorridor : materialDefault;
            default:
                return materialDefault;
        }
    }

    static Vector2 PolygonCentroid(List<Vector2> P)
    {
        double A = 0, cx = 0, cy = 0;
        int n = P.Count;
        for (int i = 0; i < n; i++)
        {
            var p = P[i]; var q = P[(i + 1) % n];
            double cross = p.x * q.y - q.x * p.y;
            A += cross; cx += (p.x + q.x) * cross; cy += (p.y + q.y) * cross;
        }
        A *= 0.5; if (Math.Abs(A) < 1e-12)
            return P.Count == 0 ? Vector2.zero : new Vector2(P[0].x, P[0].y);
        return new Vector2((float)(cx / (6 * A)), (float)(cy / (6 * A)));
    }

    static GameObject EnsureChild(Transform parent, string name, bool needMFMR)
    {
        var t = parent.Find(name);
        GameObject go = t ? t.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        if (needMFMR)
        {
            if (!go.TryGetComponent<MeshFilter>(out _)) go.AddComponent<MeshFilter>();
            if (!go.TryGetComponent<MeshRenderer>(out _)) go.AddComponent<MeshRenderer>();
        }
        return go;
    }
}
