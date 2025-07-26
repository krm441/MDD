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
using TMPro;
using System.Threading;

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

    
    public static Path calculateRangeWalk(Vector3 start, Vector3 end, int ap, float speed, int agentID, int targetId, out bool inRange)
    {
        inRange = false;
        if (grid == null)
        {
            Debug.LogError("SpellRangeBackend: GridSystem not found in scene.");
            return null;
        }

        var nodes = grid.FindPathTo(end, start, agentID, targetId);
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
        float castRange,
        int spellCost,
        int availableAP,
        float speed,
        CharacterUnit caster,
        Vector3 target,
        out bool inRange
    )
    {
        //var points = target.ComputeFootprintPoints();

        //UnityEngine.Object.FindObjectOfType<GameManagerMDD>().gridSystem.FreeFootPrintPoints(points);

        var path = calculateRangeSpell(castRange, spellCost, availableAP, speed, caster.GetFeetPos(), target, caster, null, out inRange);

        //UnityEngine.Object.FindObjectOfType<GameManagerMDD>().gridSystem.MarkFootPrintPoints(points);

        return path;
    }

    // SpellRangeBackend.cs
    public static Path calculateRangeSpell(
        float castRange,
        int spellCost,
        int availableAP,
        float speed,
        Vector3 start,
        Vector3 end,
        CharacterUnit caster,
        CharacterUnit target,
        out bool inRange
    )
    {
        inRange = false;
        if (grid == null) { Console.Error("SpellRangeBackend::calculateRangeSpell::Err: grid == null"); return null; }

        // Special case: for non-combat state: if AP is negative, skip spell constraints — draw full walk path
        //if (availableAP < 0)
        //{
        //    var nodes = grid.FindPathTo(end, start);
        //    inRange = true;
        //    return new Path { pathNodes = nodes };
        //}

        Vector3 position = end; // world-space center
        float radius = 1.0f;

        Collider[] colliders = Physics.OverlapSphere(position, radius);

        //CharacterUnit unit = null;
        List<Node> fullNodes = null;

       //foreach (Collider col in colliders)
       //{
       //    //unit = col.GetComponent<CharacterUnit>();
       //    unit = col.GetComponentInParent<CharacterUnit>();
       //    if (unit != null)
       //    { 
       //        fullNodes = grid.FindPathTo(end, start, unit);
       //        break;
       //    }
       //}

        fullNodes = grid.FindPathTo(end, start, caster.unitID, target? target.unitID : -1);
        if (fullNodes == null || fullNodes.Count == 0) return null;

        // Build cumulative distances
        var cumulative = new List<float>(fullNodes.Count);
        float total = 0f;//, walked = 0f;
        Vector3 prev = start;
        foreach (var n in fullNodes)
        {
            total += Vector3.Distance(prev, n.worldPos);
            cumulative.Add(total);
            prev = n.worldPos;
        }

        // How far we must go to be in casting range
        float need = Mathf.Max(0f, total - castRange);
        // How far we can go on remaining AP
        int moveAP = int.MaxValue; 
        if (availableAP > 0)
            moveAP = Mathf.Max(0, availableAP - spellCost);
        float canGo = moveAP * speed;

        // Actual walk distance
        float walkDist = Mathf.Min(need, canGo);
        inRange = canGo >= need;

        // Take the prefix of nodes within walkDist
        var moveNodes = new List<Node>();
        if (walkDist > 0f)
        {
            float remaining = walkDist;
            Vector3 prevPos = start;

            foreach (var n in fullNodes)
            {
                float segLen = Vector3.Distance(prevPos, n.worldPos);
                if (remaining >= segLen)
                {
                    // We can reach this node fully
                    moveNodes.Add(n);
                    remaining -= segLen;
                    prevPos = n.worldPos;
                }
                else
                {
                    // We run out of budget mid‐segment: make a partial node
                    Vector3 partialPos = prevPos
                        + (n.worldPos - prevPos).normalized * remaining;
                    // Use partial node - accurate
                    var partialNode = new Pathfinding.Node(n.gridPos, partialPos, n.isWalkable);
                    moveNodes.Add(partialNode);
                    break;
                }
            }
        }

        return new Path { pathNodes = moveNodes };
    }

    
    //public static Path VisualizeSpell(
    //    float spellRadius,
    //    float spellRange,
    //    int spellCost,
    //    int availableAP,
    //    float speed,
    //    Vector3 start,
    //    Vector3 end,
    //    out bool inRange
    //)
    //{
    //    inRange = false;
    //    Path movePath = null;
    //
    //    float dist = Vector3.Distance(start, end);
    //    if (dist <= spellRange)
    //    {
    //        inRange = true;
    //    }
    //    else
    //    {
    //        // 1- how far to walk to cast
    //        bool canCast;
    //        movePath = SpellRangeBackend.calculateRangeSpell(
    //            spellRange,
    //            spellCost,
    //            availableAP,
    //            speed,
    //            start,
    //            end,
    //            out canCast
    //        );
    //
    //        // 2- draw just that walk
    //        if (movePath?.pathNodes != null && movePath.pathNodes.Count > 0)
    //        {
    //            int moveAP = Mathf.Max(0, availableAP - spellCost);
    //            float maxDist = moveAP * speed;           // full movement budget
    //            AimingVisualizer.DrawPathPreview(
    //                start, end,
    //                movePath.pathNodes,
    //                maxDist,
    //                true
    //            );
    //            inRange = canCast;
    //        }
    //    }
    //
    //    // 3- now show the aiming circle at the target
    //    var circleColor = inRange ? Color.green : Color.red;
    //    AimingVisualizer.ShowAimingCircle(end, spellRadius, circleColor);
    //    //AimingVisualizer.DrawProjectileArc(start, end, circleColor, out false);
    //    AimingVisualizer.HighlightTargets(end, spellRadius);
    //
    //    return movePath;
    //}
}

