using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

public class RoomDecorator : MonoBehaviour
{
    public BSPLayoutGenerator generator;
    public Transform decorRoot;

    [Header("Closet placement")]

    [Tooltip("Closet prefabs to choose from at random.")]
    public GameObject[] closetPrefabs;

    [Tooltip("Maximum closets to try per room")] public int maxClosetsPerRoom = 3;
    [Tooltip("Keep this many tiles away from each other")] public int minClosetSeparationCells = 2;
    [Tooltip("Skip closets near door tiles by this many cells")] public int doorClearanceCells = 1;

    [Header("Closet wall snapping")]
    [Tooltip("Space between the back of a closet and the wall (world units)")]
    public float closetWallGap = 0.1f;
    [Tooltip("Extra factor for corners so diagonals don't slam into the corner.")]
    public float closetCornerSnapFactor = 0.7f; // 0.6–0.8 feels good

    [Header("Torch placement")]
    public GameObject torchPrefab;
    [SerializeField]
    private int torchPlacementStep = 3;

    [Tooltip("Keep this many tiles away from each other")] public int minTorchSeparationCells = 3;
    [Tooltip("Gap to wall for torches (world units)")] public float torchWallGap = 0.06f;
    [Tooltip("Corner factor for torches")] public float torchCornerSnapFactor = 0.85f;
    [Tooltip("World-space height to mount the torch at")] public float torchHeight = 1.8f;

