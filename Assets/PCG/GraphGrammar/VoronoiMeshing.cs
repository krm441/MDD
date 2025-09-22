using System;
using System.Collections.Generic;
using UnityEngine;

public class VoronoiMeshing : MonoBehaviour
{
    [HideInInspector] public GG.Graph activeGraph;     
    [HideInInspector] public GG.NodeLabel[] cellLabels; 

    [Header("Source")]
    public VoronoiLayoutGenerator generator;

    [Header("Materials")]
    public Material materialA;
    public Material materialB;
    public Material materialC;
    public Material materialStart;
    public Material materialBoss;
    public Material materialCorridor;
    public Material materialDefault;
    public Material materialShore;
    public Material materialLandDecor;

    [Header("Parent")]
    public Transform cellsParent;

    [Header("Volume")]
    [Tooltip("Height (Y) of each Voronoi cell prism")]
    public float cellHeight = 1f;
    public float hexScale = 4.0f;

    [Header("Post-process Water -> Shore -> Decorative Land")]
    [Min(1)] public int shoreRings = 1;              
    [Min(0)] public int waterRingsBeforeDecor = 5;  

    [Header("Elevation")]
    [Tooltip("How much lower the Shore sits vs islands (positive number)")]
    public float shoreDrop = 0.25f;

    [Tooltip("Offset added to decorative land’s first ring (relative to islandsY)")]
    public float landStartYOffset = 0f;

    [Tooltip("How much to raise each subsequent decorative land ring")]
    public float landRingStep = 1f;


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

        int N = generator.Cells.Count;
        if (cellLabels == null || cellLabels.Length != N)
            cellLabels = new GG.NodeLabel[N];

        ApplyWaterRings(generator.Cells, ref cellLabels, shoreRings, waterRingsBeforeDecor);

       
        BuildPerCell(generator.Cells);

        
        //PostProcessLabels(generator.Cells, ref cellLabels, autoShore, waterRingPadding, fillBackgroundWithLandDecor);


        // Set scale
        cellsParent.localScale = new Vector3( hexScale, 1, hexScale);
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

    // ---------- build helpers ---------- //
    

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

        // ---------- CAP VERTICES (shared) ---------- //
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

        // ---------- CAPS ---------- //
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

