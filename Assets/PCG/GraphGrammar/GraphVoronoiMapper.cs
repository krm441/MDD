using System.Collections.Generic;
using UnityEngine;
using System;

public class GraphVoronoiMapper
{
    public GG.NodeLabel[] LabelIslandsByRadius(
        VoronoiLayoutGenerator gen,
        GG.Graph g)
    {
        int n = gen.Cells.Count;
        var labels = new GG.NodeLabel[n];
        var adj = BuildCellAdjacencyQuantized(gen, 1e-3f);

        // seeds (nearest cell by centroid)
        var seeds = new List<(int nodeId, int cellIdx, GG.NodeLabel type, Vector2 pos, float radius)>();
        foreach (var node in g.nodes)
        {
            if (!IsIslandType(node.label)) continue;

            int best = FindClosestCellByCentroid(gen, node.pos);
            if (best >= 0)
                seeds.Add((node.id, best, node.label, node.pos, Mathf.Max(0f, node.radius)));
        }

        // grow islands
        foreach (var s in seeds)
            GrowIslandUntilRadius(gen, adj, s.cellIdx, s.pos, s.type, s.radius, ref labels);

        return labels;
    }

    bool IsIslandType(GG.NodeLabel L) =>
        L == GG.NodeLabel.A || L == GG.NodeLabel.B || L == GG.NodeLabel.C ||
        L == GG.NodeLabel.Start || L == GG.NodeLabel.Boss;

    // BFS growth: claim cells whose centroid distance <= radius
    void GrowIslandUntilRadius(
        VoronoiLayoutGenerator gen,
        List<List<int>> adj,
        int seedCell,
        Vector2 center,
        GG.NodeLabel islandType,
        float radius,
        ref GG.NodeLabel[] labels)
    {
        var frontier = new Queue<int>();
        var seen = new HashSet<int>();
        frontier.Enqueue(seedCell);
        seen.Add(seedCell);

        float r2 = radius * radius;

        while (frontier.Count > 0)
        {
            int cell = frontier.Dequeue();
            var c = PolyCentroid(gen.Cells[cell].polygon);
            float d2 = (c - center).sqrMagnitude;

            if (d2 <= r2)
            {
                if (labels[cell] == GG.NodeLabel.None)
                    labels[cell] = islandType;

                foreach (var nb in adj[cell])
                    if (seen.Add(nb)) frontier.Enqueue(nb);
            }
        }
    }

    int FindClosestCellByCentroid(VoronoiLayoutGenerator gen, Vector2 p)
    {
        int best = -1; float bd2 = float.PositiveInfinity;
        for (int i = 0; i < gen.Cells.Count; i++)
        {
            var poly = gen.Cells[i].polygon;
            if (poly == null || poly.Count < 3) continue;
            var c = PolyCentroid(poly);
            float d2 = (c - p).sqrMagnitude;
            if (d2 < bd2) { bd2 = d2; best = i; }
        }
        return best;
    }
        
    public void AddCorridorsWithAStar(
        VoronoiLayoutGenerator gen,
        GG.Graph g,
        ref GG.NodeLabel[] labels,
        float tileSize = 0.5f,
        int corridorThickness = 1)
    {
        var B = gen.worldBounds;

        foreach (var edge in g.edges)
        {
            int W = Mathf.CeilToInt(B.width / tileSize);
            int H = Mathf.CeilToInt(B.height / tileSize);
            if (W <= 1 || H <= 1) continue;

            var grid = new Pathfinding.Node[W, H];
            for (int gx = 0; gx < W; gx++)
                for (int gy = 0; gy < H; gy++)
                    grid[gx, gy] = new Pathfinding.Node(new Vector2Int(gx, gy), GridToWorld(gx, gy, B, tileSize), true);

            if (!TryPickEndpoint(gen, B, tileSize, g, edge.u, grid, out var start)) continue;
            if (!TryPickEndpoint(gen, B, tileSize, g, edge.v, grid, out var goal)) continue;

            var astar = new Pathfinding.AStar();
            astar.SetGrid(grid);

            var path = astar.FindPath(start, goal);
            if (path == null || path.Count == 0) continue;

            // Paint the path onto Voronoi cells:
            // keep island cells: paint 'Corridor' onlu on 'None'
            foreach (var step in path)
            {
                int ci = FindCellIndexAtPoint(gen, step.worldPos);
                if (ci >= 0 && labels[ci] == GG.NodeLabel.None)
                    labels[ci] = GG.NodeLabel.Corridor;

                if (corridorThickness > 1)
                    DilateCorridorStep(gen, ref labels, B, tileSize, step.gridPos, corridorThickness - 1);
            }
        }
    }

