using System.Collections.Generic;
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

    public static void Show(Vector3 center, float radius, int segments = 32)
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

        UpdateCircle(center, radius, segments);
    }

    public static void UpdateCircle(Vector3 center, float radius, int segments = 32)
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
            Object.Destroy(circleObj);
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
