using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEngine;
public enum BTState { Success, Failure, Running }

public abstract class BTNode
{
    public abstract BTState Tick();
}

/// <summary>
/// Sequence : AND logic
/// </summary>
public class Sequence : BTNode
{
    private readonly List<BTNode> children;

    public Sequence(params BTNode[] nodes) => children = new List<BTNode>(nodes);

    public override BTState Tick()
    {
        foreach (var child in children)
        {
            var result = child.Tick();
            if (result != BTState.Success)
                return result; // Failure or Running
        }
        return BTState.Success; // all must succeed - AND logic
    }
}

/// <summary>
/// Selector : OR logic
/// </summary>
public class Selector : BTNode
{
    private readonly List<BTNode> children;

    public Selector(params BTNode[] nodes)
    {
        children = new List<BTNode>(nodes);
    }

    public override BTState Tick()
    {
        foreach (var child in children)
        {
            BTState result = child.Tick();

            if (result == BTState.Success)
                return BTState.Success;

            if (result == BTState.Running)
                return BTState.Running;
        }

        return BTState.Failure; // Only one child could succeed; return it - OR logic
    }
}

// ====================== BT NODES ===================== //

/// <summary>
/// Picks the targets in radius, sorts them from nearest to farthest
/// </summary>
public class PickTargetRadius : BTNode
{
    public override BTState Tick()
    {
        return BTState.Running;
    }
}

/// <summary>
/// Builds Theta* paths to the targets. If the nearest target path is valid, pick it as valid target
/// </summary>
public class PickNearestBuildPath : BTNode
{
    public override BTState Tick() { return BTState.Running; }
}

/// <summary>
/// Ranged based attack. Hit enemies in bound.
/// </summary>
public class CastProjectile : BTNode
{
    public override BTState Tick() { return BTState.Running; }
}

/// <summary>
/// Casts AoE on several targets
/// </summary>
public class CastAoE : BTNode
{
    public override BTState Tick() { return BTState.Running; }
}

public class MoveAlongPath : BTNode
{    public override BTState Tick() { return BTState.Running; }
}

public class KeepDistance : BTNode
{
    public override BTState Tick() { return BTState.Running; }
}

/*
Possible tree:
    BTNode tree = new Selector(
        new Sequence(
            new IsHealthLow(),
            new TryHeal(),
            new RunAway(),
            new Surrender()
            ),
        new Sequence(
            new PickTargetRadius, // fill the context with data - is enemy visible
            new CastAoE, // try include several target in AoE
            new KeepDistance, // ranged based NPC should try keep distance from player
            ),
    );
*/

// ====================== BT Manager =================== //

public class BTContext
{
    // player party - thought not limited, what if in certain scenes enemies should fight with another emeny party
    public List<CharacterUnit> PotentialTargets;
    public GridSystem Grid;
}