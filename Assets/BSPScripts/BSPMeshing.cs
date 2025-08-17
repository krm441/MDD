using System.Collections.Generic;
using UnityEngine;

public class BSPMeshing : MonoBehaviour
{
    [Header("Source")]
    public BSPLayoutGenerator generator;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject pillarPrefab;
    public GameObject doorPrefab;

    [Header("Parent")]
    public Transform dungeonParent;

    [Header("Pathfinder (optional)")]
    public Pathfinding.GridSystem pathFinder;

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
    void BuildFloors(DungeonLayout layout)
    {
        int pathLayer = LayerMask.NameToLayer("PathGrid");

        foreach (var tile in layout.floorTiles)
        {
            Vector3 pos = new Vector3(tile.x * TileScale, 0f, tile.y * TileScale);
            var go = Instantiate(floorPrefab, pos, Quaternion.identity, dungeonParent);
#if UNITY_EDITOR
            go.name = $"Floor_{tile.x}_{tile.y}";
#endif
            go.layer = pathLayer;

            if (pathFinder != null)
            {
                Vector2Int start = (tile * (int)TileScale) + new Vector2Int(-1, -1);
                for (int k = 0; k < 4; k++)
                    for (int j = 0; j < 4; j++)
                        pathFinder.MarkWalkable(start + new Vector2Int(k, j), true);
            }
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
}
