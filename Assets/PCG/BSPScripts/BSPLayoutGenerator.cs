using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DelaunatorSharp;
using System.Drawing;
using UnityEngine.UIElements;
using UnityEditor;


[System.Serializable]
public class DungeonLayout
{
    public List<RectInt> rooms = new List<RectInt>();
    public HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> roomTiles = new HashSet<Vector2Int>();

    public List<Room> layoutRooms = new List<Room>();
    public List<Vector2Int> roomCenters = new List<Vector2Int>();
}

public enum RoomKind { Rectangle, L, T, U, Plus, Donut, Octagon, Circle, Cavern }

[System.Serializable]
public class RoomWeights
{
    [Range(0, 1)] public float rectangle = 0.4f;
    [Range(0, 1)] public float l = 0.15f;
    [Range(0, 1)] public float t = 0.15f;
    [Range(0, 1)] public float u = 0.1f;
};

[ExecuteInEditMode]
public abstract class BSPLayoutGenerator : MonoBehaviour
{
    [Header("Dungeon Settings")]
    public Vector2Int dungeonSize = new Vector2Int(64, 64);
    public int minRoomSize = 10;

    //[SerializeField] 
    //private int maxRoomSize;// = 2 * minRoomSize + 1;
    public int margin = 2;
    public int seed = 1;

    //[Header("BSP Leaf Area (bounds)")]
    //[Tooltip("Minimum area (in tiles) for a BSP leaf bounding box. 0 = ignore.")]
    //public int minLeafArea = 0;

    //[Tooltip("Maximum area (in tiles) for a BSP leaf bounding box. 0 = ignore.")]
    //public int maxLeafArea = 0;

    //[Header("BSP Leaf Area (bounds)")]
    //[Tooltip("Stop splitting when leaf bounding-box area is <= this (0 = disabled).")]
    //public int targetLeafArea = 30;

    [Header("Room Shapes")]
    public RoomWeights roomWeights = new RoomWeights();

    [Header("U-Shape Constraints")]
    [Range(2, 8)] public int minUThickness = 3;
    [Range(2, 12)] public int maxUThickness = 6;
    [Range(0.15f, 0.45f)] public float uThicknessRatio = 0.25f;
    [Range(0.4f, 0.9f)] public float uMinLegLengthRatio = 0.65f;
    [Range(0.20f, 0.90f)] public float uMinAreaRatio = 0.35f;

    [Header("Tile (floort) Scale")]
    public float tileScale = 4f;

    //[Header("BSP Controls (classic)")]
    //[Range(1f, 3f)] public float maxLeafAspect = 1.8f;   // 0 = ignore aspect; typical 1.6–2.0
    //[Range(0f, 0.45f)] public float splitJitter = 0.20f;  // 0 = always center, 0.2 = mild variety

    public Room startRoom, bossRoom;

    protected BSPNode root;
    public DungeonLayout LastLayout { get; protected set; }

    public void Generate()
    {
        Random.InitState(seed);
        root = new BSPNode(new RectInt(0, 0, dungeonSize.x, dungeonSize.y));

        var layout = new DungeonLayout();
        Split(root, 2, 90, minRoomSize);
        CollectLeaves(root, layout);

        ConnectStrategy(layout);

        // rooms
        BuildLogicalRooms(layout);
        AssignStartAndBoss(layout);

        LastLayout = layout;
        Console.Log(GetType().Name, ":", layout.rooms.Count, "rooms", layout.floorTiles.Count, "tiles.");
    }

