using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DelaunatorSharp;

public class BSPLayoutGenerator_DelaunayMoat : BSPLayoutGenerator
{
    [Header("Delaunay with Moat")]
    [Range(0f, 1f)] public float extraEdgeFraction = 0.35f;
    [Range(1, 4)] public int minDoorsPerRoom = 2;
    [Range(1, 4)] public int maxDoorsPerRoom = 4;
    public bool preferFacingDoors = true;

    [ContextMenu("Generate Layout Delaunay++")]
    public void GenerateLayoutDelaunayMoat() => Generate();

    protected override void ConnectStrategy(DungeonLayout layout)
    {
        // 1) Door candidates + selection
        var centers = new List<Vector2Int>();
        var roomDoorOptions = new List<List<(Vector2Int door, Vector2Int outward)>>();
        var chosenDoorExits = new Dictionary<int, List<Vector2Int>>();

        for (int i = 0; i < layout.rooms.Count; i++)
        {
            var c = GetRoomCenterTile(layout.rooms[i], layout);
            centers.Add(c);

            var candidates = GetRoomPerimeterDoorCandidates(layout.rooms[i], layout);
            roomDoorOptions.Add(candidates);

            //int want = Mathf.Clamp(Random.Range(minDoorsPerRoom, maxDoorsPerRoom + 1), 1, 3);
            int want = Mathf.Clamp(Random.Range(minDoorsPerRoom, maxDoorsPerRoom + 1), 1, 4);
            var chosen = ChooseDoors(candidates, want);

            var exits = new List<Vector2Int>();
            foreach (var d in chosen) exits.Add(d.door + d.outward);
            chosenDoorExits[i] = exits;
        }

        // 2) Moat
        var forbidden = BuildMoat(layout, chosenDoorExits);

        // 3) Delaunay -> MST -> extra edges
        var pts = centers.Select(c => (IPoint)new Point(c.x, c.y)).ToArray();
        var del = new Delaunator(pts);

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
        var chosenEdges = new List<(int a, int b)>();
        foreach (var (a, b, _) in edges) if (dsu.Union(a, b)) chosenEdges.Add((a, b));

        var remaining = edges.Where(e => !chosenEdges.Contains((e.a, e.b))).OrderBy(e => e.w).ToList();
        int extrasToAdd = Mathf.RoundToInt(remaining.Count * Mathf.Clamp01(extraEdgeFraction));
        for (int i = 0; i < extrasToAdd && i < remaining.Count; i++) chosenEdges.Add((remaining[i].a, remaining[i].b));

        // 4) Build grid for A*
        var grid = BuildPathfindingGrid(layout, forbidden, chosenDoorExits);
        var astar = new Pathfinding.AStar();
        astar.allowDiagonals = false;
        astar.SetGrid(grid);

        // 5) Route door -> door; staircase detection fallback to Manhattan
        foreach (var (ia, ib) in chosenEdges)
        {
            var aDoors = roomDoorOptions[ia];
            var bDoors = roomDoorOptions[ib];
            var pair = ChooseBestDoorPair(ia, ib, aDoors, bDoors, centers, preferFacingDoors);

            Vector2Int startPos = pair.aDoor.door + pair.aDoor.outward;
            Vector2Int goalPos = pair.bDoor.door + pair.bDoor.outward;

            if (!InBounds(startPos) || !InBounds(goalPos)) continue;

            var startNode = grid[startPos.x, startPos.y];
            var goalNode = grid[goalPos.x, goalPos.y];

            startNode.isWalkable = true;   // moat punched
            goalNode.isWalkable = true;

            var nodePath = astar.FindPath(startNode, goalNode);

            if (nodePath != null && nodePath.Count > 0 && !Pathfinding.AStar.IsStaircaseLikePath(nodePath, 3))//             IsStaircaseLike(nodePath, 3))
            {
                layout.floorTiles.Add(startPos); // include start outside tile, since my A* ignores the start node
                foreach (var n in nodePath) layout.floorTiles.Add(n.gridPos);
            }
            else
            {
                CarveManhattanAvoiding(startPos, goalPos, layout, forbidden);
            }
        }

        bool InBounds(Vector2Int p) => p.x >= 0 && p.x < dungeonSize.x && p.y >= 0 && p.y < dungeonSize.y;
    }

