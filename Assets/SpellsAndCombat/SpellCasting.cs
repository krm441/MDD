using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

/*
 
1) spell range bool => return path to range
2) spell visualisation
3) spell cast => damage calculator

 */

using System.Linq;
using Pathfinding;
using UnityEditor;

/// <summary>
/// Static backend utilities for path and spell range calculations
/// </summary>
public static class SpellRangeBackend
{
    // Cached reference to GridSystem in scene
    private static GridSystem _grid; 
    private static GridSystem grid
    {
        get
        {
            if (_grid == null)
            {
                _grid = GameObject.FindObjectOfType<GridSystem>();
            }
        return _grid;
        }
    }

    
    public static Path calculateRangeWalk(Vector3 start, Vector3 end, int ap, float speed, out bool inRange)
    {
        inRange = false;
        if (grid == null)
        {
            Debug.LogError("SpellRangeBackend: GridSystem not found in scene.");
            return null;
        }

        var nodes = grid.FindPathTo(end, start);
        if (nodes == null || nodes.Count == 0)
            return null;

        // Compute total path distance
        float totalDist = 0f;
        Vector3 prev = start;
        foreach (var node in nodes)
        {
            totalDist += Vector3.Distance(prev, node.worldPos);
            prev = node.worldPos;
        }

        // AP cost to walk full path
        int apCost = Mathf.CeilToInt(totalDist / speed);
        inRange = apCost <= ap;

        return new Path { pathNodes = nodes };
    }

    
    public static Path calculateRangeSpell(
        float spellRadius,
        int spellCost,
        int availableAP,
        float speed,
        Vector3 start,
        Vector3 end,
        out bool inRange)
    {
        inRange = false;
        if (grid == null)
        {
            Console.Error("SpellRangeBackend::calculateRangeSpell: GridSystem not found in scene.");
            return null;
        }

        var fullNodes = grid.FindPathTo(end, start);
        if (fullNodes == null || fullNodes.Count == 0)
            return null;

        // Build cumulative distance list
        List<float> cumulativeDist = new List<float>(fullNodes.Count);
        float totalDist = 0f;
        Vector3 prev = start;
        foreach (var node in fullNodes)
        {
            totalDist += Vector3.Distance(prev, node.worldPos);
            cumulativeDist.Add(totalDist);
            prev = node.worldPos;
        }

        // Distance needed to walk to be within spell radius
        float needDist = Mathf.Max(0f, totalDist - spellRadius);
        // AP left for movement
        int moveAP = Mathf.Max(0, availableAP - spellCost);
        float maxWalkDist = moveAP * speed;

        List<Node> moveNodes;

        if (needDist <= 0f)
        {
            // Already in range
            inRange = true;
            moveNodes = new List<Node>();
        }
        else if (needDist <= maxWalkDist)
        {
            // Reachable within AP budget
            inRange = true;
            // find first node where cumulative >= needDist
            int idx = cumulativeDist.FindIndex(d => d >= needDist);
            moveNodes = idx >= 0 ? fullNodes.Take(idx + 1).ToList() : new List<Node>(fullNodes);
        }
        else
        {
            // Cannot reach cast range within AP
            inRange = false;
            // truncate to AP budget
            int idx = cumulativeDist.FindIndex(d => d > maxWalkDist);
            moveNodes = idx >= 0 ? fullNodes.Take(idx).ToList() : new List<Node>(fullNodes);
        }

        return new Path { pathNodes = moveNodes };
    }
}

/// <summary>
/// Static frontend methods for runtime visualization of movement and spell ranges - under construction
/// </summary>
public static class SpellVisualizer
{
    /// <summary>
    /// Draws movement preview for given start to end with AP and speed.
    /// </summary>
    public static Pathfinding.Path VisualizePath(Vector3 start, Vector3 end, int ap, float speed, out bool inRange)
    {
        //bool inRange;
        var path = SpellRangeBackend.calculateRangeWalk(start, end, ap, speed, out inRange);
        if (path?.pathNodes == null)
        {
            AimingVisualizer.ClearPathPreview();
            return null;
        }
        float maxDist = ap * speed;
        AimingVisualizer.DrawPathPreview(start, end, path.pathNodes, maxDist);

        return path;
    }

    

    /// <summary>
    /// Visualizes both the movement path needed (if exists) and the spell impact area
    /// </summary>
    public static Pathfinding.Path VisualizeSpell(
        float spellRadius,
        float spellRange,
        int spellCost,
        int availableAP,
        float speed,
        Vector3 start,
        Vector3 end,
        out bool inRange)
    {
        inRange = false;
        bool canCast;

        Pathfinding.Path movePath = null;

        float dist = Vector3.Distance(start, end);
        if (dist < spellRange)
        {
            inRange = true;
        }
        else
        {
            movePath = SpellRangeBackend.calculateRangeSpell(
                spellRadius, spellCost, availableAP, speed, start, end, out canCast);

            if (movePath == null) 
            {
                Console.Error("here"); 
            }

            // clear old preview
            AimingVisualizer.ClearPathPreview();

            // Draw movement portion (if any)
            if (movePath?.pathNodes != null && movePath.pathNodes.Count > 0)
            {
                float moveAP = Mathf.Max(0, availableAP - spellCost);
                float maxDist = moveAP * speed - spellRadius / 2f;
                AimingVisualizer.DrawPathPreview(start, end, movePath.pathNodes, maxDist, true);
                inRange = canCast;
            }
        }

        // Draw spell radius at target
        var circleColor = inRange
            ? Color.green
            : Color.red;

        AimingVisualizer.ShowAimingCircle(end, spellRadius, circleColor);
        AimingVisualizer.DrawProjectileArc(start, end, circleColor); // ballistic trajectory
        AimingVisualizer.HighlightTargets(end, spellRadius);

        return movePath;
    }
}


public class SpellCasting : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   
}
