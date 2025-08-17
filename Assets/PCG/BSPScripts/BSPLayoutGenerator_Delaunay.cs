using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DelaunatorSharp;
using System;

[Obsolete]
public class BSPLayoutGenerator_Delaunay : BSPLayoutGenerator
{
    [Header("Delaunay")]
    [Range(0f, 1f)] public float extraEdgeFraction = 0.35f;

    [ContextMenu("Generate Layout Delaunay")]
    public void GenerateLayoutDelaunay() => Generate();

    protected override void ConnectStrategy(DungeonLayout layout)
    {
        ConnectRoomsWithDelaunay(layout);
    }

    void ConnectRoomsWithDelaunay(DungeonLayout layout)
    {
        var pts = new List<IPoint>();
        var centers = new List<Vector2Int>();
        foreach (var r in layout.rooms)
        {
            var c = GetRoomCenterTile(r, layout);
            centers.Add(c);
            pts.Add(new Point(c.x, c.y));
        }
        if (pts.Count <= 1) return;

        var del = new Delaunator(pts.ToArray());

        var edgeSet = new HashSet<(int a, int b)>();
        int[] tri = del.Triangles;
        int Next(int e) => (e % 3 == 2) ? e - 2 : e + 1;
        for (int e = 0; e < tri.Length; e++)
        {
            int a = tri[e], b = tri[Next(e)];
            if (a == b) continue;
            if (a > b) (a, b) = (b, a);
            edgeSet.Add((a, b));
        }

        var edges = new List<(int a, int b, float w)>();
        foreach (var (a, b) in edgeSet)
        {
            float w = (centers[a] - centers[b]).sqrMagnitude;
            edges.Add((a, b, w));
        }
        edges.Sort((x, y) => x.w.CompareTo(y.w));

        var dsu = new DSU(centers.Count);
        var chosen = new List<(int a, int b)>();
        foreach (var (a, b, _) in edges)
            if (dsu.Union(a, b)) chosen.Add((a, b));

        var remaining = edges.Where(e => !chosen.Contains((e.a, e.b))).OrderBy(e => e.w).ToList();
        int extras = Mathf.RoundToInt(remaining.Count * Mathf.Clamp01(extraEdgeFraction));
        for (int i = 0; i < extras && i < remaining.Count; i++)
            chosen.Add((remaining[i].a, remaining[i].b));

        foreach (var (a, b) in chosen)
        {
            var A = centers[a];
            var B = centers[b];
            A = NearestRoomTile(A, FindLeafForPoint(A), layout);
            B = NearestRoomTile(B, FindLeafForPoint(B), layout);

            CreateCorridor(A, B, layout);
        }
    }
}
