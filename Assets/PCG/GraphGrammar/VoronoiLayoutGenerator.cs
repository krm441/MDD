using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VoronoiLib;
using VoronoiLib.Structures;

[ExecuteAlways]
public class VoronoiLayoutGenerator : MonoBehaviour
{
    [Header("Bounds & Inputs")]
    public Rect worldBounds = new Rect(-25, -25, 50, 50);   // XZ plane
    public float hexSize = 1.0f;
    [Range(0f, 1f)] public float jitterRadius = 0.2f;
    [Tooltip("Lloyd relaxation iterations")] public int relaxIterations = 1;
    public int randomSeed = 1234;

    [Header("Lifecycle")]
    public bool regenerateOnValidate = true;
    public bool autoGenerateOnPlay = true;

    // Outputs
    public IReadOnlyList<Cell> Cells => cells;
    public LinkedList<VEdge> Edges => edges; // for gizmos/diagnostics
    public IReadOnlyList<Vector2> Sites => sitePositions;

    [Serializable]
    public class Cell
    {
        public Vector2 site;              // site position
        public List<Vector2> polygon;     // CCW, in XZ plane
    }
        
    private System.Random rng;
    private List<Vector2> sitePositions = new List<Vector2>();
    private LinkedList<VEdge> edges;
    private readonly List<Cell> cells = new List<Cell>();

    
    [ContextMenu("Generate")]
    public void Generate()
    {
        rng = new System.Random(randomSeed);

        // 1) hex lattice + jitter
        var lattice = MakeHexLattice(worldBounds, hexSize);
        sitePositions = Jitter(lattice, jitterRadius, rng);

        // 2) Fortune + Lloyd (at least one relax round looks good)
        var sites = sitePositions.Select(p => new FortuneSite(p.x, p.y)).ToList();

        for (int it = 0; it <= relaxIterations; it++)
        {
            edges = FortunesAlgorithm.Run(
                sites,
                worldBounds.xMin, worldBounds.yMin,
                worldBounds.xMax, worldBounds.yMax
            );

            var polys = BuildCellsBySite(sites, edges);

            if (it < relaxIterations)
            {
                // Move sites to centroids (Lloyd)
                var newSites = new List<FortuneSite>(sites.Count);
                foreach (var s in sites)
                {
                    if (polys.TryGetValue(s, out var poly) && poly != null && poly.Count >= 3)
                    {
                        var c = PolygonCentroid(poly);
                        newSites.Add(new FortuneSite(c.x, c.y));
                    }
                    else newSites.Add(new FortuneSite((float)s.X, (float)s.Y));
                }
                sites = newSites;
                sitePositions = sites.Select(s => new Vector2((float)s.X, (float)s.Y)).ToList();
            }
            else
            {
                // Finalize outputs
                cells.Clear();
                foreach (var s in sites)
                {
                    if (!polys.TryGetValue(s, out var poly) || poly == null || poly.Count < 3) continue;
                    cells.Add(new Cell { site = new Vector2((float)s.X, (float)s.Y), polygon = poly });
                }
            }
        }
    }

    public void Clear()
    {
        cells.Clear();
    }

    // -------- helpers -------- //

    private static Dictionary<FortuneSite, List<Vector2>> BuildCellsBySite(List<FortuneSite> siteList, LinkedList<VEdge> vedges)
    {
        var map = new Dictionary<FortuneSite, List<Vector2>>();
        foreach (var s in siteList) map[s] = new List<Vector2>();

        foreach (var e in vedges)
        {
            if (e.Start == null || e.End == null) continue;
            var a = new Vector2((float)e.Start.X, (float)e.Start.Y);
            var b = new Vector2((float)e.End.X, (float)e.End.Y);
            if (e.Left != null) { map[e.Left].Add(a); map[e.Left].Add(b); }
            if (e.Right != null) { map[e.Right].Add(a); map[e.Right].Add(b); }
        }

        var cells = new Dictionary<FortuneSite, List<Vector2>>();
        foreach (var s in siteList)
        {
            var pts = map[s];
            if (pts.Count < 3) continue;
            var uniq = UniqueByQuantize(pts, 1e-4f);
            if (uniq.Count < 3) continue;
            var center = new Vector2((float)s.X, (float)s.Y);
            uniq.Sort((u, v) => Angle(center, u).CompareTo(Angle(center, v)));
            cells[s] = uniq;
        }
        return cells;
    }

