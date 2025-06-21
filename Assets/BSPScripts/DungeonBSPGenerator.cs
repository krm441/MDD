using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Procedural dungeon generator using Binary Space Partitioning (BSP).
/// Generates rooms, connects them via corridors, and populates the scene with
/// floors, walls, pillars, and doors. Also configures walkability for a pathfinding grid.
/// </summary>
[ExecuteInEditMode]
public class DungeonBSPGenerator : MonoBehaviour
{
    // ---------------- Configuration ----------------

    [Header("Dungeon Settings")]
    [Tooltip("Dimensions of the dungeon grid (in tiles).")]
    public Vector2Int dungeonSize = new Vector2Int(64, 64);

    [Tooltip("Minimum size for a room split.")]
    public int minRoomSize = 10;

    [Tooltip("Margin between room and its node bounds.")]
    public int margin = 2;

    [Tooltip("Random seed for consistent generation.")]
    public int seed = 1;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject pillarPrefab;
    public GameObject doorPrefab;

    [Header("Pathfinder")]
    [Tooltip("Reference to the pathfinding system.")]
    public Pathfinding.GridSystem pathFinder;

    [Tooltip("Parent transform for all dungeon objects.")]
    public Transform dungeonParent;

    // ---------------- Internal State ----------------

    private BSPNode root;
    public List<RectInt> finalRooms = new List<RectInt>();

    private HashSet<Vector2Int> placedTiles = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> roomTiles = new HashSet<Vector2Int>();

    // ---------------- Entry Point ----------------

    void Start()
    {
        GenerateFullDungeon();
    }

    /// <summary>
    /// Full pipeline for dungeon generation.
    /// </summary>
    [ContextMenu("Generate Dungeon")]
    public void GenerateFullDungeon()
    {
        ClearDungeon();

        Random.InitState(seed);
        root = new BSPNode(new RectInt(0, 0, dungeonSize.x, dungeonSize.y));
        finalRooms.Clear(); // clear any previous meshes if exist

        // pathfinder (A* or Theta*)
        if (pathFinder != null)
        {
            Debug.Log("Initializing pathfinder...");
            pathFinder.GeneratePathfinder(dungeonSize.x * 4, dungeonSize.y * 4);
        }
        else
        {
            Debug.LogError("PathFinder is NOT assigned in DungeonBSPGenerator!");
        }

        Split(root);
        CollectLeaves(root);
        ConnectRooms(root);
        BuildDungeonFromRooms();
        PlaceDoors(); 
        PlaceWalls();
        PlacePillars();
    }

    /// <summary>
    /// Removes previously generated dungeon objects.
    /// </summary>
    [ContextMenu("Remove Dungeon")]
    public void ClearDungeon()
    {
        if (dungeonParent == null) return;

        for (int i = dungeonParent.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(dungeonParent.GetChild(i).gameObject);
            Debug.Log("Dungeon Destroyed");
        }

        placedTiles.Clear();
    }

     // ---------------- BSP Processing ----------------

    void Split(BSPNode node)
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

    void CollectLeaves(BSPNode node)
    {
        if (node == null) return;
        if (node.IsLeaf)
        {
            finalRooms.Add(node.GetRoomBounds(margin));
        }
        else
        {
            CollectLeaves(node.left);
            CollectLeaves(node.right);
        }
    }

    void ConnectRooms(BSPNode node)
    {
        if (node == null || node.IsLeaf) return;

        if (node.left != null && node.right != null)
        {
            // Recursively ensure children have room centers
            ConnectRooms(node.left);
            ConnectRooms(node.right);

            Vector2Int centerA = node.left.GetClosestRoomCenter();
            Vector2Int centerB = node.right.GetClosestRoomCenter();

            CreateCorridor(centerA, centerB);
        }
    }

    void CreateCorridor(Vector2Int from, Vector2Int to)
    {
        Vector2Int current = from;

        while (current.x != to.x)
        {
            current.x += current.x < to.x ? 1 : -1;
            PlaceCorridorTile(current);
        }

        while (current.y != to.y)
        {
            current.y += current.y < to.y ? 1 : -1;
            PlaceCorridorTile(current);
        }
    }

