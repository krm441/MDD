using GG;
using UnityEngine;

public class VoronoiGrammarController : MonoBehaviour
{
    [Header("Refs")]
    public VoronoiLayoutGenerator generator;
    public VoronoiMeshing mesher;

    // ---------------- Grammar (structure + radii) ----------------
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

    // ---------------- Placement (non-overlap circles) ----------------
    [Header("Placement (Non-overlap)")]
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
    public float boundsPadding = 5f;

    // ---------------- Corridors ----------------
    [Header("Corridors (A* raster)")]
    [Tooltip("Raster tile size used for A* pathfinding over the world bounds.")]
    public float rasterTileSize = 0.5f;

    [Tooltip("How thick to paint the corridor in cell space (>=1).")]
    public int corridorThickness = 2;

    // Implementation helpers
    GraphVoronoiMapper mapper = new GraphVoronoiMapper();

    [ContextMenu("Generate")]
    public void GenerateAndColor()
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

        // 5) Label islands by radius, then add corridors (note, paints the None labeled cells, doesnt touch the occupied)
        var labels = mapper.LabelIslandsByRadius(generator, g);
        mapper.AddCorridorsWithAStar(generator, g, ref labels, rasterTileSize, corridorThickness);

        // 6) Meshing
        mesher.activeGraph = g;
        mesher.cellLabels = labels;
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
}