    void Split(BSPNode node, int depth, int maxDepth, int minRoomSize)
    {
        if (node == null) return;
        var aabb = node.bounds = node.poly?.AABB() ?? node.bounds;

        int w = aabb.width, h = aabb.height;

        // Stop if we hit depth or cant split into two rooms >= minRoomSize on any axis
        if (depth >= maxDepth) return;
        bool canH = h >= 2 * minRoomSize;
        bool canV = w >= 2 * minRoomSize;
        if (!canH && !canV) return;

        // Axis: prefer the longer feasible axis; if both feasible and equal - pik at random
        bool splitH = canH && (!canV || h >= w || (w == h && UnityEngine.Random.value < 0.5f));
        if (splitH && !canH) splitH = false;             // guard
        if (!splitH && !canV) splitH = true;

        if (splitH)
        {
            // Pick a ratio in [0.4, 0.7]; clamp so both parts >= minRoomSize.
            float ratio = UnityEngine.Random.Range(0.4f, 0.7f);
            int lo = aabb.yMin + minRoomSize;
            int hi = aabb.yMax - minRoomSize;
            int splitY = Mathf.Clamp(Mathf.RoundToInt(aabb.yMin + ratio * h), lo, hi);

            var topRect = new RectInt(aabb.xMin, splitY, aabb.width, aabb.yMax - splitY);
            var bottomRect = new RectInt(aabb.xMin, aabb.yMin, aabb.width, splitY - aabb.yMin);

            node.split = Line2.Horizontal(splitY);
            node.left = new BSPNode(bottomRect) { poly = Poly2.FromRect(bottomRect) }; // keep spatial ordering
            node.right = new BSPNode(topRect) { poly = Poly2.FromRect(topRect) };
        }
        else
        {
            float ratio = UnityEngine.Random.Range(0.4f, 0.7f);
            int lo = aabb.xMin + minRoomSize;
            int hi = aabb.xMax - minRoomSize;
            int splitX = Mathf.Clamp(Mathf.RoundToInt(aabb.xMin + ratio * w), lo, hi);

            var leftRect = new RectInt(aabb.xMin, aabb.yMin, splitX - aabb.xMin, aabb.height);
            var rightRect = new RectInt(splitX, aabb.yMin, aabb.xMax - splitX, aabb.height);

            node.split = Line2.Vertical(splitX);
            node.left = new BSPNode(leftRect) { poly = Poly2.FromRect(leftRect) };
            node.right = new BSPNode(rightRect) { poly = Poly2.FromRect(rightRect) };
        }

        Split(node.left, depth + 1, maxDepth, minRoomSize);
        Split(node.right, depth + 1, maxDepth, minRoomSize);
    }

    protected abstract void ConnectStrategy(DungeonLayout layout);

    // ---------------- Rooms --------------- //
    void BuildLogicalRooms(DungeonLayout layout)
    {
        layout.layoutRooms.Clear();
        for (int i = 0; i < layout.rooms.Count; i++)
        {
            Vector2Int c = layout.roomCenters[i];
            Vector3 world = new Vector3(c.x * tileScale, 0f, c.y * tileScale);
            layout.layoutRooms.Add(new Room
            {
                id = i,
                label = RoomLabel.Unassigned,
                worldPos = world
            });
        }
    }

    void AssignStartAndBoss(DungeonLayout layout)
    {
        if (layout.layoutRooms.Count < 2) return;

        // pick farthest pair by BFS over floorTiles (rooms + corridors)
        int bestI = 0, bestJ = 1, bestDist = -1;

        for (int i = 0; i < layout.layoutRooms.Count; i++)
        {
            for (int j = i + 1; j < layout.layoutRooms.Count; j++)
            {
                int d = GridShortestPath(layout.roomCenters[i], layout.roomCenters[j], layout);
                if (d > bestDist)
                {
                    bestDist = d;
                    bestI = i; bestJ = j;
                }
            }
        }


        // Clear & set labels
        foreach (var r in layout.layoutRooms) r.label = RoomLabel.Unassigned;
        layout.layoutRooms[bestI].label = RoomLabel.Boss;
        layout.layoutRooms[bestJ].label = RoomLabel.Start;

        // store IDs
        startRoom = layout.layoutRooms.Find(r => r.label == RoomLabel.Start);
        bossRoom = layout.layoutRooms.Find(r => r.label == RoomLabel.Boss);
    }

