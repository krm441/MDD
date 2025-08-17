using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DelaunatorSharp;
using System.Drawing;


[System.Serializable]
public class DungeonLayout
{
    public List<RectInt> rooms = new List<RectInt>();
    public HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
    public HashSet<Vector2Int> roomTiles = new HashSet<Vector2Int>();
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
    public int margin = 2;
    public int seed = 1;

    [Header("Room Shapes")]
    public RoomWeights roomWeights = new RoomWeights();

    [Header("U-Shape Constraints")]
    [Range(2, 8)] public int minUThickness = 3;
    [Range(2, 12)] public int maxUThickness = 6;
    [Range(0.15f, 0.45f)] public float uThicknessRatio = 0.25f;
    [Range(0.4f, 0.9f)] public float uMinLegLengthRatio = 0.65f;
    [Range(0.20f, 0.90f)] public float uMinAreaRatio = 0.35f;

    protected BSPNode root;
    public DungeonLayout LastLayout { get; protected set; }

    public void Generate()
    {
        Random.InitState(seed);
        root = new BSPNode(new RectInt(0, 0, dungeonSize.x, dungeonSize.y));

        var layout = new DungeonLayout();
        Split(root);
        CollectLeaves(root, layout);

        ConnectStrategy(layout);

        LastLayout = layout;
        Console.Log(GetType().Name, ":", layout.rooms.Count, "rooms", layout.floorTiles.Count, "tiles.");
    }

    protected abstract void ConnectStrategy(DungeonLayout layout);

    // ---------------- BSP  ---------------- //
    protected void Split(BSPNode node)
    {
        if (node.bounds.width < minRoomSize * 2 && node.bounds.height < minRoomSize * 2)
            return;

        bool splitHorizontally = node.bounds.width < node.bounds.height;
        if (splitHorizontally)
        {
            int splitY = Random.Range(minRoomSize, node.bounds.height - minRoomSize);
            node.left = new BSPNode(new RectInt(node.bounds.x, node.bounds.y, node.bounds.width, splitY));
            node.right = new BSPNode(new RectInt(node.bounds.x, node.bounds.y + splitY, node.bounds.width, node.bounds.height - splitY));
        }
        else
        {
            int splitX = Random.Range(minRoomSize, node.bounds.width - minRoomSize);
            node.left = new BSPNode(new RectInt(node.bounds.x, node.bounds.y, splitX, node.bounds.height));
            node.right = new BSPNode(new RectInt(node.bounds.x + splitX, node.bounds.y, node.bounds.width - splitX, node.bounds.height));
        }

        Split(node.left);
        Split(node.right);
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
}
