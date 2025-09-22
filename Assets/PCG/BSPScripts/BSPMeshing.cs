using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class BSPMeshing : MonoBehaviour
{
    [Header("Source")]
    public BSPLayoutGenerator generator;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject pillarPrefab;
    public GameObject doorPrefab;

    [Header("Mesh rotation options")]
    [SerializeField]private bool rotateFloorRandom = true;

    [Header("Parent")]
    public Transform dungeonParent;

    //[Header("Pathfinder (obsolete)")]
    //public Pathfinding.GridSystem pathFinder;

    [Header("Room Trimming")]
    public int trimToRoomCount = 0;
    // Corridor prune toggles
    //[Tooltip("Prune unused/orphan corridors after room trimming.")]
    private bool pruneCorridors = true;
    //[Tooltip("Protect the Start↔Boss path from pruning.")]
    private bool protectMainSpine = true;

    // Grid constants
    private const float TileScale = 4f;
    private const float Half = TileScale * 0.5f;

    // One directions array - pool
    private static readonly Vector2Int[] CardinalDirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    // Endpoint mask: bit1 = horizontal wall endpoint, bit2 = vertical wall endpoint -> 3 = corner
    private readonly Dictionary<Vector2Int, byte> endpointMask = new Dictionary<Vector2Int, byte>();

    [ContextMenu("Rebuild From Layout")]
    public void Rebuild()
    {
        if (generator == null || generator.LastLayout == null)
        {
            return;
        }

        Clear();

        if (trimToRoomCount > 0)
        {
            TryTrimRooms(generator.LastLayout, Mathf.Max(1, trimToRoomCount));
        }

        BuildFloors(generator.LastLayout);
        PlaceDoors(generator.LastLayout);
        PlaceWalls(generator.LastLayout);
        PlacePillars(generator.LastLayout);
    }

    [ContextMenu("Clear")]
    public void Clear()
    {
        endpointMask.Clear();

        if (dungeonParent == null) return;
        for (int i = dungeonParent.childCount - 1; i >= 0; i--)
            DestroyImmediate(dungeonParent.GetChild(i).gameObject);
    }

    // ---------------- Build ----------------
    // Allowed Y-rotations (degrees)
    private static readonly float[] RotChoices = { 0f, 90f, -90f, 180f };
    void BuildFloors(DungeonLayout layout)
    {
        //int pathLayer = LayerMask.NameToLayer("PathGrid");

        foreach (var tile in layout.floorTiles)
        {
            Vector3 pos = new Vector3(tile.x * TileScale, 0f, tile.y * TileScale);

            // Rotate if enabled - for smoother look
            float angle = rotateFloorRandom
                ? RotChoices[UnityEngine.Random.Range(0, RotChoices.Length)]
                : 0f;

            Quaternion rot = Quaternion.Euler(0f, angle, 0f);

            //var go = Instantiate(floorPrefab, pos, Quaternion.identity, dungeonParent);
            var go = Instantiate(floorPrefab, pos, rot, dungeonParent);
#if UNITY_EDITOR
            go.name = $"Floor_{tile.x}_{tile.y}";
#endif
            //go.layer = pathLayer;
            NavMeshManager.AddFloorToNavMeshLayer(go);

            // mesh collider
            go.AddComponent<MeshCollider>();

            //if (pathFinder != null)
            //{
            //    Vector2Int start = (tile * (int)TileScale) + new Vector2Int(-1, -1);
            //    for (int k = 0; k < 4; k++)
            //        for (int j = 0; j < 4; j++)
            //            pathFinder.MarkWalkable(start + new Vector2Int(k, j), true);
            //}
        }
    }

    void PlaceWalls(DungeonLayout layout)
    {
        foreach (var tile in layout.floorTiles)
        {
            foreach (var dir in CardinalDirs)
            {
                var n = tile + dir;

                if (!layout.floorTiles.Contains(n))
                {
                    Vector3 basePos = new Vector3(tile.x * TileScale, 0f, tile.y * TileScale);

                    Vector3 offset = new Vector3(dir.x * Half, 0f, dir.y * Half);
                    float yaw = (dir.x != 0) ? 90f : 0f;
                    bool isHorizontal = (dir.y != 0);

                    Vector3 wallPos = basePos + offset;
                    var rot = Quaternion.Euler(0f, yaw, 0f);

                    var wall = Instantiate(wallPrefab, wallPos, rot, dungeonParent);
#if UNITY_EDITOR
                    wall.name = $"Wall_{tile.x}_{tile.y}_{dir}";
#endif

                    // Pillar endpoint masking
                    if (isHorizontal)
                    {
                        Vector2Int a = new Vector2Int(Mathf.RoundToInt(wallPos.x - Half), Mathf.RoundToInt(wallPos.z));
                        Vector2Int b = new Vector2Int(Mathf.RoundToInt(wallPos.x + Half), Mathf.RoundToInt(wallPos.z));
                        AddEndpointMask(a, 1); AddEndpointMask(b, 1); // 1 = H
                    }
                    else
                    {
                        Vector2Int a = new Vector2Int(Mathf.RoundToInt(wallPos.x), Mathf.RoundToInt(wallPos.z - Half));
                        Vector2Int b = new Vector2Int(Mathf.RoundToInt(wallPos.x), Mathf.RoundToInt(wallPos.z + Half));
                        AddEndpointMask(a, 2); AddEndpointMask(b, 2); // 2 = V
                    }

                    /* DEPRECATED - using nav mesh instead
                    if (pathFinder != null)
                    {
                        Vector2Int baseInt = new Vector2Int(Mathf.FloorToInt(wallPos.x), Mathf.FloorToInt(wallPos.z));
                        if (dir == Vector2Int.left || dir == Vector2Int.right)
                        {
                            for (int i = -2; i < 2; i++)
                                pathFinder.MarkWalkable(baseInt + new Vector2Int(0, i), false);
                        }
                        else
                        {
                            for (int i = -2; i < 2; i++)
                                pathFinder.MarkWalkable(baseInt + new Vector2Int(i, 0), false);
                        }
                    }*/
                }
            }
        }
    }

    void AddEndpointMask(Vector2Int p, byte bit)
    {
        if (endpointMask.TryGetValue(p, out byte mask))
            endpointMask[p] = (byte)(mask | bit);
        else
            endpointMask[p] = bit;
    }

    void PlacePillars(DungeonLayout layout)
    {
        foreach (var kv in endpointMask)
        {
            if (kv.Value != 3) continue;

            Vector3 pos = new Vector3(kv.Key.x, 0f, kv.Key.y);
            var pillar = Instantiate(pillarPrefab, pos, Quaternion.identity, dungeonParent);
#if UNITY_EDITOR
            pillar.name = $"Pillar_{kv.Key.x}_{kv.Key.y}";
#endif
        }
    }

    void PlaceDoors(DungeonLayout layout)
    {
        foreach (var tile in layout.floorTiles)
        {
            if (layout.roomTiles.Contains(tile)) continue;

            foreach (var dir in CardinalDirs)
            {
                if (!layout.roomTiles.Contains(tile + dir)) continue;

                float yaw = (dir.x != 0) ? 90f : 0f;
                Vector3 offset = new Vector3(dir.x * Half, 0f, dir.y * Half);

                Vector3 pos = new Vector3(tile.x * TileScale, 0f, tile.y * TileScale) + offset;
                var rot = Quaternion.Euler(0f, yaw, 0f);

                var door = Instantiate(doorPrefab, pos, rot, dungeonParent);
#if UNITY_EDITOR
                door.name = $"Door_{tile.x}_{tile.y}";
#endif
                break;
            }
        }
    }

    //trimming
    void TryTrimRooms(DungeonLayout L, int targetCount)
    {
        if (L == null || L.rooms == null || L.rooms.Count == 0 || L.roomTiles.Count == 0)
            return;

        // Locate start & boss room centers in tile coords
        var start = generator.startRoom; // assigned by generator
        var boss = generator.bossRoom;
        if (start == null || boss == null)
            return;

        // Room ids correspond to layout index (set by generator)
        int startId = start.id;
        int bossId = boss.id;
        if (startId < 0 || startId >= L.roomCenters.Count || bossId < 0 || bossId >= L.roomCenters.Count)
            return;

        Vector2Int startTile = L.roomCenters[startId];
        Vector2Int bossTile = L.roomCenters[bossId];

        // 1) gather rooms on the shortest walkable path between boss and start
        var path = FindTilePath(startTile, bossTile, L);
        var selected = RoomsTouchingPath(path, L);

        // 2) if we already have enough, trim others and finish; else 3) add nearest rooms until targetCount
        if (selected.Count < targetCount)
        {
            AddNearestRooms(selected, path, L, targetCount);
        }

        // Apply trimming: remove all tiles of rooms NOT selected
        TrimUnselectedRooms(L, selected);

        if (pruneCorridors)
        {
            var protect = protectMainSpine ? new HashSet<Vector2Int>(path) : new HashSet<Vector2Int>();
            CullOrphanCorridors(L, protect);
            PeelDeadEnds(L, protect);
        }
    }

    List<Vector2Int> FindTilePath(Vector2Int a, Vector2Int b, DungeonLayout L)
    {
        // BFS over L.floorTiles (rooms + corridors); return the path tiles (including endpoints)
        if (a == b) return new List<Vector2Int> { a };
        var q = new Queue<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        var prev = new Dictionary<Vector2Int, Vector2Int>();

        if (!L.floorTiles.Contains(a) || !L.floorTiles.Contains(b))
            return new List<Vector2Int>();

        q.Enqueue(a); seen.Add(a);
        Vector2Int[] dirs = CardinalDirs;

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (p == b) break;
            foreach (var d in dirs)
            {
                var n = p + d;
                if (seen.Contains(n)) continue;
                if (!L.floorTiles.Contains(n)) continue;
                seen.Add(n);
                prev[n] = p;
                q.Enqueue(n);
            }
        }

        if (!prev.ContainsKey(b) && a != b) return new List<Vector2Int>();

        var path = new List<Vector2Int>();
        var cur = b;
        path.Add(cur);
        while (cur != a && prev.TryGetValue(cur, out var p2))
        {
            cur = p2;
            path.Add(cur);
        }
        path.Reverse();
        return path;
    }

    HashSet<int> RoomsTouchingPath(List<Vector2Int> path, DungeonLayout L)
    {
        var selected = new HashSet<int>();
        if (path == null || path.Count == 0) return selected;

        // Build tile->room map once for fast lookup
        var tileToRoom = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < L.rooms.Count; i++)
        {
            var r = L.rooms[i];
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    var p = new Vector2Int(x, y);
                    if (L.roomTiles.Contains(p)) tileToRoom[p] = i;
                }
        }

        foreach (var t in path)
        {
            if (tileToRoom.TryGetValue(t, out int id))
                selected.Add(id);
        }
        return selected;
    }

    void AddNearestRooms(HashSet<int> selected, List<Vector2Int> path, DungeonLayout L, int targetCount)
    {
        if (selected.Count >= targetCount) return;
        // Precompute list of candidate room ids not yet selected
        var candidates = new List<int>();
        for (int i = 0; i < L.rooms.Count; i++) if (!selected.Contains(i)) candidates.Add(i);
        if (candidates.Count == 0) return;

        // For each candidate, compute min squared distance from room center to any path tile
        var scored = new List<(int id, int score)>();
        foreach (var id in candidates)
        {
            Vector2Int c = L.roomCenters[id];
            int best = int.MaxValue;
            foreach (var t in path)
            {
                int dx = c.x - t.x; int dy = c.y - t.y;
                int d2 = dx * dx + dy * dy;
                if (d2 < best) best = d2;
                if (best == 0) break;
            }
            scored.Add((id, best));
        }
        scored.Sort((a, b) => a.score.CompareTo(b.score));

        foreach (var (id, _) in scored)
        {
            selected.Add(id);
            if (selected.Count >= targetCount) break;
        }
    }

    void TrimUnselectedRooms(DungeonLayout L, HashSet<int> keep)
    {
        var toRemove = new List<Vector2Int>();
        for (int i = 0; i < L.rooms.Count; i++)
        {
            if (keep.Contains(i)) continue;
            var r = L.rooms[i];
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    var p = new Vector2Int(x, y);
                    if (L.roomTiles.Contains(p)) toRemove.Add(p);
                }
        }

        foreach (var p in toRemove)
        {
            L.roomTiles.Remove(p);
            L.floorTiles.Remove(p);
        }
    }

    // remove orphaned coridors
    void CullOrphanCorridors(DungeonLayout L, HashSet<Vector2Int> protect)
    {
        // Corridor set = floor - rooms
        var corridor = new HashSet<Vector2Int>(L.floorTiles);
        corridor.ExceptWith(L.roomTiles);
        if (corridor.Count == 0) return;

        // Seeds = corridor tiles that touch kept rooms OR in protect
        var seeds = new Queue<Vector2Int>();
        var seen = new HashSet<Vector2Int>();

        foreach (var r in L.roomTiles)
        {
            foreach (var d in CardinalDirs)
            {
                var n = r + d;
                if (corridor.Contains(n) && seen.Add(n))
                    seeds.Enqueue(n);
            }
        }
        foreach (var p in protect)
        {
            if (corridor.Contains(p) && seen.Add(p))
                seeds.Enqueue(p);
        }

        // Flood across corridor tiles
        while (seeds.Count > 0)
        {
            var t = seeds.Dequeue();
            foreach (var d in CardinalDirs)
            {
                var n = t + d;
                if (!corridor.Contains(n)) continue;
                if (seen.Add(n)) seeds.Enqueue(n);
            }
        }

        // Remove any corridor tile not reached
        if (seen.Count == corridor.Count) return;
        foreach (var c in corridor)
            if (!seen.Contains(c))
                L.floorTiles.Remove(c);
    }

    void PeelDeadEnds(DungeonLayout L, HashSet<Vector2Int> protect)
    {
        // Work on corridor-only graph; preserve tiles adjacent to rooms and protected tiles
        var corridor = new HashSet<Vector2Int>(L.floorTiles);
        corridor.ExceptWith(L.roomTiles);
        if (corridor.Count == 0) return;

        // Precompute adjacency counts among corridor tiles
        var degree = new Dictionary<Vector2Int, int>(corridor.Count);
        var hasRoomNeighbor = new HashSet<Vector2Int>();
        foreach (var t in corridor)
        {
            int deg = 0; bool touchesRoom = false;
            foreach (var d in CardinalDirs)
            {
                var n = t + d;
                if (corridor.Contains(n)) deg++;
                if (!touchesRoom && L.roomTiles.Contains(n)) touchesRoom = true;
            }
            degree[t] = deg;
            if (touchesRoom) hasRoomNeighbor.Add(t);
        }

        var q = new Queue<Vector2Int>();
        foreach (var t in corridor)
        {
            if (protect.Contains(t)) continue;
            if (hasRoomNeighbor.Contains(t)) continue; // keep entrance tiles
            if (degree[t] <= 1) q.Enqueue(t);
        }

        while (q.Count > 0)
        {
            var t = q.Dequeue();
            if (!corridor.Contains(t)) continue; // might be already removed
            if (protect.Contains(t)) continue;
            if (hasRoomNeighbor.Contains(t)) continue;

            // Recompute degree on demand
            int deg = 0;
            foreach (var d in CardinalDirs)
            {
                if (corridor.Contains(t + d)) deg++;
            }
            if (deg > 1) continue; // no longer a leaf

            // Remove tile from both corridor and floor
            corridor.Remove(t);
            L.floorTiles.Remove(t);

            // Update neighbors
            foreach (var d in CardinalDirs)
            {
                var n = t + d;
                if (!corridor.Contains(n)) continue;
                if (protect.Contains(n)) continue;
                if (hasRoomNeighbor.Contains(n)) continue;
                // recompute neighbor degree cheaply
                int ndeg = 0;
                foreach (var d2 in CardinalDirs)
                    if (corridor.Contains(n + d2)) ndeg++;
                if (ndeg <= 1) q.Enqueue(n);
            }
        }
    }

}