        // ---------- SIDES (with duplicated vertices for proper UVs) ---------- //
        // compute cumulative perimeter (for u tiling)
        var perimLen = new float[n + 1];
        perimLen[0] = 0f;
        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            perimLen[i + 1] = perimLen[i] + Vector2.Distance(poly[i], poly[j]);
        }
        float totalLen = perimLen[n] <= 0f ? 1f : perimLen[n];

        bool isCCWNow = IsCCW(poly);

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

        // ---------- build mesh ---------- //
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

            // elevations
            var p = go.transform.localPosition;          
            p.y = GetBaseYForCell(i, label);
            go.transform.localPosition = p;

            go.AddComponent<MeshCollider>();

            // walkable
            if (label != GG.NodeLabel.None && label != GG.NodeLabel.LandDecor)
                NavMeshManager.AddFloorToNavMeshLayer(go);
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

            case GG.NodeLabel.Shore:
                return materialShore != null ? materialShore : materialDefault;
            case GG.NodeLabel.LandDecor:
                return materialLandDecor != null ? materialLandDecor : materialDefault;

            default:
                return materialDefault;
        }
    }


    // ================ post processing =============== //
    [Header("Post-Process Labels")]
    [Tooltip("Paint a 1-cell ring of 'Shore' around islands")]
    public bool autoShore = true;

    [Tooltip("Keep this many water rings before turning remaining water into non-walkable decorative land")]
    [Min(0)] public int waterRingPadding = 2;

    [Tooltip("Fill water beyond 'waterRingPadding' with non-walkable decorative land")]
    public bool fillBackgroundWithLandDecor = true;

    int[] cellRingDist;
    int shoreMaxRingsCached;
    int decorDelayRingsCached;

    float GetBaseYForCell(int cellIndex, GG.NodeLabel label)
    {
        // shore: slightly lower
        if (label == GG.NodeLabel.Shore) return 0 - Mathf.Abs(shoreDrop);

        // decorative land: rise per ring
        if (label == GG.NodeLabel.LandDecor)
        {
            // If distances werent cached, just start at islandsY
            int d = (cellRingDist != null && cellIndex >= 0 && cellIndex < cellRingDist.Length)
                      ? cellRingDist[cellIndex]
                      : int.MaxValue;

            // first LandDecor ring is the first d that satisfied: d > shoreMax + decorDelay
            // so ringIndex 0 corresponds to d = shoreMax + decorDelay + 1
            int firstDecorD = shoreMaxRingsCached + decorDelayRingsCached + 1;
            if (d == int.MaxValue) d = firstDecorD; // disconnected; treat as first decor ring

            int ringIndex = Mathf.Max(0, d - firstDecorD);
            return landStartYOffset + landRingStep * ringIndex;
        }

        if(label == GG.NodeLabel.None)
        {
            return 0 - Mathf.Abs(shoreDrop) * 2;
        }

        // anything else (Corridor/None/etc.)
        return 0;
    }


    bool IsIsland(GG.NodeLabel l) =>
    l == GG.NodeLabel.A || l == GG.NodeLabel.B || l == GG.NodeLabel.C
    || l == GG.NodeLabel.Start || l == GG.NodeLabel.Boss;

    void ApplyWaterRings(
    IReadOnlyList<VoronoiLayoutGenerator.Cell> cells,
    ref GG.NodeLabel[] labels,
    int shoreRingCount,
    int waterRingsBeforeDecor)
    {
        int n = cells.Count;
        if (n == 0 || labels == null || labels.Length != n) return;

        // --- Build adjacency by shared polygon edges --- //
        var adj = new List<List<int>>(n);
        for (int i = 0; i < n; i++) adj.Add(new List<int>());

        string SegKey(Vector2 a, Vector2 b)
        {
            const float eps = 1e-4f;
            long ax = (long)Mathf.Round(a.x / eps), ay = (long)Mathf.Round(a.y / eps);
            long bx = (long)Mathf.Round(b.x / eps), by = (long)Mathf.Round(b.y / eps);
            bool flip = (ax < bx) || (ax == bx && ay <= by);
            long x0 = flip ? ax : bx, y0 = flip ? ay : by;
            long x1 = flip ? bx : ax, y1 = flip ? by : ay;
            return x0 + "_" + y0 + "_" + x1 + "_" + y1;
        }

        var owner = new Dictionary<string, int>(n * 6);
        for (int i = 0; i < n; i++)
        {
            var poly = cells[i].polygon; if (poly == null || poly.Count < 3) continue;
            for (int k = 0; k < poly.Count; k++)
            {
                var a = poly[k];
                var b = poly[(k + 1) % poly.Count];
                string key = SegKey(a, b);
                if (owner.TryGetValue(key, out int j))
                {
                    if (i != j) { adj[i].Add(j); adj[j].Add(i); }
                }
                else owner[key] = i;
            }
        }

        // --- Multi-source BFS starting from ALL island cells --- //
        var dist = new int[n];
        for (int i = 0; i < n; i++) dist[i] = int.MaxValue;
        var q = new Queue<int>();

        for (int i = 0; i < n; i++)
            if (IsIsland(labels[i])) { dist[i] = 0; q.Enqueue(i); }

        while (q.Count > 0)
        {
            int u = q.Dequeue();
            int du = dist[u];
            foreach (var v in adj[u])
            {
                if (dist[v] > du + 1)
                {
                    dist[v] = du + 1;
                    q.Enqueue(v);
                }
            }
        }

        // 1) One ring of SHORE
        int shoreMax = Mathf.Max(1, shoreRingCount);
        for (int i = 0; i < n; i++)
        {
            if (labels[i] == GG.NodeLabel.None && dist[i] >= 1 && dist[i] <= shoreMax)
                labels[i] = GG.NodeLabel.Shore;
        }

        //2) After a few water tiles, convert remaining *unlabeled* to decorative, non-walkable land 
        int decorAfter = Mathf.Max(0, waterRingsBeforeDecor);
        for (int i = 0; i < n; i++)
        {
            if (labels[i] == GG.NodeLabel.None && dist[i] > shoreMax + decorAfter)
                labels[i] = GG.NodeLabel.LandDecor;

            if (labels[i] == GG.NodeLabel.None && dist[i] == int.MaxValue && decorAfter <= 0)
                labels[i] = GG.NodeLabel.LandDecor;
        }

        cellRingDist = dist;
        shoreMaxRingsCached = Mathf.Max(1, shoreRingCount);
        decorDelayRingsCached = Mathf.Max(0, waterRingsBeforeDecor);
    }


}
