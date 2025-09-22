using GG;
using UnityEditor;
using UnityEngine;

public class VoronoiGrammarController : MonoBehaviour, IDungeon
{
    [Header("Refs")]
    public VoronoiLayoutGenerator generator;
    public VoronoiMeshing mesher;

    // ---------------- Grammar (structure + radii) ---------------- //
    [Header("Grammar")]
    [Tooltip("Random seed used for both grammar structure and island placement (deterministic).")]
    public int seed = 12345;

    [Tooltip("How many expansion steps to apply to the grammar.")]
    [Min(0)] public int steps = 4;

    [Tooltip("Target radius for A islands (world units).")]
    public float radiusA = 9f;

    [Tooltip("Target radius for B islands (world units).")]
    public float radiusB = 7f;

    [Tooltip("Target radius for C islands (world units).")]
    public float radiusC = 7f;

    [Tooltip("Target radius for Start island (world units).")]
    public float radiusStart = 6f;

    [Tooltip("Target radius for Boss island (world units).")]
    public float radiusBoss = 7f;

    [Header("Final Scale")]
    public float finalScale = 4.0f;

    // ---------------- Placement ---------------- //
    [Header("Placement")]
    [Tooltip("Extra spacing added between island circles (world units).")]
    public float placementGap = 2f;

    [Tooltip("Candidate angles per ring around the anchor during placement.")]
    public int placementAngleSamples = 16;

    [Tooltip("Number of rings to try if the current ring has no valid angle.")]
    public int placementRingTries = 4;

    [Tooltip("Extra distance added each time we bump to the next ring.")]
    public float placementRingStep = 3f;

    [Tooltip("Use golden-angle spiral for angle sampling (else uniform angles).")]
    public bool placementUseGoldenAngle = false;

    [Header("Voronoi")]
    [Tooltip("Padding added around all islands when computing world bounds for Voronoi.")]
    public float boundsPadding = 15f;

    // ---------------- Corridors ----------------
    [Header("Corridors (A* raster)")]
    [Tooltip("Raster tile size used for A* pathfinding over the world bounds.")]
    public float rasterTileSize = 0.5f;

    [Tooltip("How thick to paint the corridor in cell space (>=1).")]
    public int corridorThickness = 2;

    // ---------------- Rooms -------------------- //
    [Header("Rooms")]
    public System.Collections.Generic.List<Room> rooms = new System.Collections.Generic.List<Room>();
    private Room startRoom, bossRoom;

    public Room GetPlayerStart() => startRoom;
    public Room GetBossLocation() => bossRoom;

    static bool IsIslandType(NodeLabel L) =>
        L == NodeLabel.Start || L == NodeLabel.Boss ||
        L == NodeLabel.A || L == NodeLabel.B || L == NodeLabel.C;

    static RoomLabel ToRoomLabel(NodeLabel g)
    {
        switch (g)
        {
            case NodeLabel.Start: return RoomLabel.Start;
            case NodeLabel.Boss: return RoomLabel.Boss;
            case NodeLabel.A: return RoomLabel.A;
            case NodeLabel.B: return RoomLabel.B;
            case NodeLabel.C: return RoomLabel.C;
            default: return RoomLabel.Unassigned;
        }
    }

    void BuildRooms(Graph g)
    {
        rooms.Clear();
        foreach (var n in g.nodes)
        {
            if (!IsIslandType(n.label)) continue;
            rooms.Add(new Room
            {
                id = n.id,
                label = ToRoomLabel(n.label),
                worldPos = new Vector3(n.pos.x, 0f, n.pos.y) * finalScale
            });

            // inject start - boss rooms
            if(n.label == NodeLabel.Start)
            {
                startRoom = rooms[rooms.Count - 1];
            }
            else if (n.label == NodeLabel.Boss)
            {
                bossRoom = rooms[rooms.Count - 1];
            }
        }
    }

    // Implementation helpers
    GraphVoronoiMapper mapper = new GraphVoronoiMapper();

