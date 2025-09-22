using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CAMeshing : MonoBehaviour
{
    [Header("Terrain Target")]
    [Tooltip("If left empty, a Terrain will be created as a child.")]
    public Terrain terrain;
    [Tooltip("Only used when auto-creating the Terrain GameObject.")]
    public string terrainObjectName = "CA Terrain";

    [Header("Sizing (meters)")]
    [Tooltip("World meters per CA cell (X/Z)")]
    public float scale = 4f;                  
    [Tooltip("Terrain vertical size in meters (Y). This scales the 0..1 heightmap range.")]
    public float height = 12f;

    [Header("Height Mapping")] 
    [Range(0f, 1f)] public float floorHeight01 = 0.10f; // normalized 0..1 (multiplied by 'height')
    [Range(0f, 1f)] public float wallHeight01  = 1.00f; // normalized 0..1 (multiplied by 'height')
    [Tooltip("Blend heights using distance-to-wall (soft slopes). Disable for hard binary cliffs.")]
    public bool useDistanceBlend = true;
    [Tooltip("Controls how quickly floors fall off from walls. Larger = softer slopes.")]
    [Range(0.01f, 2f)] public float distanceFalloff = 0.35f;
    [Tooltip("Optional box-blur passes applied to the final heightmap.")]
    [Range(0, 8)] public int blurPasses = 1;

    [Header("Resolution")]
    [Tooltip("Unity requires heightmap resolution to be 2^n + 1")]
    public bool autoPickResolution = true;
    [Tooltip("Used if Auto Pick Resolution is OFF. Will be clamped to 33..4097 and forced to 2^n+1. keep 513 for best results")]
    public int heightmapResolution = 513;

    [Header("Materials")]
    [SerializeField] private Material terrainMaterial;
    [SerializeField] private TerrainLayer paintLayer;

    // -------------------------- Public API -------------------------- //
    public void GenerateMeshes(int[,] map)
    {
        if (map == null) return;

        int H = map.GetLength(0);
        int W = map.GetLength(1);

        EnsureTerrain(W, H);
        var td = terrain.terrainData;

        int targetRes = autoPickResolution ? NextPow2PlusOne(Mathf.Max(W, H)) : ForcePow2PlusOne(heightmapResolution);
        td.heightmapResolution = Mathf.Clamp(targetRes, 33, 4097);
        td.size = new Vector3(W * scale, height, H * scale);

        float[,] heights = BuildHeights(map, td.heightmapResolution, td.heightmapResolution);
        if (blurPasses > 0) BoxBlurInPlace(heights, blurPasses);

        td.SetHeights(0, 0, heights);

        // Make sure there's a TerrainCollider so we can walk on it
        var col = terrain.GetComponent<TerrainCollider>();
        if (!col) col = terrain.gameObject.AddComponent<TerrainCollider>();
        col.terrainData = td;
    }

    public void ClearPrevious()
    {
        if (terrain && terrain.transform.parent == transform && terrain.gameObject.name == terrainObjectName)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(terrain.gameObject);
            else Destroy(terrain.gameObject);
#else
            DestroyImmediate(terrain.gameObject);
#endif
            terrain = null;
        }
    }

    // -------------------------- Internal -------------------------- //
    void EnsureTerrain(int widthCells, int heightCells)
    {
        if (terrain) return;

        // Try to find an existing child Terrain
        var existing = transform.Find(terrainObjectName);
        if (existing) terrain = existing.GetComponent<Terrain>();

        if (!terrain)
        {
            var go = new GameObject(terrainObjectName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;

            terrain = go.AddComponent<Terrain>();
            var td = new TerrainData();
            td.heightmapResolution = NextPow2PlusOne(Mathf.Max(widthCells, heightCells));
            td.size = new Vector3(widthCells * scale, height, heightCells * scale);
            terrain.terrainData = td;
            go.AddComponent<TerrainCollider>().terrainData = td;

            // layer
            go.layer = LayerMask.NameToLayer("Walkable");
        }

        if (terrainMaterial) terrain.materialTemplate = terrainMaterial;

        if (paintLayer)
        {
            var td = terrain.terrainData;
            var layers = new List<TerrainLayer>(td.terrainLayers ?? System.Array.Empty<TerrainLayer>());

            if (/*replaceFirstLayer &&*/ layers.Count > 0) layers[0] = paintLayer;
            else layers.Add(paintLayer);

            td.terrainLayers = layers.ToArray();
        }
    }

    int NextPow2PlusOne(int min)
    {
        // We need 2^k + 1 >= min. So ensure 2^k >= (min - 1)
        int pow = Mathf.NextPowerOfTwo(Mathf.Max(32, min - 1));
        int res = pow + 1;
        return Mathf.Clamp(res, 33, 4097);
    }

    int ForcePow2PlusOne(int any)
    {
        // Convert arbitrary value to nearest valid 2^k + 1
        if (any < 33) return 33;
        if (any > 4097) return 4097;
        int k = 32;
        while (k + 1 < any) k <<= 1; // find pow2 >= any-1
        return k + 1;
    }

    float[,] BuildHeights(int[,] map, int resY, int resX)
    {
        int H = map.GetLength(0);
        int W = map.GetLength(1);

        // Precompute distance-to-wall on the CA grid (4-neighbour)
        int[,] dist = ComputeDistanceToWall(map);

        float[,] heights = new float[resY, resX]; // TerrainData expects [y, x]

        // Max useful distance for normalization (not strictly needed when using exp falloff)
        int maxD = 1;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (dist[y, x] > maxD) maxD = dist[y, x];
        if (maxD <= 0) maxD = 1;

        for (int y = 0; y < resY; y++)
        {
            float gy = (H - 1) * (y / (float)(resY - 1));
            int cy = Mathf.Clamp(Mathf.RoundToInt(gy), 0, H - 1);
            for (int x = 0; x < resX; x++)
            {
                float gx = (W - 1) * (x / (float)(resX - 1));
                int cx = Mathf.Clamp(Mathf.RoundToInt(gx), 0, W - 1);

                int tile = map[cy, cx]; // 0 = floor, 1 = wall
                float h;
                if (!useDistanceBlend)
                {
                    h = (tile == 1) ? wallHeight01 : floorHeight01;
                }
                else
                {
                    // Distance from this floor sample to the nearest wall in grid steps.
                    // For walls, set 0 so we stay near wallHeight.
                    int d = (tile == 0) ? dist[cy, cx] : 0;

                    // Smooth falloff from wallHeight -> floorHeight as we move away from walls
                    // t ~ 0 near walls, -> 1 deeper inside rooms
                    float t = 1f - Mathf.Exp(-d * distanceFalloff);
                    t = Mathf.Clamp01(t);
                    h = Mathf.Lerp(wallHeight01, floorHeight01, t);
                }

                heights[y, x] = Mathf.Clamp01(h);
            }
        }

        return heights;
    }

    void BoxBlurInPlace(float[,] a, int passes)
    {
        int H = a.GetLength(0);
        int W = a.GetLength(1);
        float[,] tmp = new float[H, W];

        for (int p = 0; p < passes; p++)
        {
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = y + dy; if (yy < 0 || yy >= H) continue;
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int xx = x + dx; if (xx < 0 || xx >= W) continue;
                            sum += a[yy, xx]; n++;
                        }
                    }
                    tmp[y, x] = sum / Mathf.Max(1, n);
                }
            }
            // swap
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    a[y, x] = tmp[y, x];
        }
    }

    int[,] ComputeDistanceToWall(int[,] map)
    {
        int H = map.GetLength(0);
        int W = map.GetLength(1);
        int[,] dist = new int[H, W];

        // init
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                dist[y, x] = int.MaxValue;

        Queue<Vector2Int> q = new Queue<Vector2Int>();

        // multi-source: all walls are at distance 0
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
                if (map[y, x] == 1) { dist[y, x] = 0; q.Enqueue(new Vector2Int(x, y)); }

        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            int d = dist[p.y, p.x];
            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dirs[i].x;
                int ny = p.y + dirs[i].y;
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
}