    // ------------------------------- helpers -------------------------------
    bool TryPickEndpoint(
        VoronoiLayoutGenerator gen,
        Rect B,
        float tileSize,
        GG.Graph g,
        int nodeId,
        Pathfinding.Node[,] grid,
        out Pathfinding.Node endpoint)
    {
        endpoint = null;

        var gn = g.nodes.Find(n => n.id == nodeId);
        if (gn == null) return false;

        var gp = WorldToGrid(gn.pos, B, tileSize);
        int W = grid.GetLength(0), H = grid.GetLength(1);

        // Spiral search outward until we hit any grid cell that maps to a valid Voronoi cell
        int maxR = Mathf.Max(W, H);
        for (int r = 0; r < maxR; r++)
        {
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    int gx = Mathf.Clamp(gp.x + dx, 0, W - 1);
                    int gy = Mathf.Clamp(gp.y + dy, 0, H - 1);
                    var wp = grid[gx, gy].worldPos;
                    int ci = FindCellIndexAtPoint(gen, wp);
                    if (ci >= 0)
                    {
                        endpoint = grid[gx, gy];
                        return true;
                    }
                }
        }
        return false;
    }

    void DilateCorridorStep(
        VoronoiLayoutGenerator gen,
        ref GG.NodeLabel[] labels,
        Rect B,
        float tileSize,
        Vector2Int gp,
        int radius)
    {
        int W = Mathf.CeilToInt(B.width / tileSize);
        int H = Mathf.CeilToInt(B.height / tileSize);
        for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
            {
                int gx = gp.x + dx, gy = gp.y + dy;
                if (gx < 0 || gy < 0 || gx >= W || gy >= H) continue;
                int ci = FindCellIndexAtPoint(gen, GridToWorld(gx, gy, B, tileSize));
                if (ci >= 0 && labels[ci] == GG.NodeLabel.None)
                    labels[ci] = GG.NodeLabel.Corridor;
            }
    }

    int FindCellIndexAtPoint(VoronoiLayoutGenerator gen, Vector3 worldPos)
    {
        var p = new Vector2(worldPos.x, worldPos.z);
        var cells = gen.Cells;
        for (int i = 0; i < cells.Count; i++)
        {
            var poly = cells[i].polygon;
            if (poly == null || poly.Count < 3) continue;
            if (PointInPolygonInclusive(p, poly)) return i;
        }
        return -1;
    }

    bool PointInPolygonInclusive(Vector2 p, List<Vector2> poly)
    {
        bool inside = false;
        for (int i = 0, j = poly.Count - 1; i < poly.Count; j = i++)
        {
            var a = poly[i]; var b = poly[j];
            if (PointOnSegment(p, a, b)) return true; // edge counts as inside
            bool intersect = ((a.y > p.y) != (b.y > p.y)) &&
                             (p.x < (b.x - a.x) * (p.y - a.y) / ((b.y - a.y) + Mathf.Epsilon) + a.x);
            if (intersect) inside = !inside;
        }
        return inside;
    }

    bool PointOnSegment(Vector2 p, Vector2 a, Vector2 b, float eps = 1e-4f)
    {
        float cross = (p.y - a.y) * (b.x - a.x) - (p.x - a.x) * (b.y - a.y);
        if (Mathf.Abs(cross) > eps) return false;
        float dot = (p.x - a.x) * (b.x - a.x) + (p.y - a.y) * (b.y - a.y);
        if (dot < -eps) return false;
        float len2 = (b - a).sqrMagnitude;
        if (dot - len2 > eps) return false;
        return true;
    }

    Vector2Int WorldToGrid(Vector2 w, Rect B, float tileSize)
    {
        int gx = Mathf.Clamp(Mathf.FloorToInt((w.x - B.xMin) / tileSize), 0, Mathf.CeilToInt(B.width / tileSize) - 1);
        int gy = Mathf.Clamp(Mathf.FloorToInt((w.y - B.yMin) / tileSize), 0, Mathf.CeilToInt(B.height / tileSize) - 1);
        return new Vector2Int(gx, gy);
    }

    Vector3 GridToWorld(int gx, int gy, Rect B, float tileSize)
    {
        float x = B.xMin + (gx + 0.5f) * tileSize;
        float z = B.yMin + (gy + 0.5f) * tileSize;
        return new Vector3(x, 0f, z);
    }

    List<List<int>> BuildCellAdjacencyQuantized(VoronoiLayoutGenerator gen, float eps)
    {
        long Key(Vector2 v)
        {
            float inv = 1f / eps;
            long kx = (long)Mathf.Round(v.x * inv);
            long ky = (long)Mathf.Round(v.y * inv);
            return (kx << 32) ^ (ky & 0xffffffffL);
        }

        var indexBySite = new Dictionary<long, int>(gen.Cells.Count);
        for (int i = 0; i < gen.Cells.Count; i++)
            indexBySite[Key(gen.Cells[i].site)] = i;

        var adj = new List<List<int>>(gen.Cells.Count);
        for (int i = 0; i < gen.Cells.Count; i++) adj.Add(new List<int>());

        foreach (var e in gen.Edges)
        {
            if (e.Left == null || e.Right == null) continue;
            var ls = new Vector2((float)e.Left.X, (float)e.Left.Y);
            var rs = new Vector2((float)e.Right.X, (float)e.Right.Y);
            if (!indexBySite.TryGetValue(Key(ls), out int li)) continue;
            if (!indexBySite.TryGetValue(Key(rs), out int ri)) continue;
            if (li == ri) continue;
            if (!adj[li].Contains(ri)) adj[li].Add(ri);
            if (!adj[ri].Contains(li)) adj[ri].Add(li);
        }
        return adj;
    }

    Vector2 PolyCentroid(List<Vector2> P)
    {
        double A = 0, cx = 0, cy = 0;
        int m = P.Count;
        for (int i = 0; i < m; i++)
        {
            var p = P[i]; var q = P[(i + 1) % m];
            double cross = p.x * q.y - q.x * p.y;
            A += cross; cx += (p.x + q.x) * cross; cy += (p.y + q.y) * cross;
        }
        A *= 0.5; if (Math.Abs(A) < 1e-12) return P[0];
        return new Vector2((float)(cx / (6 * A)), (float)(cy / (6 * A)));
    }
}