    void PlaceWalls()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up,    // north
            Vector2Int.down,  // south
            Vector2Int.left,  // west
            Vector2Int.right  // east
        };

        foreach (Vector2Int tile in placedTiles)
        {
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = tile + dir;
                if (!placedTiles.Contains(neighbor))
                {
                    // Determine wall position and rotation based on direction
                    Vector3 offset = Vector3.zero;
                    Quaternion rotation = Quaternion.identity;

                    if (dir == Vector2Int.up)
                    {
                        offset = new Vector3(0f, 0f, 2f); // +Z half tile
                        rotation = Quaternion.Euler(0f, 0f, 0f); // default (length along Z)
                    }
                    else if (dir == Vector2Int.down)
                    {
                        offset = new Vector3(0f, 0f, -2f); // -Z
                        rotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else if (dir == Vector2Int.left)
                    {
                        offset = new Vector3(-2f, 0f, 0f); // -X
                        rotation = Quaternion.Euler(0f, 90f, 0f); // rotate to run along X
                    }
                    else if (dir == Vector2Int.right)
                    {
                        offset = new Vector3(2f, 0f, 0f); // +X
                        rotation = Quaternion.Euler(0f, 90f, 0f);
                    }

                    Vector3 wallPos = new Vector3(tile.x * 4f, 0f, tile.y * 4f) + offset;

                    GameObject wall = Instantiate(wallPrefab, wallPos, rotation, dungeonParent);
                    wall.name = $"Wall_{tile.x}_{tile.y}_{dir}";


                    if (pathFinder != null)
                    {
                        Vector2Int basePos = new Vector2Int(Mathf.FloorToInt(wallPos.x), Mathf.FloorToInt(wallPos.z));

                        // Mark 1x4 or 4x1 depending on direction
                        if (dir == Vector2Int.left || dir == Vector2Int.right)
                        {
                            for (int i = -2; i < 2; i++)
                            {
                                Vector2Int pos = basePos + new Vector2Int(0, i);
                                pathFinder.MarkWalkable(pos, false);
                            }
                        }
                        else // up/down
                        {
                            for (int i = -2; i < 2; i++)
                            {
                                Vector2Int pos = basePos + new Vector2Int(i, 0);
                                pathFinder.MarkWalkable(pos, false);
                            }
                        }
                    }

                }
            }
        }
    }

    void PlacePillars()
    {
        foreach (RectInt room in finalRooms)
        {
            // Get the four corners of the room
            Vector2Int[] corners = new Vector2Int[]
            {
                new Vector2Int(room.xMin, room.yMin),
                new Vector2Int(room.xMax, room.yMin),
                new Vector2Int(room.xMin, room.yMax),
                new Vector2Int(room.xMax, room.yMax)
            };

            foreach (var corner in corners)
            {
                // Pillars should sit precisely at the corner — center the mesh on the tile border
                Vector3 worldPos = new Vector3(corner.x * 4f - 2f, 0f, corner.y * 4f - 2f);
                GameObject pillar = Instantiate(pillarPrefab, worldPos, Quaternion.identity, dungeonParent);
                pillar.name = $"Pillar_{corner.x}_{corner.y}";
            }
        }
    }

    void PlaceDoors()
    {
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        HashSet<Vector2Int> doorPositions = new HashSet<Vector2Int>();

        foreach (var tile in placedTiles)
        {
            // Corridor tile only
            if (roomTiles.Contains(tile)) continue;

            foreach (var dir in directions)
            {
                Vector2Int neighbor = tile + dir;

                // If neighbor is a room tile and this tile is not, it’s a doorway
                if (roomTiles.Contains(neighbor))
                {
                    if (!doorPositions.Contains(tile))
                    {
                        Debug.Log("Placing doors ");
                        // Calculate world position of door
                        Vector3 offset = Vector3.zero;
                        Quaternion rotation = Quaternion.identity;

                        // Determine rotation and offset from direction
                        if (dir == Vector2Int.up)
                        {
                            rotation = Quaternion.Euler(0f, 0f, 0f);        // Facing forward (Z+)
                            offset = new Vector3(0f, 0f, 2f);               // Push forward
                        }
                        else if (dir == Vector2Int.down)
                        {
                            rotation = Quaternion.Euler(0f, 0f, 0f);        // Same rotation
                            offset = new Vector3(0f, 0f, -2f);              // Push backward
                        }
                        else if (dir == Vector2Int.left)
                        {
                            rotation = Quaternion.Euler(0f, 90f, 0f);       // Face left/right
                            offset = new Vector3(-2f, 0f, 0f);              // Push left
                        }
                        else if (dir == Vector2Int.right)
                        {
                            rotation = Quaternion.Euler(0f, 90f, 0f);       // Same rotation
                            offset = new Vector3(2f, 0f, 0f);               // Push right
                        }


                        Vector3 pos = new Vector3(tile.x * 4f, 0f, tile.y * 4f) + offset;
                        GameObject door = Instantiate(doorPrefab, pos, rotation, dungeonParent);
                        door.name = $"Door_{tile.x}_{tile.y}";
                        doorPositions.Add(tile);
                    }
                }
            }
        }
    }


    void BuildDungeonFromRooms()
    {
        foreach (var room in finalRooms)
        {
            for (int x = 0; x < room.width; x++)
            {
                for (int y = 0; y < room.height; y++)
                {
                    Vector2Int gridPos = new Vector2Int(room.x + x, room.y + y);
                    roomTiles.Add(gridPos); // Add this
                    placedTiles.Add(gridPos); // Ensure consistency

                    if (pathFinder != null)
                    {
                        Vector2Int basePos = gridPos * 4;
                        for (int k = 0; k < 4; k++)
                        {
                            for (int j = 0; j < 4; j++)
                            {
                                pathFinder.MarkWalkable(basePos + new Vector2Int(k - 1, j - 1), true);
                            }
                        }
                    }

                    Vector3 pos = new Vector3((room.x + x) * 4f, 0, (room.y + y) * 4f);
                    GameObject tile = Instantiate(floorPrefab, pos, Quaternion.identity, dungeonParent);
                    tile.name = $"Floor_{room.x + x}_{room.y + y}";

                    tile.layer = LayerMask.NameToLayer("PathGrid");
                }
            }
        }
    }

    void PlaceCorridorTile(Vector2Int gridPos)
    {
        if (placedTiles.Contains(gridPos)) return;

        placedTiles.Add(gridPos);

        if (pathFinder != null)
        {
            Vector2Int basePos = gridPos * 4;
            for (int k = 0; k < 4; k++)
            {
                for (int j = 0; j < 4; j++)
                {
                    pathFinder.MarkWalkable(basePos + new Vector2Int(k - 1, j - 1), true);
                }
            }
        }

        Vector3 pos = new Vector3(gridPos.x * 4f, 0, gridPos.y * 4f);
        GameObject tile = Instantiate(floorPrefab, pos, Quaternion.identity, dungeonParent);
        tile.name = $"Corridor_{gridPos.x}_{gridPos.y}";
        tile.layer = LayerMask.NameToLayer("PathGrid");
    }

}