    // ---- Door/moat/path helpers ----
    List<(Vector2Int door, Vector2Int outward)> GetRoomPerimeterDoorCandidates(RectInt rect, DungeonLayout L)
    {
        var res = new List<(Vector2Int, Vector2Int)>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int x = rect.xMin; x < rect.xMax; x++)
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                var p = new Vector2Int(x, y);
                if (!L.roomTiles.Contains(p)) continue;
                foreach (var d in dirs)
                {
                    var q = p + d;
                    if (!L.roomTiles.Contains(q) && q.x >= 0 && q.x < dungeonSize.x && q.y >= 0 && q.y < dungeonSize.y)
                        res.Add((p, d));
                }
            }
        
        res = res.GroupBy(t => t.Item1).Select(g => g.First()).ToList();
        return res;
    }

    List<(Vector2Int door, Vector2Int outward)> ChooseDoors(List<(Vector2Int door, Vector2Int outward)> candidates, int n)
    {
        if (candidates == null || candidates.Count == 0) 
            return new List<(Vector2Int, Vector2Int)>();

        for (int i = 0; i < candidates.Count; i++) 
        { 
            int j = Random.Range(i, candidates.Count); 
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        var chosen = new List<(Vector2Int, Vector2Int)>();
        foreach (var c in candidates)
        {
            bool farEnough = true;
            foreach (var k in chosen) 
                if ((k.Item1 - c.door).sqrMagnitude < 9) { farEnough = false; break; }

            if (farEnough) 
            { 
                chosen.Add(c); 
                if (chosen.Count >= n)                     break; 
            }
        }
        for (int i = 0; chosen.Count < n && i < candidates.Count; i++)
            if (!chosen.Contains(candidates[i])) chosen.Add(candidates[i]);
        return chosen;
    }

    HashSet<Vector2Int> BuildMoat(DungeonLayout L, Dictionary<int, List<Vector2Int>> doorExitsByRoom)
    {
        var forbid = new HashSet<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var p in L.roomTiles)
            foreach (var d in dirs)
            {
                var q = p + d;
                if (q.x < 0 || q.x >= dungeonSize.x || q.y < 0 || q.y >= dungeonSize.y) continue;
                if (!L.roomTiles.Contains(q)) forbid.Add(q);
            }

        foreach (var kv in doorExitsByRoom)
            foreach (var exit in kv.Value) forbid.Remove(exit);
        return forbid;
    }

    (
        (Vector2Int door, Vector2Int outward) aDoor, 
        (Vector2Int door, Vector2Int outward) bDoor
    )
    ChooseBestDoorPair(int ia, int ib,
                        List<(Vector2Int door, Vector2Int outward)> aDoors,
                        List<(Vector2Int door, Vector2Int outward)> bDoors,
                        List<Vector2Int> centers,
                        bool faceBias)
    {
        (Vector2Int door, Vector2Int outward) bestA = aDoors[0];
        (Vector2Int door, Vector2Int outward) bestB = bDoors[0];
        float bestScore = float.PositiveInfinity;

        foreach (var da in aDoors)
            foreach (var db in bDoors)
            {
                float dist = ((da.door + da.outward) - (db.door + db.outward)).sqrMagnitude;
                float score = dist;

                if (faceBias)
                {
                    var v = (db.door - da.door);
                    var vNorm = (v == Vector2Int.zero) ? Vector2.zero : new Vector2(v.x, v.y).normalized;
                    float alignA = Vector2.Dot(new Vector2(da.outward.x, da.outward.y), vNorm);
                    float alignB = Vector2.Dot(new Vector2(-db.outward.x, -db.outward.y), vNorm);
                    score *= (2f - 0.25f * (alignA + alignB));
                }

                if (score < bestScore) { bestScore = score; bestA = da; bestB = db; }
            }
        return (bestA, bestB);
    }

    Pathfinding.Node[,] BuildPathfindingGrid(DungeonLayout L, HashSet<Vector2Int> forbidden,
                                             Dictionary<int, List<Vector2Int>> doorExitsByRoom)
    {
        var grid = new Pathfinding.Node[dungeonSize.x, dungeonSize.y];
        for (int x = 0; x < dungeonSize.x; x++)
            for (int y = 0; y < dungeonSize.y; y++)
            {
                var p = new Vector2Int(x, y);
                bool blocked = L.roomTiles.Contains(p) || forbidden.Contains(p);
                grid[x, y] = new Pathfinding.Node(p, new Vector3(x, 0f, y), !blocked);
            }
        foreach (var kv in doorExitsByRoom)
            foreach (var pos in kv.Value)
                if (pos.x >= 0 && pos.x < dungeonSize.x && pos.y >= 0 && pos.y < dungeonSize.y)
                    grid[pos.x, pos.y].isWalkable = true;
        return grid;
    }

    // Staircase detector
    bool IsStaircaseLike(List<Pathfinding.Node> path, int tolerance = 3)
    {
        if (path == null || path.Count < 3) return false;

        int maxDiagInRun = 0; int i = 1;
        while (i < path.Count)
        {
            var prev = path[i - 1].gridPos;
            var cur = path[i].gridPos;
            int dx = cur.x - prev.x, dy = cur.y - prev.y;

            if (Mathf.Abs(dx) == 1 && Mathf.Abs(dy) == 1)
            {
                int sx = dx > 0 ? 1 : -1, sy = dy > 0 ? 1 : -1;
                int diagCount = 1; bool expectOrth = true;
                int j = i + 1;
                while (j < path.Count)
                {
                    var a = path[j - 1].gridPos; var b = path[j].gridPos;
                    int ddx = b.x - a.x, ddy = b.y - a.y;

                    if (expectOrth)
                    {
                        bool ok = (ddx == sx && ddy == 0) || (ddx == 0 && ddy == sy);
                        if (!ok) break; expectOrth = false;
                    }
                    else
                    {
                        bool isDiag = Mathf.Abs(ddx) == 1 && Mathf.Abs(ddy) == 1;
                        bool sameDir = (ddx > 0 ? 1 : -1) == sx && (ddy > 0 ? 1 : -1) == sy;
                        if (!(isDiag && sameDir)) break;
                        diagCount++; expectOrth = true;
                    }
                    j++;
                }
                if (diagCount > maxDiagInRun) maxDiagInRun = diagCount;
                i = j;
            }
            else i++;
        }
        return maxDiagInRun >= Mathf.Max(1, tolerance);
    }

    // Manhattan fallback
    void CarveManhattanAvoiding(Vector2Int start, Vector2Int goal, DungeonLayout L, HashSet<Vector2Int> forbidden)
    {
        L.floorTiles.Add(start);
        Vector2Int cur = start;
        int guard = dungeonSize.x * dungeonSize.y * 2;

        bool Blocked(Vector2Int p) =>
            p.x < 0 || p.x >= dungeonSize.x || p.y < 0 || p.y >= dungeonSize.y ||
            L.roomTiles.Contains(p) || forbidden.Contains(p);

        while (cur != goal && guard-- > 0)
        {
            Vector2Int step = cur;
            if (cur.x != goal.x)
            {
                int dx = (cur.x < goal.x) ? 1 : -1;
                if (!Blocked(cur + new Vector2Int(dx, 0))) step = cur + new Vector2Int(dx, 0);
                else if (cur.y != goal.y)
                {
                    int dy = (cur.y < goal.y) ? 1 : -1;
                    if (!Blocked(cur + new Vector2Int(0, dy))) step = cur + new Vector2Int(0, dy);
                }
            }
            else if (cur.y != goal.y)
            {
                int dy = (cur.y < goal.y) ? 1 : -1;
                if (!Blocked(cur + new Vector2Int(0, dy))) step = cur + new Vector2Int(0, dy);
                else if (cur.x != goal.x)
                {
                    int dx = (cur.x < goal.x) ? 1 : -1;
                    if (!Blocked(cur + new Vector2Int(dx, 0))) step = cur + new Vector2Int(dx, 0);
                }
            }

            if (step == cur) break;
            cur = step;
            L.floorTiles.Add(cur);
        }
        L.floorTiles.Add(goal);
    }
}
