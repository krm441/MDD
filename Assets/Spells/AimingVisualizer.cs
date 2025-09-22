

using System.Collections.Generic;
using System;//.Drawing;
using UnityEngine;
using UnityEngine.AI;
using PartyManagement;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Assertions;

public static class AimingVisualizer
{

    private static GameObject aimingVisualiserParent;


    private static GameObject circleObj;
    private static GameObject spellRange;
    private static GameObject lineProjectile;
    //private static LineRenderer lineRendererCircle;
    //private static LineRenderer lineRendererLine;
    private static List<Renderer> highlighted = new List<Renderer>();
    private static Color highlightColor = Color.white * 0.5f;
    private static GameObject clickMarkerPrefab; // actual marker
    private static GameObject currentClickMarker;// active marker (that will be destroyed after 1.5 sec animation)

    private static int lastSegments = 32;

    // ============== Utility Functions: Init and Clean ======================== //
    private static void CreateAimingVisualiserParent()
    {
        if (aimingVisualiserParent == null)
            aimingVisualiserParent = new GameObject("AimingVisualiser");
    }

    private static void SetParent(GameObject child)
    {
        child.transform.SetParent(aimingVisualiserParent.transform);
    }

    public static void DestroyAllChildren(GameObject parent)
    {
        // It's safest to iterate backwards so removing children
        // doesn't mess up the indexing.
        if (parent != null)
        {
            var parentTransform = parent.transform;
            for (int i = parentTransform.childCount - 1; i >= 0; i--)
            {
                Transform child = parentTransform.GetChild(i);
                GameObject.Destroy(child.gameObject);
            }
        }
    }

    public static void Hide()
    {
        ClearPathPreview();
        previewSegments.Clear();
        ClearHighlights();
        DestroyAllChildren(aimingVisualiserParent); return;
        //if (circleObj != null)
        //{
        //    UnityEngine.Object.Destroy(circleObj);
        //    circleObj = null;
        //    //lineRendererCircle = null;
        //}
        //
        //ClearState();
        //ClearHighlights();
    }

    // ============== Visuals ================================================== //