    [ContextMenu("Generate")]
    public void Gen()
    {
        if (generator == null || generator.LastLayout == null)
        {
            Debug.LogWarning("RoomDecorator: generator or layout missing.");
            return;
        }
        if (closetPrefabs == null || closetPrefabs.Length == 0 || torchPrefab == null)
        {
            Debug.LogWarning("RoomDecorator: No closet or torch prefabs assigned.");
            return;
        }

        // clear previous  
        Clear();

        // 1) Build cells for each room (BSP)
        var layout = generator.LastLayout;
        var rooms = layout.layoutRooms; // logical rooms
        BuildCellsForAllRooms(generator, rooms);

        // Occupancy map so a tile is used by at most one thing (closet/torch)
        var occupied = BuildOccupiedMap(rooms);

        // 2) Place closets at random eligible wall tiles        
        PlaceClosets(layout, rooms, occupied);

        // 3) Place torches on walls (avoid tiles used by closets)
        PlaceTorches(layout, rooms, occupied);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {    
        var parent = decorRoot;
        for (int i = parent.childCount - 1; i >= 0; i--)
#if UNITY_EDITOR
            DestroyImmediate(parent.GetChild(i).gameObject);
#else
            Destroy(parent.GetChild(i).gameObject);
#endif
    }

    GameObject GetRandomClosetPrefab()
    {
        if (closetPrefabs != null && closetPrefabs.Length > 0)
        {
            // Try a few random picks to avoid a temp list alloc
            for (int attempt = 0; attempt < 4; attempt++)
            {
                int idx = Random.Range(0, closetPrefabs.Length);
                if (closetPrefabs[idx] != null) return closetPrefabs[idx];
            }
            // Fallback: linear search
            for (int i = 0; i < closetPrefabs.Length; i++)
                if (closetPrefabs[i] != null) return closetPrefabs[i];
        }
        Assert.IsFalse(true);
        return null;
    }

    void BuildCellsForAllRooms(BSPLayoutGenerator gen, List<Room> rooms)
    {
        var L = gen.LastLayout;
        if (L == null || L.rooms == null || rooms == null) return;

        for (int i = 0; i < rooms.Count && i < L.rooms.Count; i++)
        {
            var rect = L.rooms[i]; // BSP room i (RectInt)
            var room = rooms[i];
            room.cells = BSPRoomCellBuilder.BuildCellsForRoom(L, rect);
        }
    }

    Dictionary<int, HashSet<Vector2Int>> BuildOccupiedMap(List<Room> rooms)
    {
        var map = new Dictionary<int, HashSet<Vector2Int>>(rooms.Count);
        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i] != null) map[rooms[i].id] = new HashSet<Vector2Int>();
        }
        return map;
    }

    void PlaceClosets(DungeonLayout L, List<Room> rooms, Dictionary<int, HashSet<Vector2Int>> occupied)
    {
        float tileScale = generator != null ? generator.tileScale : 4f;
        float half = tileScale * 0.5f;
        
        var parent = decorRoot;

        for (int r = 0; r < rooms.Count; r++)
        {
            var room = rooms[r];
            if (room == null || room.cells == null || room.cells.Count == 0) continue;

            var inRoom = new HashSet<Vector2Int>();
            for (int i = 0; i < room.cells.Count; i++) inRoom.Add(room.cells[i].tile);

            var occ = occupied[room.id];

            // candidate cells: wall-edge, not doorway, honor door clearance, not already occupied
            var candidates = new List<Cell>(room.cells.Count);
            for (int i = 0; i < room.cells.Count; i++)
            {
                var c = room.cells[i];
                if (c.isWallEdge && !c.isDoorway && FarFromAnyDoor(c, room.cells, doorClearanceCells) && !occ.Contains(c.tile))
                    candidates.Add(c);
            }

            Shuffle(candidates);

            var placed = new List<Vector2Int>(maxClosetsPerRoom);
            for (int i = 0; i < candidates.Count && placed.Count < maxClosetsPerRoom; i++)
            {
                var cell = candidates[i];
                if (!IsFarFromPlaced(cell.tile, placed, minClosetSeparationCells)) continue;

                // Compute outward normal (toward non-room neighbours), face into room
                Vector3 outward = ComputeOutwardNormal(cell.tile, inRoom);
                if (outward == Vector3.zero) outward = Vector3.forward; // fallback

                int outwardAxes = (Mathf.Abs(outward.x) > 0.5f ? 1 : 0) + (Mathf.Abs(outward.z) > 0.5f ? 1 : 0);
                float snap = (outwardAxes >= 2) ? (half - closetWallGap) * closetCornerSnapFactor
                                                : (half - closetWallGap);

                Vector3 posWorld = cell.position + outward.normalized * snap;
                posWorld.y = 0f;
                Quaternion rotWorld = Quaternion.LookRotation(-outward, Vector3.up); // face into room

                var prefab = GetRandomClosetPrefab();
                if (prefab == null) continue;

                // Respect explicit local placement contract
                Vector3 pos = cell.position + outward.normalized * snap;
                pos.y = 0;
                Quaternion rot = Quaternion.LookRotation(-outward, Vector3.up); // face into room


                //var parent = decorRoot != null ? decorRoot : transform;
                //var go = Instantiate(closetPrefab, decorRoot); //pos, rot, 
                //go.transform.localPosition = pos;
                //go.transform.localRotation = rot;
                var go = Instantiate(prefab, decorRoot); // pos, rot
                go.transform.localPosition = pos;
                go.transform.localRotation = rot;
                go.name = $"Closet_R{room.id}_{cell.tile.x}_{cell.tile.y}";

                placed.Add(cell.tile);
                occ.Add(cell.tile); // mark tile as used
            }
        }
    }

    public void PlaceTorches(DungeonLayout L, List<Room> rooms, Dictionary<int, HashSet<Vector2Int>> occupied)
    {
        if (torchPrefab == null) return;

        float tileScale = generator != null ? generator.tileScale : 4f;
        float half = tileScale * 0.5f;
        var parent = decorRoot; // explicit local placement target
        int step = Mathf.Max(1, torchPlacementStep);

        for (int r = 0; r < rooms.Count; r++)
        {
            var room = rooms[r];
            if (room == null || room.cells == null || room.cells.Count == 0) continue;

            var inRoom = new HashSet<Vector2Int>();
            for (int i = 0; i < room.cells.Count; i++) inRoom.Add(room.cells[i].tile);

            var occ = occupied[room.id];

            // wall tiles excluding doorways and any occupied tile (e.g., closets)
            var candidates = new List<Cell>(room.cells.Count);
            for (int i = 0; i < room.cells.Count; i++)
            {
                var c = room.cells[i];
                if (c.isWallEdge && !c.isDoorway && !occ.Contains(c.tile)) candidates.Add(c);
            }

            // deterministic ordering
            candidates.Sort((a, b) =>
            {
                int by = a.tile.y.CompareTo(b.tile.y);
                return by != 0 ? by : a.tile.x.CompareTo(b.tile.x);
            });

            int placedCount = 0;
            for (int i = 0; i < candidates.Count; i += step)
            {
                //if (placedCount >= maxTorchesPerRoom) break;
                var cell = candidates[i];

                Vector3 outward = ComputeOutwardNormal(cell.tile, inRoom);
                if (outward == Vector3.zero) outward = Vector3.forward;

                int outwardAxes = (Mathf.Abs(outward.x) > 0.5f ? 1 : 0) + (Mathf.Abs(outward.z) > 0.5f ? 1 : 0);
                float snap = (outwardAxes >= 2) ? (half - torchWallGap) * torchCornerSnapFactor
                                                : (half - torchWallGap);

                Vector3 pos = cell.position + outward.normalized * snap;
                pos.y = torchHeight;
                Quaternion rot = Quaternion.LookRotation(-outward, Vector3.up);

                var go = Instantiate(torchPrefab, decorRoot); // pos, rot
                go.transform.localPosition = pos;
                go.transform.localRotation = rot;

                occ.Add(cell.tile); // keep tiles unique across props
                placedCount++;
            }
        }
    }

    // --- helpers --- //
    static bool IsFarFromPlaced(Vector2Int tile, List<Vector2Int> already, int minSep)
    {
        for (int i = 0; i < already.Count; i++)
        {
            if (Manhattan(tile, already[i]) < minSep) return false;
        }
        return true;
    }

    static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    static bool FarFromAnyDoor(Cell c, List<Cell> cells, int clearance)
    {
        if (clearance <= 0) return true;
        for (int i = 0; i < cells.Count; i++)
        {
            var other = cells[i];
            if (!other.isDoorway) continue;
            if (Manhattan(c.tile, other.tile) <= clearance) return false;
        }
        return true;
    }

    static Vector3 ComputeOutwardNormal(Vector2Int tile, HashSet<Vector2Int> inRoom)
    {
        int sx = 0, sy = 0;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        for (int i = 0; i < 4; i++)
        {
            var d = dirs[i];
            var n = tile + d;
            if (!inRoom.Contains(n)) { sx += d.x; sy += d.y; }
        }
        Vector2 sum = new Vector2(sx, sy);
        if (sum.sqrMagnitude < 1e-4f) return Vector3.zero;
        sum.Normalize();
        // map XY (grid) to XZ (world)
        return new Vector3(sum.x, 0f, sum.y);
    }

    static void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

}
