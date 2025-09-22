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
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;
using System.IO;
using UnityEngine.SocialPlatforms;


/// <summary>
/// Static frontend methods for runtime visualization of movement and spell ranges - under construction
/// </summary>
public static class SpellVisualizer
{
    public static void VisualizePath(NavMeshPath path)
    {

    }



    public static NavMeshPath GetPathAtDistance(Vector3 start, Vector3 end, float targetDistance, float maximumDistance = -1f)
    {
        NavMeshPath fullPath = new NavMeshPath();

        // early returns
        if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, fullPath))
            return null;



        float accumulated = 0f;
        Vector3 finalPoint = Vector3.zero;

        for (int i = fullPath.corners.Length - 1; i > 0; i--)
        {
            Vector3 p0 = fullPath.corners[i];
            Vector3 p1 = fullPath.corners[i - 1];
            float segmentLength = Vector3.Distance(p0, p1);

            if (accumulated + segmentLength >= targetDistance)
            {
                float remaining = targetDistance - accumulated;
                float t = remaining / segmentLength;
                finalPoint = Vector3.Lerp(p0, p1, t);
                break;
            }

            accumulated += segmentLength;
        }

        if (finalPoint == Vector3.zero) return null;

        if (!NavMesh.CalculatePath(start, finalPoint, NavMesh.AllAreas, fullPath))
            return null;

        if (maximumDistance != -1f)
        {
            var pathDistance = MathMDD.CalculatePathDistance(fullPath);
            if (pathDistance > maximumDistance)
                return null;
        }

        return fullPath;
    }


    public static bool VisualizeSpell(
        Spell spell,
        int availableAP,
        float speed,
        Vector3 start,
        Vector3 end,
        out List<GameObject> hitTargets)
    {
        hitTargets = null;
        bool inRange = true;

        // 1) check distance
        float dist = Vector3.Distance(start, end);
        if (dist > spell.range)
        {
            inRange = false;
        }

        // 2) chose visualisation type
        bool obstacle = true;
        if(inRange || spell.physicsType == SpellPhysicsType.Static)
            switch (spell.physicsType)
            {
                case SpellPhysicsType.Parabolic:
                    {
                        AimingVisualizer.DrawProjectileArc(start, end, Color.green, out obstacle); // ballistic trajectory                   
                    }
                    break;
                case SpellPhysicsType.Linear:
                    {
                        AimingVisualizer.DrawStraightLine(start, end, Color.green, out obstacle); // rectilinear trajectory
                    }
                    break;
                case SpellPhysicsType.Static: 
                    end = start; inRange = true; obstacle = false;
                    break;
                default:
                    Console.Warn("Unknown spell effect.");
                    break;
            }
        else
        {
            AimingVisualizer.ClearProjectileArc();
        }

        // Draw spell radius at target
        var circleColor = !obstacle
            ? Color.green
            : Color.red;
        AimingVisualizer.ShowAimingCircle(end, spell.radius, circleColor);

        if (!obstacle)
            AimingVisualizer.HighlightTargets(end, spell.radius, out hitTargets);

        return inRange && !obstacle;
    }
}