/// <summary>
/// Static frontend methods for runtime visualization of movement and spell ranges - under construction
/// </summary>
public static class SpellVisualizer
{
    /// <summary>
    /// Draws movement preview for given start to end with AP and speed.
    /// </summary>
    public static Pathfinding.Path VisualizePath(CharacterUnit starter, Vector3 end, int ap, float speed, out bool inRange)
    {
        //bool inRange;
        var path = SpellRangeBackend.calculateRangeWalk(starter.GetFeetPos(), end, ap, speed, starter.unitID, -1, out inRange);
        if (path?.pathNodes == null)
        {
            AimingVisualizer.ClearPathPreview();
            return null;
        }
        float maxDist = ap * speed;
        
        var res = AimingVisualizer.DrawPathPreview(starter.GetFeetPos(), end, path.pathNodes, maxDist);

        return res;
    }

    /// <summary>
    /// Visualizes both the movement path needed (if exists) and the spell impact area
    /// </summary>
    public static Pathfinding.Path VisualizeSpell(
        Spell spell,
        int availableAP,
        float speed,
        CharacterUnit start,
        Vector3 end,
        out bool inRange)
    {
        inRange = false;
        bool canCast;

        Pathfinding.Path movePath = null;

        /* Variables for spell 
        float spellRadius,
        float spellRange,
        int spellCost,
        
        */



        float dist = Vector3.Distance(start.GetFeetPos(), end);
        if (dist < spell.range)
        {
            inRange = true;
        }
        else
        {
            movePath = SpellRangeBackend.calculateRangeSpell(
                spell.range, spell.apCost, availableAP, speed, start, end, out canCast);

            // clear old preview
            AimingVisualizer.ClearPathPreview();

            // Draw movement portion (if any)
            if (movePath?.pathNodes != null && movePath.pathNodes.Count > 0)
            {
                float moveAP = Mathf.Max(0, availableAP - spell.apCost);
                float maxDist = moveAP * speed;// - spellRadius / 2f;
                if (availableAP < 0)
                    maxDist = float.MaxValue;
                AimingVisualizer.DrawPathPreview(start.GetFeetPos(), end, movePath.pathNodes, maxDist, true);
                inRange = canCast;
            }
        }

        

        switch (spell.physicsType)
        {
            case SpellPhysicsType.Parabolic:
                {
                    bool obstacle = false;
                    if (movePath != null && movePath.pathNodes.Count > 0)
                        AimingVisualizer.DrawProjectileArc(movePath.pathNodes.Last().worldPos, end, Color.green, out obstacle);
                    else
                        AimingVisualizer.DrawProjectileArc(start.GetFeetPos(), end, Color.green, out obstacle); // ballistic trajectory
                    inRange = !obstacle;
                }
                break;
            case SpellPhysicsType.Linear:
                {
                    bool obstacle = false;
                    if (movePath != null && movePath.pathNodes.Count > 0)
                        AimingVisualizer.DrawStraightLine(movePath.pathNodes.Last().worldPos, end, Color.green, out obstacle);
                    else
                        AimingVisualizer.DrawStraightLine(start.GetFeetPos(), end, Color.green, out obstacle); // rectilinear trajectory
                    inRange = !obstacle;
                }
                break;
            default:
                Console.Warn("Unknown spell effect.");
                break;
        }

        // Draw spell radius at target
        var circleColor = inRange
            ? Color.green
            : Color.red;
        AimingVisualizer.ShowAimingCircle(end, spell.radius, circleColor);

        if (inRange)
            AimingVisualizer.HighlightTargets(end, spell.radius);

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