    public void Generate(int seed)
    {
        this.seed = seed;
        Generate();
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        if (!generator || !mesher)
        {
            return;
        }

        // 1) Build grammar spec
        var grammar = new GG.GraphGrammar(seed: seed)
        {
            radiusA = this.radiusA,
            radiusB = this.radiusB,
            radiusC = this.radiusC,
            radiusStart = this.radiusStart,
            radiusBoss = this.radiusBoss
        };
        grammar.AddDefaultRules();
        var spec = grammar.Derive(GG.NodeLabel.S, steps);

        // 2) Place islands with non-overlap + compute bounds
        var placer = new GG.IslandPlacer(seed)
        {
            gap = placementGap,
            angleSamples = placementAngleSamples,
            ringTries = placementRingTries,
            ringStep = placementRingStep,
            useGoldenAngle = placementUseGoldenAngle
        };
        var positions = placer.Place(spec);
        var bounds = GG.IslandPlacer.ComputeBounds(spec, positions, boundsPadding);

        // 3) Generate Voronoi inside computed bounds
        generator.worldBounds = bounds;
        generator.Generate();

        // 4) Build runtime graph with positions
        var g = GG.Graph.FromSpec(spec, positions);

        // 5) Label islands by radius, then add corridors
        var labels = mapper.LabelIslandsByRadius(generator, g);
        mapper.AddCorridorsWithAStar(generator, g, ref labels, rasterTileSize, corridorThickness);

        BuildRooms(g); // create Room objects at island centers

        // 6) Meshing
        mesher.activeGraph = g;
        mesher.cellLabels = labels;
        mesher.hexScale = finalScale;
        mesher.Rebuild();

        // Debug display
        int a = 0, b = 0, c = 0, start = 0, boss = 0, corr = 0, none = 0;
        foreach (var L in labels)
        {
            switch (L)
            {
                case NodeLabel.A: a++; break;
                case NodeLabel.B: b++; break;
                case NodeLabel.C: c++; break;
                case NodeLabel.Start: start++; break;
                case NodeLabel.Boss: boss++; break;
                case NodeLabel.Corridor: corr++; break;
                default: none++; break;
            }
        }
        Console.Log($"Islands: A={a} B={b} C={c} Start={start} Boss={boss} | Corridor={corr} | None={none} | Bounds={bounds}");
    }

    [ContextMenu("Clear")]
    public void Clean()
    {
        if (!generator || !mesher)
        {
            return;
        }

        generator.Clear();
        mesher.Clear();
    }

    [SerializeField] float roomGizmoRadius = 0.45f;
    [SerializeField] float verticalOffset = 0.2f;

    void OnDrawGizmos()
    {
        if (rooms == null || rooms.Count == 0) return;

        Vector3? startPos = null, bossPos = null;

        foreach (var r in rooms)
        {
            if (r == null) continue;

            Color color;
            switch (r.label)
            {
                case RoomLabel.Start: color = Color.cyan; break;
                case RoomLabel.Boss: color = Color.magenta; break;
                case RoomLabel.A: color = new Color(0.2f, 0.6f, 1f); break;
                case RoomLabel.B: color = new Color(0.2f, 1f, 0.6f); break;
                case RoomLabel.C: color = new Color(1f, 0.8f, 0.2f); break;
                default: color = Color.yellow; break;
            }

            Gizmos.color = color;
            var pos = r.worldPos + Vector3.up * verticalOffset;
            Gizmos.DrawSphere(pos, roomGizmoRadius);
            Gizmos.DrawWireSphere(pos, roomGizmoRadius * 1.15f);

            if (r.label == RoomLabel.Start) startPos = pos;
            else if (r.label == RoomLabel.Boss) bossPos = pos;

#if UNITY_EDITOR
            Handles.color = color;
            Handles.Label(pos + Vector3.up * (roomGizmoRadius + 0.15f), $"{r.label} ({r.id})");
#endif
        }
    }
}
