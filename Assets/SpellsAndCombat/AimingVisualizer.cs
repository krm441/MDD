using System.Collections.Generic;
using System;//.Drawing;
using UnityEngine;

public static class AimingVisualizer
{
    private static GameObject circleObj;
    private static LineRenderer lr;
    private static List<Renderer> highlighted = new List<Renderer>();
    private static Color highlightColor = Color.white * 0.5f;

    private static int lastSegments = 32;

    public static void DrawImpactCircle(Vector3 center, float radius, int segments = 32)
    {
        GameObject circleObj = new GameObject("SpellRangeCircle");
        LineRenderer lr = circleObj.AddComponent<LineRenderer>();

        lr.positionCount = segments + 1;
        lr.loop = true;
        lr.widthMultiplier = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = Color.red;

        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + center;
            lr.SetPosition(i, pos);
        }

        GameObject.Destroy(circleObj, 1.5f); // destroy after 1.5 sec
    }

    public static void ShowAimingCircle(Vector3 center, float radius, int segments = 32)
    {
        if (circleObj == null)
        {
            circleObj = new GameObject("AimingCircle");
            lr = circleObj.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.widthMultiplier = 0.05f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
            lr.positionCount = segments + 1;
            lastSegments = segments;
        }

        UpdateAimingCircle(center, radius, segments);
    }

    public static void UpdateAimingCircle(Vector3 center, float radius, int segments = 32)
    {
        if (circleObj == null || lr == null) return;
        if (segments != lastSegments)
        {
            lr.positionCount = segments + 1;
            lastSegments = segments;
        }

        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + center;
            lr.SetPosition(i, pos);
        }
    }

    private static GameObject pathLineObj;
    //private static LineRenderer pathLineRenderer;
    private static float pathWidth = 0.1f;
    /*public static void ClearPathPreview()
    {
        if (pathLineRenderer != null)
            pathLineRenderer.positionCount = 0;
    }*/

    private static List<LineRenderer> previewSegments = new List<LineRenderer>();
    private static Transform previewContainer;
    public static List<Vector3> reachablePath = new List<Vector3>();  // Stores white path only

    // Draw path preview, white if traversable, red if not, with a gradient in between 
    public static void DrawPathPreview(Vector3 start, Vector3 end, List<Pathfinding.Node> path, float maxDistance)
    {
        // Always clear previous visual path to avoid leftover segments
        ClearPathPreview(); reachablePath.Clear();

        // Exit early if there's no valid path
        if (path == null || path.Count == 0) return;

        // Create a parent container for organization (only once)
        if (previewContainer == null)
            previewContainer = new GameObject("PathPreviewContainer").transform;

        // Flatten the path into a list of Vector3 world positions, starting from the unit's position
        List<Vector3> pathPoints = new List<Vector3> { start };
        foreach (var node in path)
            pathPoints.Add(node.worldPos);

        // Vertical offset to raise the path above the ground
        Vector3 elevation = new Vector3(0, 0.6f);

        float distanceSoFar = 0f;         // Tracks cumulative distance walked
        float stepSize = 1f;              // Distance between sampled path points
        Vector3 current = start;          // Current position along the path
        int currentIndex = 0;             // Index of the current segment in the full path

        List<Vector3> currentSegmentPoints = new List<Vector3>(); // List of points in the current LineRenderer segment
        Color currentColor = Color.white;                         // Starting color (white = in range)

        currentSegmentPoints.Add(current + elevation); // Start from the unit's position

        // Traverse the full path, segment-by-segment
        while (currentIndex < pathPoints.Count - 1)
        {
            Vector3 a = pathPoints[currentIndex];         // Current segment start
            Vector3 b = pathPoints[currentIndex + 1];     // Segment end
            float segLength = Vector3.Distance(a, b);     // Length of segment
            Vector3 dir = (b - a).normalized;             // Direction of segment
            float remaining = segLength;                  // Distance still to walk in this segment

            // Walk along the current segment in `stepSize` increments
            while (remaining >= stepSize)
            {
                current += dir * stepSize;
                distanceSoFar += stepSize;

                // Decide what color this point should be (white if in range, red if not)
                Color newColor = distanceSoFar <= maxDistance ? Color.white : Color.red;
                Vector3 elevated = current + elevation;

                // the OUT variable = white distance covered
                if (distanceSoFar <= maxDistance)
                    reachablePath.Add(elevated);

                // Color transition: if we've crossed from white to red (or vice versa)
                if (newColor != currentColor)
                {
                    // Insert a short blending segment between last white and first red point
                    if (currentSegmentPoints.Count > 0)
                    {
                        Vector3 blendStart = currentSegmentPoints[currentSegmentPoints.Count - 1];
                        Vector3 blendEnd = elevated;
                        CreateGradientSegment(blendStart, blendEnd, currentColor, newColor);
                    }

                    // Finalize the current white/red segment
                    CreateSegment(currentSegmentPoints, currentColor);
                    currentSegmentPoints.Clear();

                    // Start the new segment with the current point
                    currentSegmentPoints.Add(elevated);
                    currentColor = newColor;
                }

                // Add this point to the current segment
                currentSegmentPoints.Add(elevated);
                remaining -= stepSize;
            }

            currentIndex++;
        }

        // Add the final segment (if it has at least two points)
        if (currentSegmentPoints.Count > 1)
            CreateSegment(currentSegmentPoints, currentColor);
    }

    private static LineRenderer pathLineRenderer;
    //private static LineRenderer pathLineRenderer;
    //private static Transform previewContainer;
    public static void ClearStaticPath()
    {
        if (pathLineRenderer != null)
            pathLineRenderer.positionCount = 0;
    }

    public static void DrawPathf(List<Vector3> points)
    {
        if (points == null || points.Count < 2)
        {
            ClearStaticPath(); // Clear if path is too short
            return;
        }

        if (previewContainer == null)
            previewContainer = new GameObject("PathPreviewContainer").transform;

        if (pathLineRenderer == null)
        {
            GameObject obj = new GameObject("DrawnPath");
            obj.transform.SetParent(previewContainer);

            pathLineRenderer = obj.AddComponent<LineRenderer>();
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathLineRenderer.widthMultiplier = 0.1f;
            pathLineRenderer.useWorldSpace = true;
            pathLineRenderer.loop = false;
        }

        pathLineRenderer.startColor = Color.white;
        pathLineRenderer.endColor = Color.white;

        pathLineRenderer.positionCount = points.Count;
        pathLineRenderer.SetPositions(points.ToArray());
    }

    private static GameObject objTemp;
    private static LineRenderer rendererLineTemp; 

    public static void ClearState()
    {
        //rendererLineTemp.positionCount = 0;
        //rendererLineTemp = null; 
        GameObject.Destroy(objTemp);
        GameObject.Destroy(GameObject.Find("PathPreviewContainer"));
        reachablePath.Clear();
        //objTemp = null;
    }

    public static void DrawPath(List<Vector3> points)
    {
        ClearPathPreview(); // clear previous render

        if (points == null || points.Count < 2)
            return;

        if (previewContainer == null)
            previewContainer = new GameObject("PathPreviewContainer").transform;

        if (objTemp == null)
        {
            //GameObject obj = new GameObject("DrawnPath");
            //objTemp = obj;
            objTemp = new GameObject("DrawnPath");
            var obj = objTemp;
            obj.transform.parent = previewContainer;

            var lr = obj.AddComponent<LineRenderer>();
            rendererLineTemp = lr;
            lr.positionCount = points.Count;
            lr.SetPositions(points.ToArray());
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.white;
            lr.endColor = Color.white;
            lr.widthMultiplier = 0.1f;
            lr.useWorldSpace = true;
            lr.loop = false;
        }

        rendererLineTemp.positionCount = points.Count;
        rendererLineTemp.SetPositions(points.ToArray());
        previewSegments.Add(lr);
    }

    public static void DrawPath2(List<Vector3> points)
    {
        if (points == null || points.Count < 2)
            return;

        if (previewContainer == null)
            previewContainer = new GameObject("PathPreviewContainer").transform;

        // Reuse or create once
        if (pathLineRenderer == null)
        {
            GameObject obj = new GameObject("DrawnPath");
            obj.transform.parent = previewContainer;

            pathLineRenderer = obj.AddComponent<LineRenderer>();
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathLineRenderer.widthMultiplier = 0.1f;
            pathLineRenderer.useWorldSpace = true;
            pathLineRenderer.loop = false;
            pathLineRenderer.startColor = Color.white;
            pathLineRenderer.endColor = Color.white;
        }

        pathLineRenderer.positionCount = points.Count;
        pathLineRenderer.SetPositions(points.ToArray());
    }


    // Creates a short 2-point LineRenderer that blends from one color to another
    private static void CreateGradientSegment(Vector3 from, Vector3 to, Color startColor, Color endColor)
    {
        GameObject obj = new GameObject("BlendSegment");
        obj.transform.parent = previewContainer;

        var lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.widthMultiplier = 0.1f;
        lr.useWorldSpace = true;
        lr.loop = false;

        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[]
            {
            new GradientColorKey(startColor, 0f),
            new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[]
            {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
            });

        lr.colorGradient = g;

        previewSegments.Add(lr);
    }

    // Creates a solid-colored LineRenderer from a list of points
    private static void CreateSegment(List<Vector3> points, Color color)
    {
        GameObject obj = new GameObject("PathSegment");
        obj.transform.parent = previewContainer;

        var lr = obj.AddComponent<LineRenderer>();
        lr.positionCount = points.Count;
        lr.SetPositions(points.ToArray());
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.widthMultiplier = 0.1f;
        lr.useWorldSpace = true;
        lr.loop = false;

        previewSegments.Add(lr);
    }

    // Deletes all previous LineRenderers
    public static void ClearPathPreview()
    {
        if (previewSegments.Count > 0)
        {
            foreach (var seg in previewSegments)
                if (seg != null)
                    GameObject.Destroy(seg.gameObject);
            previewSegments.Clear();
        }
    }


    public static void DrawPathPreview13(Vector3 start, Vector3 end, List<Pathfinding.Node> path, float maxDistance)
    {
        if (path == null || path.Count == 0)
        {
            ClearPathPreview();
            return;
        }

        if (pathLineObj == null)
        {
            pathLineObj = new GameObject("PathPreviewLine");
            pathLineRenderer = pathLineObj.AddComponent<LineRenderer>();
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathLineRenderer.widthMultiplier = 0.1f;
            pathLineRenderer.loop = false;
            pathLineRenderer.useWorldSpace = true;
        }

        float totalDistance = 0f;
        totalDistance += Vector3.Distance(start, path[0].worldPos);
        for (int i = 0; i < path.Count; i++)
        {
            if (i > 0)
                totalDistance += Vector3.Distance(path[i - 1].worldPos, path[i].worldPos);
        }

        int count = Mathf.CeilToInt(maxDistance);

        pathLineRenderer.positionCount = count;// path.Count + 1;
        pathLineRenderer.startColor = Color.white;
        pathLineRenderer.endColor = Color.white;

        Vector3 elevation = new Vector3(0, 0.6f);

        pathLineRenderer.SetPosition(0, start + elevation);

        List<Vector3> positions = new List<Vector3>();
        List<GradientColorKey> colorKeys = new List<GradientColorKey>();
        List<GradientAlphaKey> alphaKeys = new List<GradientAlphaKey>();

        // Start point
        Vector3 prev = start + elevation;
        positions.Add(prev);
        float distanceSoFar = 0f;
        colorKeys.Add(new GradientColorKey(Color.white, 0f));
        alphaKeys.Add(new GradientAlphaKey(1f, 0f));

        Debug.Log("zabre " + count + " max: " + maxDistance);

        // Walk through the path and subdivide
        for (int i = 1; i < count; i++)
        {
            float interpolationRatio = (float)i / (float)count;

            Vector3 interpolatedPosition = Vector3.Lerp(Vector3.up, Vector3.forward, interpolationRatio);

            Vector3 next = interpolatedPosition; // here use some interpolation to find the vec3 point
            float segmentLength = Vector3.Distance(prev, next);
            distanceSoFar += segmentLength;

            float t = Mathf.Clamp01(distanceSoFar / totalDistance);
            Color color = distanceSoFar <= maxDistance ? Color.white : Color.red;

            positions.Add(next);
            colorKeys.Add(new GradientColorKey(color, t));
            alphaKeys.Add(new GradientAlphaKey(1f, t));

            prev = next;
        }

        // Set LineRenderer
        pathLineRenderer.positionCount = positions.Count;
        pathLineRenderer.SetPositions(positions.ToArray());

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys.ToArray(), alphaKeys.ToArray());
        pathLineRenderer.colorGradient = gradient;
    }

   /* Debug.Log("POSITION " +  path[0].worldPos);
        float distanceSoFar = 0f;
        distanceSoFar += Vector3.Distance(start, path[0].worldPos);
        for (int i = 0; i < path.Count; i++)
        {
            pathLineRenderer.SetPosition(i + 1, path[i].worldPos + elevation);

            if (i > 0)
                distanceSoFar += Vector3.Distance(path[i - 1].worldPos, path[i].worldPos);
        }

        // Decide gradient color split
        // Recalculate totalDistance for full path
        float totalDistance = 0f;
        for (int i = 1; i < path.Count; i++)
            totalDistance += Vector3.Distance(path[i - 1].worldPos, path[i].worldPos);

        float maxDist = Mathf.Max(0.001f, maxDistance); // avoid divide by zero
        float affordRatio = Mathf.Clamp01(maxDist / totalDistance);

        // Set color gradient split
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.white, affordRatio),
            new GradientColorKey(Color.red, affordRatio + 0.001f),
            new GradientColorKey(Color.red, 1f)
        };

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        gradient.SetKeys(colorKeys, alphaKeys);
        pathLineRenderer.colorGradient = gradient;
    }*/


    public static void DrawPathPreview2(Vector3 start, List<Pathfinding.Node> path, float maxDistance)
    {
        if (path == null || path.Count < 2)
        {
            ClearPathPreview();
            return;
        }

        if (pathLineObj == null)
        {
            pathLineObj = new GameObject("PathPreviewLine");
            pathLineRenderer = pathLineObj.AddComponent<LineRenderer>();
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathLineRenderer.widthMultiplier = pathWidth;
            pathLineRenderer.loop = false;
            pathLineRenderer.useWorldSpace = true;
            pathLineRenderer.positionCount = 0;
        }

        pathLineRenderer.positionCount = path.Count;

        float distanceSoFar = 0f;
        distanceSoFar += Vector3.Distance(start, path[0].worldPos);
        pathLineRenderer.SetPosition(0, start);

        for (int i = 1; i < path.Count; i++)
        {
            pathLineRenderer.SetPosition(i, path[i].worldPos);

            if (i > 0)
                distanceSoFar += Vector3.Distance(path[i - 1].worldPos, path[i].worldPos);
        }

        // Decide gradient color split
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys;
        GradientAlphaKey[] alphaKeys;

        float threshold = Mathf.Clamp01(maxDistance / Mathf.Max(distanceSoFar, 0.001f));
        colorKeys = new GradientColorKey[]
        {
        new GradientColorKey(Color.white, 0f),
        new GradientColorKey(Color.white, threshold),
        new GradientColorKey(Color.red, threshold + 0.01f),
        new GradientColorKey(Color.red, 1f)
        };

        alphaKeys = new GradientAlphaKey[]
        {
        new GradientAlphaKey(1f, 0f),
        new GradientAlphaKey(1f, 1f)
        };

        gradient.SetKeys(colorKeys, alphaKeys);
        pathLineRenderer.colorGradient = gradient;
    }


    public static void HighlightTargets(Vector3 center, float radius)
    {
        ClearHighlights();

        var hits = Physics.OverlapSphere(center, radius, LayerMask.GetMask("Characters", "Destructibles"));
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

    public static void Hide()
    {
        if (circleObj != null)
        {
            UnityEngine.Object.Destroy(circleObj);
            circleObj = null;
            lr = null;
        }

        ClearHighlights();
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
}