    private static float Angle(Vector2 o, Vector2 p) => Mathf.Atan2(p.y - o.y, p.x - o.x);

    private static List<Vector2> UniqueByQuantize(List<Vector2> pts, float eps)
    {
        var hash = new HashSet<long>();
        var outPts = new List<Vector2>();
        float inv = 1f / eps;
        foreach (var p in pts)
        {
            long kx = (long)Math.Round(p.x * inv);
            long ky = (long)Math.Round(p.y * inv);
            long key = (kx << 32) ^ (ky & 0xffffffffL);
            if (hash.Add(key)) outPts.Add(p);
        }
        return outPts;
    }

    private static Vector2 AxialToWorld(int q, int r, float size)
    {
        float x = size * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float y = size * 1.5f * r;
        return new Vector2(x, y);
    }
    private static List<Vector2> MakeHexLattice(Rect B, float size)
    {
        var pts = new List<Vector2>();
        float dx = Mathf.Sqrt(3f) * size;
        float dy = 1.5f * size;
        int rMin = Mathf.FloorToInt((B.yMin - 2 * size) / dy) - 2;
        int rMax = Mathf.CeilToInt((B.yMax + 2 * size) / dy) + 2;
        for (int r = rMin; r <= rMax; r++)
        {
            int q0 = Mathf.FloorToInt((B.xMin - 2 * size) / dx - 0.5f * r) - 2;
            int q1 = Mathf.CeilToInt((B.xMax + 2 * size) / dx - 0.5f * r) + 2;
            for (int q = q0; q <= q1; q++)
            {
                var p = AxialToWorld(q, r, size);
                if (B.Contains(p)) pts.Add(p);
            }
        }
        return pts;
    }
    private static List<Vector2> Jitter(List<Vector2> centers, float radius, System.Random rng)
    {
        if (radius <= 0f) return new List<Vector2>(centers);
        var outPts = new List<Vector2>(centers.Count);
        foreach (var c in centers)
        {
            double u = rng.NextDouble(), v = rng.NextDouble();
            float r = radius * Mathf.Sqrt((float)u);
            float a = (float)(2 * Math.PI * v);
            outPts.Add(c + new Vector2(r * Mathf.Cos(a), r * Mathf.Sin(a)));
        }
        return outPts;
    }
    private static Vector2 PolygonCentroid(List<Vector2> P)
    {
        double A = 0, cx = 0, cy = 0;
        int n = P.Count;
        for (int i = 0; i < n; i++)
        {
            var p = P[i]; var q = P[(i + 1) % n];
            double cross = p.x * q.y - q.x * p.y;
            A += cross; cx += (p.x + q.x) * cross; cy += (p.y + q.y) * cross;
        }
        A *= 0.5; if (Math.Abs(A) < 1e-12) return P.Aggregate(Vector2.zero, (s, v) => s + v) / n;
        return new Vector2((float)(cx / (6 * A)), (float)(cy / (6 * A)));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 0);
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        if (Edges == null) return;
        Gizmos.color = Color.yellow;
        foreach (var e in Edges)
        {
            if (e.Start == null || e.End == null) continue;
            Vector3 a = new Vector3((float)e.Start.X, 0f, (float)e.Start.Y);
            Vector3 b = new Vector3((float)e.End.X, 0f, (float)e.End.Y);
            Gizmos.DrawLine(a, b);
        }
    }
}