    // 4-neighbour BFS through walkable tiles = layout.floorTiles
    int GridShortestPath(Vector2Int a, Vector2Int b, DungeonLayout L)
    {
        if (a == b) return 0;
        if (!L.floorTiles.Contains(a) || !L.floorTiles.Contains(b)) return -1;

        var q = new Queue<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var dist = new Dictionary<Vector2Int, int>();

        q.Enqueue(a); seen.Add(a); dist[a] = 0;

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        bool InBounds(Vector2Int p) => p.x >= 0 && p.x < dungeonSize.x && p.y >= 0 && p.y < dungeonSize.y;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int d = dist[p];

            foreach (var dir in dirs)
            {
                var n = p + dir;
                if (!InBounds(n) || seen.Contains(n)) continue;
                if (!L.floorTiles.Contains(n)) continue;

                if (n == b) return d + 1;
                seen.Add(n);
                dist[n] = d + 1;
                q.Enqueue(n);
            }
        }
        return -1; // unreachable
    }


    // ---------------- BSP  ---------------- //
    // --- Classical 2D BSP node (stores polygon + split line) ---
    protected sealed class BSPNode
    {
        // Leaf geometry (convex polygon). Internal nodes keep this null.
        public Poly2 poly;

        // AABB cached for FindLeafForPoint / room sizing / quick tests
        public RectInt bounds;

        // Split line for internal nodes: n·p + d = 0 (front: n·p + d >= 0)
        public Line2 split;

        public BSPNode left, right;
                
        public Vector2Int roomCenter;

        public bool IsLeaf => left == null && right == null;

        // Root ctor from a rectangle
        public BSPNode(RectInt r)
        {
            poly = Poly2.FromRect(r);
            bounds = poly.AABB();
        }

        // Compute room bounds = polygon AABB shrunk by margin (clamped to ≥1 cell)
        public RectInt GetRoomBounds(int margin)
        {
            var aabb = poly != null ? poly.AABB() : bounds;
            int x = aabb.x + margin;
            int y = aabb.y + margin;
            int w = Mathf.Max(1, aabb.width - 2 * margin);
            int h = Mathf.Max(1, aabb.height - 2 * margin);
            return new RectInt(x, y, w, h);
        }
    }
    // Numerical tolerance for half-plane tests
    //const float EPS = 1e-5f;

    void OnDrawGizmosSelected()
    {
        // draw BSP leaves
        void DrawLeaves(BSPNode n)
        {
            if (n == null) return;
            if (n.IsLeaf)
            {
                var r = n.bounds;
                var c = new Vector3((r.x + r.width * 0.5f) * tileScale, 0.05f, (r.y + r.height * 0.5f) * tileScale);
                var s = new Vector3(r.width * tileScale, 0.01f, r.height * tileScale);
                Gizmos.color = new UnityEngine.Color(0f, 1f, 0f, 0.10f); Gizmos.DrawCube(c, s);
                Gizmos.color = UnityEngine.Color.green; Gizmos.DrawWireCube(c, s);
            }
            else { DrawLeaves(n.left); DrawLeaves(n.right); }
        }

        if (root != null) DrawLeaves(root);                             // leaves (pre-room)

        if (LastLayout != null && LastLayout.rooms != null)            // final room rects
        {
            Gizmos.color = new UnityEngine.Color(0f, 0.8f, 1f, 0.12f);
            foreach (var r in LastLayout.rooms)
            {
                var c = new Vector3((r.x + r.width * 0.5f) * tileScale, 0.06f, (r.y + r.height * 0.5f) * tileScale);
                var s = new Vector3(r.width * tileScale, 0.01f, r.height * tileScale);
                Gizmos.DrawCube(c, s);
                Gizmos.color = new UnityEngine.Color(0f, 0.5f, 1f, 1f); Gizmos.DrawWireCube(c, s);
                Gizmos.color = new UnityEngine.Color(0f, 0.8f, 1f, 0.12f);
            }
        }
    }




    protected void CollectLeaves(BSPNode node, DungeonLayout layout)
    {
        if (node == null) return;

        if (node.IsLeaf)
        {
            var room = node.GetRoomBounds(margin);
            layout.rooms.Add(room);

            CarveRoom(room, layout);
            node.roomCenter = GetRoomCenterTile(room, layout);

            // for rrooms
            layout.roomCenters.Add(node.roomCenter);
        }
        else
        {
            CollectLeaves(node.left, layout);
            CollectLeaves(node.right, layout);
        }
    }

    protected Vector2Int GetRoomCenterTile(RectInt room, DungeonLayout layout)
    {
        Vector2 center = new Vector2(room.center.x, room.center.y);
        Vector2Int best = new Vector2Int((int)room.center.x, (int)room.center.y);
        float bestD = float.MaxValue;
        bool found = false;

        for (int x = room.xMin; x < room.xMax; x++)
            for (int y = room.yMin; y < room.yMax; y++)
            {
                var p = new Vector2Int(x, y);
                if (!layout.roomTiles.Contains(p)) continue;
                float d = (new Vector2(x, y) - center).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; found = true; }
            }
        return found ? best : best;
    }

    protected void CarvePath(Vector2Int from, Vector2Int to, DungeonLayout layout)
    {
        var cur = from;
        while (cur.x != to.x) { cur.x += (cur.x < to.x) ? 1 : -1; layout.floorTiles.Add(cur); }
        while (cur.y != to.y) { cur.y += (cur.y < to.y) ? 1 : -1; layout.floorTiles.Add(cur); }
    }

    protected void CreateCorridor(Vector2Int from, Vector2Int to, DungeonLayout layout)
    {
        var current = from;
        while (current.x != to.x) { current.x += current.x < to.x ? 1 : -1; layout.floorTiles.Add(current); }
        while (current.y != to.y) { current.y += current.y < to.y ? 1 : -1; layout.floorTiles.Add(current); }
    }

    protected Vector2Int NearestRoomTile(Vector2Int target, BSPNode subtree, DungeonLayout layout)
    {
        RectInt room = subtree.GetRoomBounds(0);
        Vector2Int best = target; float bestD = Mathf.Infinity;
        for (int x = room.xMin; x < room.xMax; x++)
            for (int y = room.yMin; y < room.yMax; y++)
            {
                var p = new Vector2Int(x, y);
                if (!layout.roomTiles.Contains(p)) continue;
                float d = (p - target).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
        return best;
    }

    protected BSPNode FindLeafForPoint(Vector2Int p) => FindLeafForPoint(root, p);
    protected BSPNode FindLeafForPoint(BSPNode n, Vector2Int p)
    {
        if (n == null) return null;
        if (!n.bounds.Contains(p)) return null;
        if (n.IsLeaf) return n;
        return FindLeafForPoint(n.left, p) ?? FindLeafForPoint(n.right, p);
    }

    // ---- Disjoint Set Union (DSU) ---- //
    protected sealed class DSU
    {
        int[] p, r;
        public DSU(int n) { p = new int[n]; r = new int[n]; for (int i = 0; i < n; i++) p[i] = i; }
        int Find(int x) { return p[x] == x ? x : (p[x] = Find(p[x])); }
        public bool Union(int a, int b)
        {
            a = Find(a); b = Find(b); if (a == b) return false;
            if (r[a] < r[b]) (a, b) = (b, a);
            p[b] = a; if (r[a] == r[b]) r[a]++;
            return true;
        }
    }

    // ---------------- Room carving ----------------
    protected void CarveRoom(RectInt roomBounds, DungeonLayout layout)
    {
        var kind = PickKind(roomWeights);
        switch (kind)
        {
            case RoomKind.Rectangle: FillRect(roomBounds, layout); break;
            case RoomKind.L: CarveL(roomBounds, layout); break;
            case RoomKind.T: CarveT(roomBounds, layout); break;
            case RoomKind.U: CarveU(roomBounds, layout); break;
            default: FillRect(roomBounds, layout); break;
        }
    }

    // TODO: optimise 
    protected RoomKind PickKind(RoomWeights w)
    {
        var pool = new List<(RoomKind, float)>
        {
            (RoomKind.Rectangle, w.rectangle),
            (RoomKind.L,         w.l),
            (RoomKind.T,         w.t),
            (RoomKind.U,         w.u),
        };

        float total = 0f;
        foreach (var kv in pool) total += kv.Item2;

        float r = Random.value * total;
        foreach (var kv in pool)
        {
            if ((r -= kv.Item2) <= 0f)
                return kv.Item1;
        }

        return RoomKind.Rectangle;
    }

    protected static void FillRect(RectInt r, DungeonLayout L)
    {
        for (int x = r.xMin; x < r.xMax; x++)
            for (int y = r.yMin; y < r.yMax; y++)
            {
                var p = new Vector2Int(x, y);
                L.roomTiles.Add(p);
                L.floorTiles.Add(p);
            }
    }

    void CarveL(RectInt b, DungeonLayout L)
    {
        const int MinThickness = 4;
        if (b.width < MinThickness || b.height < MinThickness)
        {
            FillRect(b, L);
            return;
        }

        int minDim = Mathf.Min(b.width, b.height);

        int t = Mathf.Clamp(minDim / 4, MinThickness, Mathf.Min(8, minDim - 1));

        int minW = Mathf.Min(b.width, Mathf.Max(MinThickness, b.width / 2));
        int minH = Mathf.Min(b.height, Mathf.Max(MinThickness, b.height / 2));
        int wA = (minW >= b.width) ? b.width : Random.Range(minW, b.width + 1);
        int hB = (minH >= b.height) ? b.height : Random.Range(minH, b.height + 1);

        bool right = Random.value < 0.5f;
        bool up = Random.value < 0.5f;

        int xH = right ? (b.xMax - wA) : b.xMin;
        int yH = up ? (b.yMax - t) : b.yMin;
        RectInt armH = new RectInt(xH, yH, wA, t);

        int xV = right ? (b.xMax - t) : b.xMin;
        int yV = up ? (b.yMax - hB) : b.yMin;
        RectInt armV = new RectInt(xV, yV, t, hB);

        FillRect(armH, L);
        FillRect(armV, L);
    }

    void CarveT(RectInt b, DungeonLayout L)
    {
        bool vertical = Random.value < 0.7f;
        if (vertical)
        {
            int stemW = Mathf.Max(3, b.width / Random.Range(3, 5));
            int stemX = b.xMin + (b.width - stemW) / 2;
            RectInt stem = new RectInt(stemX, b.yMin, stemW, Mathf.FloorToInt(b.height * Random.Range(0.5f, 0.75f)));

            int barH = Mathf.Max(3, b.height / Random.Range(6, 8));
            int barY = stem.yMax - barH / 2;
            RectInt bar = new RectInt(b.xMin, Mathf.Clamp(barY, b.yMin, b.yMax - barH), b.width, barH);

            FillRect(stem, L);
            FillRect(bar, L);
        }
        else
        {
            int stemH = Mathf.Max(3, b.height / Random.Range(3, 5));
            int stemY = b.yMin + (b.height - stemH) / 2;
            RectInt stem = new RectInt(b.xMin, stemY, Mathf.FloorToInt(b.width * Random.Range(0.5f, 0.75f)), stemH);

            int barW = Mathf.Max(3, b.width / Random.Range(6, 8));
            int barX = stem.xMax - barW / 2;
            RectInt bar = new RectInt(Mathf.Clamp(barX, b.xMin, b.xMax - barW), b.yMin, barW, b.height);

            FillRect(stem, L);
            FillRect(bar, L);
        }
    }
    void CarveU(RectInt b, DungeonLayout L)
    {
        // early return
        if (b.width < 6 || b.height < 6) { FillRect(b, L); return; }

        int t = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(b.width, b.height) * uThicknessRatio),
                            minUThickness, maxUThickness);

        int side = Random.Range(0, 4); // 0 up, 1 down, 2 left, 3 right

        RectInt legA, legB, baseBar;

        if (side == 0) // open UP => vertical legs + bottom base
        {
            legA = new RectInt(b.xMin, b.yMin, t, b.height);
            legB = new RectInt(b.xMax - t, b.yMin, t, b.height);
            baseBar = new RectInt(b.xMin, b.yMin, b.width, t);
        }
        else if (side == 1) // open DOWN => vertical legs + top base
        {
            legA = new RectInt(b.xMin, b.yMin, t, b.height);
            legB = new RectInt(b.xMax - t, b.yMin, t, b.height);
            baseBar = new RectInt(b.xMin, b.yMax - t, b.width, t);
        }
        else if (side == 2) // open LEFT => horizontal bars + right leg
        {
            legA = new RectInt(b.xMax - t, b.yMin, t, b.height);
            legB = legA;
            baseBar = new RectInt(b.xMin, b.yMin, b.width, t); // bottom

            FillRect(new RectInt(b.xMin, b.yMax - t, b.width, t), L);
        }
        else // open RIGHT => horizontal bars + left leg
        {
            legA = new RectInt(b.xMin, b.yMin, t, b.height);
            legB = legA; // placeholder
            baseBar = new RectInt(b.xMin, b.yMin, b.width, t); // bottom

            FillRect(new RectInt(b.xMin, b.yMax - t, b.width, t), L);
        }

        // Enforce minimum leg lengths so U room doesnt look like a corridor
        int minLegLenV = Mathf.RoundToInt(b.height * uMinLegLengthRatio);
        int minLegLenH = Mathf.RoundToInt(b.width * uMinLegLengthRatio);

        if (side == 0 || side == 1)
        {
            int extra = (b.height - minLegLenV) / 2;
            var legCrop = new RectInt(0, b.yMin + Mathf.Max(0, extra), 0, minLegLenV);

            RectInt A = new RectInt(legA.x, legCrop.y, legA.width, legCrop.height);
            RectInt B = new RectInt(legB.x, legCrop.y, legB.width, legCrop.height);
            FillRect(A, L);
            FillRect(B, L);
            FillRect(baseBar, L);
        }
        else // side == 2 or 3 (left/right open)
        {
            int extra = (b.width - minLegLenH) / 2;
            RectInt leg = (side == 2) ? new RectInt(b.xMax - t, b.yMin + Mathf.Max(0, (b.height - minLegLenV) / 2), t, minLegLenV)
                                      : new RectInt(b.xMin, b.yMin + Mathf.Max(0, (b.height - minLegLenV) / 2), t, minLegLenV);
            FillRect(leg, L);
            FillRect(baseBar, L);
        }

        // Area check: ensure U covers enough of the bounds; otherwise revert to rectangle
        int footprint = 0;
        for (int x = b.xMin; x < b.xMax; x++)
            for (int y = b.yMin; y < b.yMax; y++)
                if (L.roomTiles.Contains(new Vector2Int(x, y))) footprint++;

        float areaRatio = (float)footprint / (b.width * b.height);
        if (areaRatio < uMinAreaRatio)
        {
            for (int x = b.xMin; x < b.xMax; x++)
                for (int y = b.yMin; y < b.yMax; y++)
                    L.roomTiles.Remove(new Vector2Int(x, y));
            FillRect(b, L);
        }
    }

    // ---- Gyzmo ---- //
    void OnDrawGizmos()
    {
        if (LastLayout == null || LastLayout.layoutRooms == null) return;
        var rooms = LastLayout.layoutRooms;

        foreach (var room in rooms)
        {
            UnityEngine.Color color;
            switch (room.label)
            {
                case RoomLabel.Start: color = UnityEngine.Color.cyan; break;
                case RoomLabel.Boss: color = UnityEngine.Color.magenta; break;
                case RoomLabel.A: color = new UnityEngine.Color(0.2f, 0.6f, 1f); break;
                case RoomLabel.B: color = new UnityEngine.Color(0.2f, 1f, 0.6f); break;
                case RoomLabel.C: color = new UnityEngine.Color(1f, 0.8f, 0.2f); break;
                default: color = UnityEngine.Color.yellow; break;
            }

            Gizmos.color = color;

            Vector3 pos = room.worldPos + Vector3.up * 0.2f;
            Gizmos.DrawSphere(pos, 0.45f);
            Gizmos.DrawWireSphere(pos, 0.52f);

#if UNITY_EDITOR
            Handles.color = color;
            Handles.Label(pos + Vector3.up * 0.55f, $"{room.label} ({room.id})");
#endif
        }
    }


    protected struct Line2
    {
        public Vector2 n;   // unit normal
        public float d;     // offset so that n·p + d = 0

        public Line2(Vector2 normal, float offset)
        {
            n = normal.normalized;
            d = offset;
        }

        public static Line2 Vertical(float x) => new Line2(new Vector2(1f, 0f), -x); // x = constant
        public static Line2 Horizontal(float y) => new Line2(new Vector2(0f, 1f), -y); // y = constant

        public float SignedDistance(in Vector2 p) => Vector2.Dot(n, p) + d;
    }

    protected sealed class Poly2
    {
        public readonly List<Vector2> v = new List<Vector2>(8);

        public static Poly2 FromRect(RectInt r)
        {
            var p = new Poly2();
            // CCW rectangle (closed implicitly)
            p.v.Add(new Vector2(r.xMin, r.yMin));
            p.v.Add(new Vector2(r.xMax, r.yMin));
            p.v.Add(new Vector2(r.xMax, r.yMax));
            p.v.Add(new Vector2(r.xMin, r.yMax));
            return p;
        }

        public RectInt AABB()
        {
            float minx = float.PositiveInfinity, miny = float.PositiveInfinity;
            float maxx = float.NegativeInfinity, maxy = float.NegativeInfinity;
            foreach (var p in v)
            {
                if (p.x < minx) minx = p.x; if (p.x > maxx) maxx = p.x;
                if (p.y < miny) miny = p.y; if (p.y > maxy) maxy = p.y;
            }
            // RectInt is [xMin, xMax) style; clamp to grid
            int xi = Mathf.FloorToInt(minx + 0.0001f);
            int yi = Mathf.FloorToInt(miny + 0.0001f);
            int wi = Mathf.CeilToInt(maxx - 0.0001f) - xi;
            int hi = Mathf.CeilToInt(maxy - 0.0001f) - yi;
            wi = Mathf.Max(1, wi); hi = Mathf.Max(1, hi);
            return new RectInt(xi, yi, wi, hi);
        }
    }
}
