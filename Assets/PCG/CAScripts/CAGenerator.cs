using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class CAGenerator : MonoBehaviour, IDungeon
{
    [Range(0,100)]
	public int randomFillPercent = 60;

    public int iterationsCA = 3;

	int[,] map;

    public int mapHeight = 64, mapWidth = 64;


    private const int floor = 0;
    private const int wall = 1;

    public int seed = 1;

    private Room startRoom, bossRoom;

    // path
    public Pathfinding.GridSystem pathFinder;

    [SerializeField] private CAMeshing caMeshing;

    public void Generate(int seed)
    {
        this.seed = seed;
        Generate();
    }

    [ContextMenu("Generate Dungeon")]
    public void Generate()
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
        //AssignRoomLabels();

        if (pathFinder != null)
        {
            Debug.Log("generating a*");
            pathFinder.GeneratePathfinder(mapWidth, mapHeight);

            //var start = pathFinder.GetNodeFromWorldPosition(new Vector3(0, 0));
            //var finish = pathFinder.GetNodeFromWorldPosition(new Vector3(30, 30));


            // this is ussed to conenct the separated corridors to one another. this is not the pathfinder for walking of the party
            ConnectRegionsWithAStar(pathFinder);

            DetectRoomsByThickness();    // builds rooms list + centroids
            AssignStartAndBoss();        // labels the farthest pair
        }
                
        caMeshing.GenerateMeshes(map);               
    }

    [ContextMenu("Remove Dungeon")]
    public void Clean()
    {
        caMeshing?.ClearPrevious();
    }

    public Room GetPlayerStart() => startRoom;
    public Room GetBossLocation() => bossRoom;

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

    int[,] ComputeDistanceToWall()
    {
        // Multi-source BFS from all walls: distance = steps to nearest wall
        int H = mapHeight, W = mapWidth;
        int[,] dist = new int[H, W];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                dist[y, x] = int.MaxValue;

        Queue<Vector2Int> q = new Queue<Vector2Int>();

        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (map[y, x] == 1) { dist[y, x] = 0; q.Enqueue(new Vector2Int(x, y)); } // walls are sources

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right }; // 4-neighbours
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int d = dist[p.y, p.x];
            foreach (var dir in dirs)
            {
                int nx = p.x + dir.x, ny = p.y + dir.y;
                if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                if (dist[ny, nx] > d + 1)
                {
                    dist[ny, nx] = d + 1;
                    q.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
        return dist;
    }

    void DetectRoomsByThickness()
    {
        rooms.Clear();
        roomCores.Clear();
        roomCentroid.Clear();

        var dist = ComputeDistanceToWall();

        // Core mask: floor tiles that are at least minRoomRadius away from any wall
        bool[,] core = new bool[mapHeight, mapWidth];
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                core[y, x] = (map[y, x] == 0) && (dist[y, x] >= minRoomRadius);

        // Label connected core components
        int[,] coreId = new int[mapHeight, mapWidth];
        int nextId = 0;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                if (!core[y, x] || coreId[y, x] != 0) continue;

                nextId++;
                var tiles = new List<Vector2Int>();
                Queue<Vector2Int> q = new Queue<Vector2Int>();
                q.Enqueue(new Vector2Int(x, y));
                coreId[y, x] = nextId;

                while (q.Count > 0)
                {
                    var p = q.Dequeue();
                    tiles.Add(p);
                    foreach (var d in dirs)
                    {
                        int nx = p.x + d.x, ny = p.y + d.y;
                        if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight) continue;
                        if (core[ny, nx] && coreId[ny, nx] == 0)
                        {
                            coreId[ny, nx] = nextId;
                            q.Enqueue(new Vector2Int(nx, ny));
                        }
                    }
                }

                if (tiles.Count >= minCoreSize)
                    roomCores[nextId] = tiles;
            }

        // Build Room objects + centroids from cores
        foreach (var kv in roomCores)
        {
            int id = kv.Key;
            var tiles = kv.Value;

            Vector2 sum = Vector2.zero;
            foreach (var t in tiles) sum += new Vector2(t.x, t.y);
            roomCentroid[id] = sum / Mathf.Max(1, tiles.Count);

            Vector2 upscaledPos = caMeshing.scale * roomCentroid[id]; // world pos of the room (scale is needed, since the grid is 1x1)
            rooms.Add(new Room
            {
                id = id,
                label = RoomLabel.Unassigned,
                worldPos = new Vector3(upscaledPos.x, 0, upscaledPos.y)
            });
        }
    }

    void AssignStartAndBoss()
    {
        if (rooms.Count == 0) return;

        int[,] dist = ComputeDistanceToWall();
        Dictionary<int, Vector2Int> rep = new Dictionary<int, Vector2Int>();

        foreach (var kv in roomCores)
        {
            int id = kv.Key;
            Vector2Int best = kv.Value[0];
            int bestD = dist[best.y, best.x];
            foreach (var t in kv.Value)
            {
                int d = dist[t.y, t.x];
                if (d > bestD) { bestD = d; best = t; }
            }
            rep[id] = best;
        }

        // Find the pair with the longest shortest-path on floors
        int startId = rooms[0].id, bossId = rooms[0].id;
        int bestPath = -1;

        var ids = new List<int>(roomCores.Keys);
        for (int i = 0; i < ids.Count; i++)
            for (int j = i + 1; j < ids.Count; j++)
            {
                int d = ShortestPathFloor(rep[ids[i]], rep[ids[j]]);
                if (d > bestPath)
                {
                    bestPath = d; startId = ids[i]; bossId = ids[j];
                }
            }

        // Label
        foreach (var r in rooms) r.label = RoomLabel.Unassigned;
        rooms.Find(r => r.id == startId).label = RoomLabel.Start;
        rooms.Find(r => r.id == bossId).label = RoomLabel.Boss;

        // store IDs
        startRoom = rooms.Find(r => r.id == startId);
        bossRoom = rooms.Find(r => r.id == bossId);
    }

    int ShortestPathFloor(Vector2Int a, Vector2Int b)
    {
        // Grid BFS on floor cells (0 = floor, 1 = wall)
        if (a == b) return 0;
        bool[,] seen = new bool[mapHeight, mapWidth];
        Queue<(Vector2Int p, int d)> q = new Queue<(Vector2Int, int)>();
        q.Enqueue((a, 0));
        seen[a.y, a.x] = true;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (q.Count > 0)
        {
            var (p, d) = q.Dequeue();
            foreach (var dir in dirs)
            {
                int nx = p.x + dir.x, ny = p.y + dir.y;
                if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight) continue;
                if (seen[ny, nx] || map[ny, nx] != 0) continue; // must be floor
                if (nx == b.x && ny == b.y) return d + 1;
                seen[ny, nx] = true;
                q.Enqueue((new Vector2Int(nx, ny), d + 1));
            }
        }
        return -1; // unreachable (a* or thetta should connect all, so this should not hit)
    }

    // --- Rooms ---
    // Rooms via thickness (distance-to-wall) cores
    [Header("Room detection")]
    [SerializeField] int minRoomRadius = 3;   
    [SerializeField] int minCoreSize = 6;   
    public List<Room> rooms = new List<Room>();

    Dictionary<int, List<Vector2Int>> roomCores = new Dictionary<int, List<Vector2Int>>();
    Dictionary<int, Vector2> roomCentroid = new Dictionary<int, Vector2>();




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



    // DEBUG - visual debugging 
    public float tileSize = 4f; // size of each cell for visualization

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

        if (roomCentroid != null)
        {
            foreach (var kv in roomCentroid)
            {
                var id = kv.Key;
                var c = kv.Value;
                var w = new Vector3(c.x * tileSize * caMeshing.scale, 0.3f, c.y * tileSize * caMeshing.scale);

                var room = rooms.Find(r => r.id == id);
                Color color = Color.yellow;
                if (room != null)
                {
                    if (room.label == RoomLabel.Start) color = Color.cyan;
                    else if (room.label == RoomLabel.Boss) color = Color.magenta;
                }
                Gizmos.color = color;
                Gizmos.DrawSphere(w, tileSize * 0.35f);
            }
        }
    }

}
