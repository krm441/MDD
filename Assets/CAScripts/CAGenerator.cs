using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CAGenerator : MonoBehaviour
{
    [Range(0,100)]
	public int randomFillPercent = 60;

    public int iterationsCA = 3;

	int[,] map;

    public int mapHeight = 64, mapWidth = 64;


    private const int floor = 0;
    private const int wall = 1;

    public int seed = 1;

    // path
    public Pathfinding.GridSystem pathFinder;

    [SerializeField] private CAMeshing caMeshing;

    // Start is called before the first frame update
    void Start()
    {
        //GenerateDungeon();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Generate Dungeon")]
    public void GenerateDungeon()
    {
        // init seed
        Random.InitState(seed);

        // init map
        map = new int[mapHeight, mapWidth];
        regionTiles = new Dictionary<int, List<Vector2Int>>();
        currentRegion = 0;

        MakeNoiseGrid(randomFillPercent);
        GenerateCA(iterationsCA);
        IdentifyRegions();


        if (pathFinder != null)
        {
            Debug.Log("generating a*");
            pathFinder.GeneratePathfinder(mapWidth, mapHeight);

            //var start = pathFinder.GetNodeFromWorldPosition(new Vector3(0, 0));
            //var finish = pathFinder.GetNodeFromWorldPosition(new Vector3(30, 30));


            // this is ussed to conenct the separated corridors to one another. this is not the pathfinder for walking of the party
            ConnectRegionsWithAStar(pathFinder);
        }

        UnityEditor.SceneView.RepaintAll(); // Editor mode only
        
        caMeshing?.GenerateMeshes(map);
    }

    [ContextMenu("Remove Dungeon")]
    public void ClearDungeon()
    {
        caMeshing?.ClearPrevious();
    }

    void MakeNoiseGrid(int density)
    {
        for(int i = 0; i < mapHeight; i++)
        {
            for(int j = 0; j < mapWidth; j++)
            {
                int random = Random.Range(1, 100);
                if(random > density)
                    map[i, j] = floor;
                else
                    map[i, j] = wall;
            }
        }
    }

    void GenerateCA(int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            int[,] tempGrid = map.Clone() as int[,]; // create a copy

            for (int j = 0; j < mapHeight; j++)
            {
                for (int k = 0; k < mapWidth; k++)
                {
                    int neighborWallCount = 0;

                    for (int y = j - 1; y <= j + 1; y++)
                    {
                        for (int x = k - 1; x <= k + 1; x++)
                        {
                            if (IsWithinMapBounds(x, y))
                            {
                                if (x != k || y != j)
                                {
                                    if (tempGrid[y, x] == wall)
                                        neighborWallCount++;
                                }
                            }
                            else
                            {
                                // Treat out-of-bounds as wall
                                neighborWallCount++;
                            }
                        }
                    }

                    map[j, k] = (neighborWallCount > 4) ? wall : floor;
                }
            }
        }
    }

    bool IsWithinMapBounds(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }

    // ---- Room Connections ----

    // Each region has its own list of floor tile positions
    Dictionary<int, List<Vector2Int>> regionTiles = new Dictionary<int, List<Vector2Int>>();
    int[,] regionMap;
    int currentRegion = 0;

    void IdentifyRegions()
    {
        regionMap = new int[mapHeight, mapWidth];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (map[y, x] == floor && regionMap[y, x] == 0)
                {
                    currentRegion++;
                    List<Vector2Int> region = new List<Vector2Int>();
                    FloodFillRegion(x, y, currentRegion, region);
                    regionTiles[currentRegion] = region;
                }
            }
        }
    }

    void FloodFillRegion(int startX, int startY, int regionId, List<Vector2Int> region)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        regionMap[startY, startX] = regionId;

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            region.Add(current);

            foreach (var dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (IsWithinMapBounds(neighbor.x, neighbor.y) &&
                    map[neighbor.y, neighbor.x] == floor &&
                    regionMap[neighbor.y, neighbor.x] == 0)
                {
                    regionMap[neighbor.y, neighbor.x] = regionId;
                    queue.Enqueue(neighbor);
                }
            }
        }
    }


    void ConnectRegionsWithAStar(Pathfinding.GridSystem gridSystem)
    {
        if (regionTiles.Count <= 1) return;

        // 1) Temporarily mark all tiles walkable to let A* tunnel through walls
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                gridSystem.MarkWalkable(new Vector2Int(x, y), true);

        var aStar = gridSystem.aStar;

        List<List<Vector2Int>> regions = new List<List<Vector2Int>>(regionTiles.Values);
        HashSet<(int, int)> connectedPairs = new HashSet<(int, int)>();

        const float connectionThreshold = 8f; // Only connect regions closer than this

        for (int i = 0; i < regions.Count; i++)
        {
            for (int j = i + 1; j < regions.Count; j++)
            {
                float bestDist = float.MaxValue;
                Vector2Int bestA = Vector2Int.zero, bestB = Vector2Int.zero;

                foreach (var a in regions[i])
                    foreach (var b in regions[j])
                    {
                        float dist = Vector2Int.Distance(a, b);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestA = a;
                            bestB = b;
                        }
                    }

                if (bestDist > connectionThreshold) continue;
                if (connectedPairs.Contains((i, j)) || connectedPairs.Contains((j, i))) continue;

                var start = gridSystem.GetNodeFromWorldPosition(new Vector3(bestA.x, 0f, bestA.y));
                var end = gridSystem.GetNodeFromWorldPosition(new Vector3(bestB.x, 0f, bestB.y));

                if (start != null && end != null)
                {
                    var path = aStar.FindPath(start, end);
                    if (path != null && path.Count > 0)
                    {
                        foreach (var node in path)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                                for (int dy = -1; dy <= 1; dy++)
                                {
                                    var p = node.gridPos + new Vector2Int(dx, dy);
                                    if (IsWithinMapBounds(p.x, p.y))
                                    {
                                        map[p.y, p.x] = floor;
                                        gridSystem.MarkWalkable(p, true);
                                    }
                                }
                        }
                        connectedPairs.Add((i, j));
                    }
                }
            }
        }

        // 2) Restore unwalkable walls
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                if (map[y, x] == wall)
                    gridSystem.MarkWalkable(new Vector2Int(x, y), false);
    }





    Vector2Int FindSafeFloorTile(List<Vector2Int> region)
    {
        foreach (var tile in region)
        {
            if (map[tile.y, tile.x] == floor)
                return tile;
        }

        return region[0]; // fallback (should never be wall due to flood-fill logic)
    }



    // DEBUG - visual debugging 
    public float tileSize = 1f; // size of each cell for visualization

    void OnDrawGizmos()
    {
        if (map == null) return;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0f, y * tileSize);

                if (map[y, x] == floor)
                    Gizmos.color = Color.green;
                else
                    Gizmos.color = Color.red;

                Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.9f);
            }
        }
    }

}