    public static void HighlightTargets(Vector3 center, float radius)
    {
        ClearHighlights();

        var hits = Physics.OverlapSphere(center, radius, LayerMask.GetMask("PartyLayer", "Destructibles", "HostileNPCs"));
        foreach (var col in hits)
        {
            var rend = col.GetComponentInChildren<Renderer>();
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", highlightColor);
                highlighted.Add(rend);
            }
        }
    }

    /// <summary>
    /// Better version - with 'out' variable of GO
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="highlightedObjects"></param>
    public static void HighlightTargets(Vector3 center, float radius, out List<GameObject> highlightedObjects)
    {
        ClearHighlights();

        highlightedObjects = new List<GameObject>();

        var hits = Physics.OverlapSphere(center, radius, LayerMask.GetMask("PartyLayer", "Destructibles", "HostileNPCs"));
        foreach (var col in hits)
        {
            GameObject go = col.gameObject;
            highlightedObjects.Add(go);    

            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", highlightColor);

                highlighted.Add(rend);           
            } 
        }
    }


    private static void ClearHighlights()
    {
        foreach (var rend in highlighted)
        {
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.SetColor("_EmissionColor", Color.black);
                rend.material.DisableKeyword("_EMISSION");
            }
        }

        highlighted.Clear();
    }



    public static void SpawnClickMarker(Vector3 position)
    {
        //AimingVisualizer.DrawImpactCircle(position, 0.5f, Color.green); return;


        if (clickMarkerPrefab == null)
        {
            clickMarkerPrefab = Resources.Load<GameObject>("Markers/selector1");
            if (clickMarkerPrefab == null)
            {
                Debug.LogWarning("ClickMarker prefab not found in Resources/Markers!");
            }
        }

        if (currentClickMarker != null)
            GameObject.Destroy(currentClickMarker);

        Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
        Vector3 pos = position + new Vector3(0f, 0.1f, 0f);

        currentClickMarker = GameObject.Instantiate(clickMarkerPrefab, pos, rotation);
        GameObject.Destroy(currentClickMarker, 1.5f);
    }
    
    /// <summary>
    /// FOR DEGBUGGING: draws a circle of impact of a spell after spell cast
    /// needs no parent - destruction automatic after 1.5 seconds
    /// </summary>
    /// <param name="center">Center of the circle</param>
    /// <param name="radius">Radius</param>
    /// <param name="color">Circle border color</param>
    /// <param name="segments">Default value: 32</param>
    public static void DrawImpactCircle(Vector3 center, float radius, Color color, int segments = 32)
    {
        if (spellRange == null)
        {
            spellRange = new GameObject("SpellRangeCircle");
            LineRenderer lr = spellRange.AddComponent<LineRenderer>();

            lr.positionCount = segments + 1;
            lr.loop = true;
            lr.widthMultiplier = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + center;
            spellRange.GetComponent<LineRenderer>().SetPosition(i, pos);
        }

        GameObject.Destroy(spellRange, 1.5f); // destroy after 1.5 sec
    }
    public static void ShowAimingCircle(Vector3 center, float radius, Color color, int segments = 32)
    {
        CreateAimingVisualiserParent();
        if (circleObj == null)
        {
            circleObj = new GameObject("AimingCircle");
            SetParent(circleObj);
            LineRenderer lineRendCircle = circleObj.AddComponent<LineRenderer>();
            lineRendCircle.loop = true;
            lineRendCircle.widthMultiplier = 0.05f;
            lineRendCircle.material = new Material(Shader.Find("Sprites/Default"));
            lineRendCircle.startColor = color; //Color.yellow;
            lineRendCircle.endColor = color; //Color.yellow;
            lineRendCircle.positionCount = segments + 1;
            lastSegments = segments;
        }

        var lineRendererCircle = circleObj.GetComponent<LineRenderer>();
        // change color
        if (lineRendererCircle.startColor != color)
        {
            lineRendererCircle.startColor = color;
            lineRendererCircle.endColor = color;
        }

        UpdateAimingCircle(center, radius, segments);
    }

    private static bool GenerateProjectileArc(
    Vector3 start,
    Vector3 end,
    float arcHeight,
    int segments,
    LayerMask obstacleMask,
    out Vector3[] arcPoints,
    out int hitIndex)
    {
        arcPoints = new Vector3[segments + 1];
        hitIndex = -1;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y += arcHeight * 4f * t * (1f - t);
            arcPoints[i] = point;
        }

        for (int i = 0; i < segments; i++)
        {
            Vector3 a = arcPoints[i];
            Vector3 b = arcPoints[i + 1];

            if (Physics.Linecast(a, b, out RaycastHit hit, obstacleMask))
            {
                arcPoints[i + 1] = hit.point;
                hitIndex = i + 1;
                return true;
            }
        }

        return false;
    }

    static readonly RaycastHit[] raycastHits = new RaycastHit[32];
    private static int mask = LayerMask.GetMask("Interactables", "FriendlyNPCs", "PartyLayer", "Walkable", "Obstacles", "HostileNPCs");
    // helper for painter's sorting
    private static readonly IComparer<RaycastHit> HitDistanceComparer =
    Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    public static class LayerCursorMap
    {
        public static readonly Dictionary<int, Action<CursorManager, bool>> Handlers = new Dictionary<int, Action<CursorManager, bool>>()
        {
            [Obstacles] = (CursorManager CursorManager, bool b)       => CursorManager.SetCursor(CursorTypesMDD.Forbidden),
            [Interactables] = (CursorManager CursorManager, bool b)   => CursorManager.SetCursor(CursorTypesMDD.Use),
            [HostileNPCs] = (CursorManager CursorManager, bool b)     => CursorManager.SetCursor(CursorTypesMDD.Melee),
            [FriendlyNPCs] = (CursorManager CursorManager, bool b)    => CursorManager.SetCursor(CursorTypesMDD.Talk),
            [Walkable] = (CursorManager CursorManager, bool isReachable) => CursorManager.SetCursor(isReachable ? CursorTypesMDD.Default : CursorTypesMDD.Forbidden),
        };
    }

    static readonly int Obstacles = LayerMask.NameToLayer("Obstacles");
    static readonly int Interactables = LayerMask.NameToLayer("Interactables");
    static readonly int HostileNPCs = LayerMask.NameToLayer("HostileNPCs");
    static readonly int FriendlyNPCs = LayerMask.NameToLayer("FriendlyNPCs");
    static readonly int Walkable = LayerMask.NameToLayer("Walkable");
    public static void ManageCursor(CursorManager cm, int layer, bool isReachable, bool isRangedWeapon, bool displayTextMsg = true)
    {
        Assert.IsNotNull(cm);
        if (LayerCursorMap.Handlers.TryGetValue(layer, out var handler))
        {
            // special case - ranged
            if(isReachable && isRangedWeapon)
            {
                cm.SetCursor(CursorTypesMDD.Arrow);
                return;
            }
            handler(cm, isReachable);
        }
        else
            cm.SetCursor(CursorTypesMDD.Default);        
    }


    /*public static int ManageCursor(out GameObject go)
    {
        int ret = -1;
        go = null;

        Ray rayIdle = Camera.main.ScreenPointToRay(Input.mousePosition);

        int countIdle = Physics.RaycastNonAlloc(rayIdle, raycastHits, 100f, mask, QueryTriggerInteraction.Ignore);

        // sort by distance (aka Painter's apgorithm)
        System.Array.Sort<RaycastHit>(raycastHits, 0, countIdle, HitDistanceComparer);

        for (int i = 0; i < countIdle; i++)
        {
            var h = raycastHits[i];
            go = h.collider.gameObject;

            ret = go.layer;

            if (go.layer == LayerMask.NameToLayer("Obstacles"))
            {
                CursorManager.SetCursorType(CursorTypesMDD.Forbidden);
                break;
            }

            if (go.layer == LayerMask.NameToLayer("Interactables"))
            {
                CursorManager.SetCursorType(CursorTypesMDD.Use);
                break;
            }

            if (go.layer == LayerMask.NameToLayer("HostileNPCs"))
            {
                CursorManager.SetCursorType(CursorTypesMDD.Melee);
                break;
            }

            if (go.layer == LayerMask.NameToLayer("FriendlyNPCs"))
            {
                CursorManager.SetCursorType(CursorTypesMDD.Talk);
                break;
            }

            CursorManager.SetCursorType(CursorTypesMDD.Default);
        }

        return ret;
    }*/

    public static void ClearProjectileArc()
    {
        if (lineProjectile != null)
        {
            GameObject.Destroy(lineProjectile);
        }
    }

    private static void RenderArc(Vector3[] points, int drawCount, Color color)
    {
        CreateAimingVisualiserParent();

        if (lineProjectile == null)
        {
            lineProjectile = new GameObject("AimingVisualizer_LineRenderer");
            SetParent(lineProjectile);
            lineProjectile.hideFlags = HideFlags.None;

            var lr = lineProjectile.AddComponent<LineRenderer>();
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.useWorldSpace = true;
        }

        var lineRenderer = lineProjectile.GetComponent<LineRenderer>();
        lineRenderer.positionCount = drawCount;
        lineRenderer.startColor = lineRenderer.endColor = color;

        for (int i = 0; i < drawCount; i++)
            lineRenderer.SetPosition(i, points[i]);
    }

    public static void DrawStraightLine(
     Vector3 start,
     Vector3 end,
     Color baseColor,
     out bool obstaclesHit,
     float width = 0.05f
 )
    {
        LayerMask mask = LayerMask.GetMask("Obstacles");

        RaycastHit hitInfo;
        bool hit = Physics.Raycast(start, (end - start).normalized, out hitInfo, Vector3.Distance(start, end), mask);

        Vector3 finalEnd = hit ? hitInfo.point : end;
        obstaclesHit = hit;

        CreateAimingVisualiserParent();

        if (lineProjectile == null)
        {
            lineProjectile = new GameObject("StraightLineRenderer");
            SetParent(lineProjectile);
            var lr = lineProjectile.AddComponent<LineRenderer>();
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.useWorldSpace = true;
        }

        var lineRenderer = lineProjectile.GetComponent<LineRenderer>();
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, finalEnd);

        Color lineColor = hit ? Color.red : baseColor;
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
    }



    public static void DrawProjectileArc(
    Vector3 start,
    Vector3 end,
    Color baseColor,
    out bool obstaclesHit,
    int segments = 30,
    float arcHeight = 2f)
    {
        LayerMask mask = LayerMask.GetMask("Obstacles");

        bool hit = GenerateProjectileArc(start, end, arcHeight, segments, mask, out Vector3[] points, out int hitIndex);
        obstaclesHit = hit;

        int drawCount = (hitIndex > 0) ? hitIndex + 1 : points.Length;
        Color arcColor = hit ? Color.red : baseColor;

        RenderArc(points, drawCount, arcColor);
    }


    public static void DrawProjectileArc1(
        Vector3 start,
        Vector3 end,
        Color color,
        out bool obstaclesHit,
        int segments = 30,
        float arcHeight = 2f)
    {
        obstaclesHit = false;

        CreateAimingVisualiserParent();

        if (lineProjectile == null)
        {
            lineProjectile = new GameObject("AimingVisualizer_LineRenderer");
            SetParent(lineProjectile);
            lineProjectile.hideFlags = HideFlags.None;
            var lineRendererLine = lineProjectile.AddComponent<LineRenderer>();
            lineRendererLine.positionCount = 0;
            lineRendererLine.startWidth = 0.05f;
            lineRendererLine.endWidth = 0.05f;
            lineRendererLine.material = new Material(Shader.Find("Sprites/Default"));
            lineRendererLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRendererLine.receiveShadows = false;
            lineRendererLine.useWorldSpace = true;
        }

        // Build parabola points 
        Vector3[] points = new Vector3[segments + 1];
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            // Linear interpolation
            Vector3 point = Vector3.Lerp(start, end, t);
            // Add height offset: a simple parabola peaked at t=0.5
            point.y += arcHeight * 4f * t * (1f - t);
            points[i] = point;
        }

        // Collision check per segment, trim on hit
        int hitIndex = -1;
        int mask = LayerMask.GetMask("Obstacles");
        for (int i = 0; i < segments; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            if (Physics.Raycast(a, (b - a).normalized, out RaycastHit hit, Vector3.Distance(a, b), mask))
            {
                // Insert the exact hit point
                points[i + 1] = hit.point;
                hitIndex = i + 1;
                obstaclesHit = true;
                break;
            }
        }

        // Determine points to draw
        int drawCount = (hitIndex > 0) ? hitIndex + 1 : points.Length;

        // Push into LineRenderer
        lineProjectile.GetComponent<LineRenderer>().positionCount = drawCount;
        lineProjectile.GetComponent<LineRenderer>().startColor = lineProjectile.GetComponent<LineRenderer>().endColor = color;
        for (int i = 0; i < drawCount; i++)
            lineProjectile.GetComponent<LineRenderer>().SetPosition(i, points[i]);
    }


    public static void UpdateAimingCircle(Vector3 center, float radius, int segments = 32)
    {
        if (circleObj == null) return;

        var lineRendererCircle = circleObj.GetComponent<LineRenderer>();
        if (lineRendererCircle == null) return;

        if (segments != lastSegments)
        {
            lineRendererCircle.positionCount = segments + 1;
            lastSegments = segments;
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + center;
            lineRendererCircle.SetPosition(i, pos);
        }
    }

    // Draw path preview, white if traversable, red if not, with a gradient in between
    public static List<Vector3> reachablePath = new List<Vector3>();  // Stores white path only
    private static GameObject previewContainer;
    private static List<LineRenderer> previewSegments = new List<LineRenderer>();




    public static void DrawNavMeshPathPreview(Vector3 start, NavMeshPath navPath)
    {
        ClearPathPreview();

        if (navPath == null || navPath.corners.Length < 2)
            return;

        // Create container
        if (previewContainer == null)
            previewContainer = new GameObject("NavMeshPathPreview");

        // Create line object
        GameObject lineObj = new GameObject("PathLine");
        lineObj.transform.SetParent(previewContainer.transform, worldPositionStays: true);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = 0.05f;
        lr.useWorldSpace = true;
        lr.loop = false;

        // Apply path corners
        lr.positionCount = navPath.corners.Length;
        lr.SetPositions(navPath.corners);

        // Solid white color
        lr.startColor = Color.white;
        lr.endColor = Color.white;

        // Add spheres at each corner
        foreach (Vector3 corner in navPath.corners)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = corner + new Vector3(0, 0.1f, 0); // Slightly above ground
            sphere.transform.localScale = Vector3.one * 0.2f;
            sphere.GetComponent<Renderer>().material.color = Color.white;
            sphere.transform.SetParent(previewContainer.transform, worldPositionStays: true);

            // Remove collider
            GameObject.Destroy(sphere.GetComponent<Collider>());
        }
    }

    static Material lineMat;
    static Material dotMat;

    public static void HidePathPreview()
    {
        ClearPathPreview();
    }

    public static void DrawPathPreview(
    NavMeshPath path,
    Color color, float width = 0.03f, float dotRadius = 0.05f)
    {
        // 1 Clear last frame
        ClearPathPreview();

        CreateAimingVisualiserParent();
        if (previewContainer == null)
        {
            previewContainer = new GameObject("PathPreviewContainer");
            SetParent(previewContainer);
        }
        else
        {
            DestroyAllChildren(previewContainer);
        }

        // 2 Gradient
        var go = new GameObject("PathPreviewLine");
        go.transform.SetParent(previewContainer.transform, worldPositionStays: true);
        var lr = go.AddComponent<LineRenderer>();
        if (lineMat == null) lineMat = new Material(Shader.Find("Sprites/Default"));
        lr.material = lineMat;
        lr.widthMultiplier = width;
        lr.useWorldSpace = true;
        lr.loop = false;

        lr.positionCount = path.corners.Length;
        lr.SetPositions(path.corners);

        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.9f, 1f) }
        );
        lr.colorGradient = grad;

        // Line positions
        var corners = path.corners;
        lr.positionCount = corners.Length;
        lr.SetPositions(corners);

       
        // 3) Corner dots (bigger at the last point)
        for (int i = 0; i < corners.Length; i++)
        {
            //float r = (i == corners.Length - 1) ? dotRadius * 1.75f : dotRadius;
            //CreateDot($"Corner_{i}", corners[i], r, color);
            CreateDot($"Corner_{i}", corners[i], dotRadius, color);
        }
    }

    static void CreateDot(string name, Vector3 pos, float radius, Color color)
    {
        // Simple spher
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = name;
        dot.transform.SetParent(previewContainer.transform, true);
        dot.transform.position = pos;
        dot.transform.localScale = Vector3.one * (radius * 2f); // unit sphere has diameter 1

        // Unlit material so its visible regardless of lighting
        if (dotMat == null)
        {
            // Fallback to Standard if unavailable.
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            dotMat = new Material(shader);
        }

        var mr = dot.GetComponent<MeshRenderer>();
        mr.sharedMaterial = dotMat;

        // Set color 
        mr.sharedMaterial.color = color;

        // Make it pop a bit 
        if (mr.sharedMaterial.HasProperty("_EmissionColor"))
        {
            mr.sharedMaterial.EnableKeyword("_EMISSION");
            mr.sharedMaterial.SetColor("_EmissionColor", color * 0.5f);
        }
    }

    public static List<Vector3> DrawPathPreview(
    //Vector3 start,
    //Vector3 end,
    NavMeshPath path,
    float maxDistance,
    bool drawToMaxOnly = false
)
    {
        // 1 Clear last frame
        ClearPathPreview();
        reachablePath.Clear();

        // 2 Early return
        if (path == null)
            return null;

        // 3 Container != null
        CreateAimingVisualiserParent();
        if (previewContainer == null)
        {
            previewContainer = new GameObject("PathPreviewContainer");
            SetParent(previewContainer);
        }
        else
        {
            DestroyAllChildren(previewContainer);
        }

        // 4 Flatten Pathfinding.Node list to Vec3
        List<Vector3> pathPoints = new List<Vector3>(path.corners.Length + 1) { path.corners[0] };
        foreach (var n in path.corners) pathPoints.Add(n);

        // 5 Compute total path length
        float totalDistance = 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
            totalDistance += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
        if (totalDistance <= 0f) return null;

        // 6 Sample along the path at fixed increments
        float stepSize = 1f;
        Vector3 elevation = new Vector3(0f, 0f, 0f);
        float distanceSoFar = 0f;
        Vector3 current = path.corners[0];
        var sampled = new List<Vector3> { path.corners[0] + elevation };

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 a = pathPoints[i];
            Vector3 b = pathPoints[i + 1];
            float segLength = Vector3.Distance(a, b);
            Vector3 dir = (b - a).normalized;

            float walked = 0f;
            while (walked < segLength)
            {
                float move = Mathf.Min(stepSize, segLength - walked);
                walked += move;
                distanceSoFar += move;
                current = a + dir * walked;
                var elevated = current + elevation;

                // record the 'in-range' positions
                if (distanceSoFar <= maxDistance)
                    reachablePath.Add(elevated);

                sampled.Add(elevated);

                if (drawToMaxOnly && distanceSoFar >= maxDistance)
                    break;
            }
            if (drawToMaxOnly && distanceSoFar >= maxDistance)
                break;
        }

        if (sampled.Count < 2)
            return null;

        // 7 Create a single LineRenderer
        var go = new GameObject("PathPreviewLine");
        go.transform.SetParent(previewContainer.transform, worldPositionStays: true);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = 0.05f;
        lr.useWorldSpace = true;
        lr.loop = false;

        lr.positionCount = sampled.Count;
        lr.SetPositions(sampled.ToArray());

        // 8 Build a 4-key gradient: passable - blended - blocked
        float split = Mathf.Clamp01(maxDistance / totalDistance);
        var gradient = new Gradient();

        if (split <= 0f)
        {
            // all red
            gradient.SetKeys(
                new[] {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.red, 1f)
                },
                new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
                }
            );
        }
        else if (split >= 1f)
        {
            // all white
            gradient.SetKeys(
                new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
                },
                new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
                }
            );
        }
        else
        {
            // white until split, then red (instant blend)
            gradient.SetKeys(
                new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, split),
                new GradientColorKey(Color.red,   split),
                new GradientColorKey(Color.red,   1f)
                },
                new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
                }
            );
        }

        lr.colorGradient = gradient;

        // 9 Track for cleanup
        previewSegments.Add(lr);

        // 10  Add spheres at each corner
        foreach (Vector3 corner in path.corners)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = corner + new Vector3(0, 0f, 0); // Slightly above ground
            sphere.transform.localScale = Vector3.one * 0.2f;
            sphere.GetComponent<Renderer>().material.color = Color.white;
            sphere.transform.SetParent(previewContainer.transform, worldPositionStays: true);

            // Remove collider
            GameObject.Destroy(sphere.GetComponent<Collider>());
        }

        //var ret = new NavMeshPath();
        //NavMesh.CalculatePath(reachablePath[0], reachablePath[reachablePath.Count - 1], NavMesh.AllAreas, ret);
        return reachablePath;
    }

    public static Pathfinding.Path DrawPathPreview(
    Vector3 start,
    Vector3 end,
    List<Pathfinding.Node> path,
    float maxDistance,
    bool drawToMaxOnly = false
)
    {
        // 1 Clear last frame
        ClearPathPreview();
        reachablePath.Clear();

        // 2 Early return
        if (path == null || path.Count == 0)
            return null;

        // 3 Container != null
        CreateAimingVisualiserParent();
        if (previewContainer == null)
        {
            previewContainer = new GameObject("PathPreviewContainer");
            SetParent(previewContainer);
        }
        else
        {
            DestroyAllChildren(previewContainer);
        }

        // 4 Flatten Pathfinding.Node list to Vec3
        List<Vector3> pathPoints = new List<Vector3>(path.Count + 1) { start };
        foreach (var n in path) pathPoints.Add(n.worldPos);

        // 5 Compute total path length
        float totalDistance = 0f;
        for (int i = 0; i < pathPoints.Count - 1; i++)
            totalDistance += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
        if (totalDistance <= 0f) return null;

        // 6 Sample along the path at fixed increments
        float stepSize = 1f;
        Vector3 elevation = new Vector3(0f, 0.6f, 0f);
        float distanceSoFar = 0f;
        Vector3 current = start;
        var sampled = new List<Vector3> { start + elevation };

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 a = pathPoints[i];
            Vector3 b = pathPoints[i + 1];
            float segLength = Vector3.Distance(a, b);
            Vector3 dir = (b - a).normalized;

            float walked = 0f;
            while (walked < segLength)
            {
                float move = Mathf.Min(stepSize, segLength - walked);
                walked += move;
                distanceSoFar += move;
                current = a + dir * walked;
                var elevated = current + elevation;

                // record the 'in-range' positions
                if (distanceSoFar <= maxDistance)
                    reachablePath.Add(elevated);

                sampled.Add(elevated);

                if (drawToMaxOnly && distanceSoFar >= maxDistance)
                    break;
            }
            if (drawToMaxOnly && distanceSoFar >= maxDistance)
                break;
        }

        if (sampled.Count < 2)
            return null;

        // 7 Create a single LineRenderer
        var go = new GameObject("PathPreviewLine");
        go.transform.SetParent(previewContainer.transform, worldPositionStays: true);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = 0.05f;
        lr.useWorldSpace = true;
        lr.loop = false;

        lr.positionCount = sampled.Count;
        lr.SetPositions(sampled.ToArray());

        // 8 Build a 4-key gradient: passable - blended - blocked
        float split = Mathf.Clamp01(maxDistance / totalDistance);
        var gradient = new Gradient();

        if (split <= 0f)
        {
            // all red
            gradient.SetKeys(
                new[] {
                new GradientColorKey(Color.red, 0f),
                new GradientColorKey(Color.red, 1f)
                },
                new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
                }
            );
        }
        else if (split >= 1f)
        {
            // all white
            gradient.SetKeys(
                new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
                },
                new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
                }
            );
        }
        else
        {
            // white until split, then red (instant blend)
            gradient.SetKeys(
                new[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, split),
                new GradientColorKey(Color.red,   split),
                new GradientColorKey(Color.red,   1f)
                },
                new[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
                }
            );
        }

        lr.colorGradient = gradient;

        // 9 Track for cleanup
        previewSegments.Add(lr);

        // 10 return walkable path
        return new Pathfinding.Path(reachablePath);
    }

    public static void ClearPathPreview()
    {
        if (previewContainer != null)
        {
            foreach (Transform child in previewContainer.transform)
                GameObject.Destroy(child.gameObject);
        }
    }





    public static void DrawPath(List<Vector3> points)
    {
        ClearPathPreview(); // clear previous render

        //if (points == null || points.Count < 2)
        //    return;
        //
        //if (previewContainer == null)
        //    previewContainer = new GameObject("PathPreviewContainer").transform;
        //
        //if (objTemp == null)
        //{
        //    //GameObject obj = new GameObject("DrawnPath");
        //    //objTemp = obj;
        //    objTemp = new GameObject("DrawnPath");
        //    var obj = objTemp;
        //    obj.transform.parent = previewContainer;
        //
        //    var lr = obj.AddComponent<LineRenderer>();
        //    rendererLineTemp = lr;
        //    lr.positionCount = points.Count;
        //    lr.SetPositions(points.ToArray());
        //    lr.material = new Material(Shader.Find("Sprites/Default"));
        //    lr.startColor = Color.white;
        //    lr.endColor = Color.white;
        //    lr.widthMultiplier = 0.1f;
        //    lr.useWorldSpace = true;
        //    lr.loop = false;
        //}
        //
        //rendererLineTemp.positionCount = points.Count;
        //rendererLineTemp.SetPositions(points.ToArray());
        //previewSegments.Add(lineRendererCircle);
    }


    // ====================== End Visualisers ============================= //

   
}
