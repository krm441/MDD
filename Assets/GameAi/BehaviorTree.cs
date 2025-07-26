using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Linq;
using static UnityEngine.GraphicsBuffer;
using System.IO;
using static UnityEngine.UI.CanvasScaler;
using System.Drawing;

#region BT Core
public class GlobalStringReportAI
{
    public static void Report()
    {
        Console.Error("Report", sequence);
    }

    public static void Append(string text)
    {
        sequence += "\n" + text;
    }
    private static string sequence;
}

public enum BTState { Success, Failure, Running }

public abstract class BTNode
{
    public abstract BTState Tick(BTblackboard blackboard);
}

/// <summary>
/// Sequence : AND logic
/// </summary>
public class Sequence : BTNode
{
    private readonly List<BTNode> children;

    public Sequence(params BTNode[] nodes) => children = new List<BTNode>(nodes);

    public override BTState Tick(BTblackboard blackboard)
    {
        foreach (var child in children)
        {
            var result = child.Tick(blackboard);
            if (result != BTState.Success)
                return result; // Failure or Running
        }
        return BTState.Success; // all must succeed : AND logic
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

    public override BTState Tick(BTblackboard blackboard)
    {
        foreach (var child in children)
        {
            BTState result = child.Tick(blackboard);

            if (result == BTState.Success)
                return BTState.Success;

            if (result == BTState.Running)
                return BTState.Running;
        }

        return BTState.Failure; // Only one child could succeed -> return it : OR logic
    }
}
#endregion

// ====================== BT NODES ===================== //

// --------------------------------------------------- IDLE ----------------------------------------------- //
#region IdleLogic: Gathering 
public static class ResourceOccupancyManager
{
    private static readonly HashSet<GameObject> occupiedResources = new HashSet<GameObject>();

    public static bool TryOccupy(GameObject resource)
    {
        if (resource == null || occupiedResources.Contains(resource))
            return false;

        occupiedResources.Add(resource);
        return true;
    }

    public static void Release(GameObject resource)
    {
        occupiedResources.Remove(resource);
    }

    public static bool IsOccupied(GameObject resource)
    {
        return occupiedResources.Contains(resource);
    }

    public static void ClearAll() => occupiedResources.Clear();
}


public class CheckCombatStateFalse : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        var gamemanager = context.gameManager;
        if(gamemanager.IsCombat())
        {
            return BTState.Failure;
        }

        return BTState.Success;
    }
}

public class CheckCombatStateTrue : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        var gamemanager = context.gameManager;
        if (gamemanager.IsCombat())
        {
            return BTState.Success;
        }

        return BTState.Failure;
    }
}

public class FindResourceInRadius : BTNode
{
    private float radius;

    public FindResourceInRadius(float searchRadius)
    {
        radius = searchRadius;
    }

    public override BTState Tick(BTblackboard context)
    {
        var unit = context.Caster;

        if (context.SelectedResource.TryGetValue(unit, out var value) && value != null)
        {
            return BTState.Success;
        }

        var center = unit.transform.position;

        Collider[] hits = Physics.OverlapSphere(center, radius, LayerMask.GetMask("Resources"));
        if (hits.Length == 0) return BTState.Failure;

        foreach (var hit in hits.OrderBy(h => Vector3.Distance(center, h.transform.position)))
        {
            var res = hit.gameObject;
            if (!ResourceOccupancyManager.IsOccupied(res))
            {
                if (ResourceOccupancyManager.TryOccupy(res))
                {
                    context.SelectedResource[unit] = res;
                    return BTState.Success;
                }
            }
        }

        return BTState.Failure; // all are occupied
                
    }
}

public class MoveToResource : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        //if (context.SelectedResource == null) return BTState.Failure;
        var unit = context.Caster;
        if (context.AcquiredResource.TryGetValue(unit, out var value) && value != null)
        {
            return BTState.Success;
        }

        var coroutineKey = $"npc_move_to_resource_{context.Caster.unitID}";

        var castingSpellCoroutine = context.gameManager.GetCoroutine(coroutineKey);

        if (castingSpellCoroutine?.IsRunning == true)
        {
            return BTState.Running;
        }
        else if (castingSpellCoroutine?.IsRunning == false)
        {
            context.gameManager.RemoveCoroutine(coroutineKey);
            return BTState.Success;
        }

       
        var targetPos = context.SelectedResource[unit].transform.position;

        var path = context.Grid.FindPathTo(targetPos, unit.transform.position, unit.unitID, -1);
        if (path == null) return BTState.Failure;

        unit.MoveAlongPath(path);
        context.gameManager.CreateCoroutine(coroutineKey, MoveCoroutine(unit, path));
        return BTState.Running;
    }

    private IEnumerator MoveCoroutine(CharacterUnit unit, List<Pathfinding.Node> path)
    {
        yield return unit.WaitForMovement(); // wait until movement is over
        
        //yield return null;
    }
}

public class HarvestResource : BTNode
{
    public  BTState Tick5(BTblackboard context)
    {
        var unit = context.Caster;
        if (!context.SelectedResource.TryGetValue(unit, out var resource) || resource == null)
            return BTState.Failure;

        var coroutineKey = $"npc_mining_{unit.unitID}";
        var miningCoroutine = context.gameManager.GetCoroutine(coroutineKey);

        if (miningCoroutine?.IsRunning == true)
            return BTState.Running;

        if (miningCoroutine?.IsRunning == false)
        {
            context.gameManager.RemoveCoroutine(coroutineKey);
            return BTState.Success;
        }

        //context.gameManager.CreateCoroutine(coroutineKey, MineAnimation(unit, resource));
        return BTState.Running;
    }

    public override BTState Tick(BTblackboard context)
    {
        var unit = context.Caster;
        //if (context.SelectedResource.TryGetValue(unit, out var value) && value != null)
        //{
        //    return BTState.Success;
        //}

        var resource = context.SelectedResource[unit];

        if (resource == null) return BTState.Failure;

        var coroutineKey = $"npc_mining_{context.Caster.unitID}";

        var castingSpellCoroutine = context.gameManager.GetCoroutine(coroutineKey);

        if (castingSpellCoroutine?.IsRunning == true)
        {
            return BTState.Running;
        }
        else if (castingSpellCoroutine?.IsRunning == false)
        {
            context.gameManager.RemoveCoroutine(coroutineKey);
            return BTState.Success;
        }

        // play animation or effect
        context.gameManager.CreateCoroutine(coroutineKey, MineAnimation(unit, resource, context));

        //context.HasResource = true;
        return BTState.Running;
    }

    private IEnumerator MineAnimation(CharacterUnit unit, GameObject resource, BTblackboard context)
    {
        Console.Log("Mining...");
        yield return new WaitForSeconds(2f); // Simulate mining

        context.AcquiredResource[unit] = resource;

        
        
        //GameObject.Destroy(resource); // or disable
    }
}

public class MoveToStockpile : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        var coroutineKey = $"npc_return_{context.Caster.unitID}";

        var castingSpellCoroutine = context.gameManager.GetCoroutine(coroutineKey);

        if (castingSpellCoroutine?.IsRunning == true)
        {
            return BTState.Running;
        }
        else if (castingSpellCoroutine?.IsRunning == false)
        {
            context.gameManager.RemoveCoroutine(coroutineKey);
            return BTState.Success;
        }

        var caster = context.Caster;
        var targetPos = context.StockpilePosition.position;

        var path = context.Grid.FindPathTo(targetPos, caster.transform.position, caster.unitID, -1);
        if (path == null) return BTState.Failure;

        context.Caster.MoveAlongPath(path); // starts movement
        context.gameManager.CreateCoroutine(coroutineKey, WaitForArrival(context.Caster, context));

        return BTState.Running;
    }

    private IEnumerator WaitForArrival(CharacterUnit unit, BTblackboard context)
    {
        yield return unit.WaitForMovement(); // wait til movement is over
        Console.Log("Returned to stockpile.");
        //ResourceOccupancyManager.Release(context.SelectedResource[unit]);
        //context.SelectedResource[unit] = null;
        context.AcquiredResource[unit] = null;
    }

}
#endregion

// ---------------------------------------------------COMBAT----------------------------------------------- //
#region Combat
/// <summary>
/// Picks the targets in radius, sorts them from nearest to farthest
/// </summary>
public class PickTargetRadius : BTNode
{
    public override BTState Tick(BTblackboard context)
    {

        var caster = context.Caster;

        var closest = context.PotentialTargets
            .Where(t => !t.IsDead)
            .OrderBy(t => Vector3.Distance(caster.transform.position, t.transform.position))
            .FirstOrDefault();

        if (closest == null)
            return BTState.Failure;

        context.SelectedTargetUnit = closest;
        context.SelectedTargetPosition = closest;//.transform.position;
        return BTState.Success;
    }
}

public class CalculateSpellPath : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        var caster = context.Caster;
        caster.SelectSpell(caster.spellBook.GetAllSpells()[0]);
        var spell = caster.GetSelectedSpell();
        var target = context.SelectedTargetPosition;
        var stats = caster.attributeSet.stats;

        // ---------- Line of Sight (LoS) check ----------
        Vector3 from = caster.transform.position + Vector3.up * 1.5f;
        Vector3 to = target.GetFeetPos();//.GetChestPos();
        Vector3 dir = to - from;
        float dist = dir.magnitude;
        int obstaclesLayer = LayerMask.GetMask("Obstacles");

        if (Physics.Raycast(from, dir, dist, obstaclesLayer))
        {
            // LoS blocked!
            return BTState.Failure;
        }
        // -----------------------------------------------

        context.MovePath = SpellRangeBackend.calculateRangeSpell(
            spell.range,
            spell.apCost,
            stats.ActionPoints,
            stats.Speed,
            caster.GetFeetPos(),
            target.GetFeetPos(),
            caster, 
            target,
            out bool canCast
        );

        if(canCast)
        {
            var coroutineKey = $"npc_pursue_{caster.unitID}";
            context.gameManager.GetCoroutine(coroutineKey)?.Stop();
            return BTState.Success;
        }

        return canCast ? BTState.Success : BTState.Failure;
    }
}

public class CastSpell : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        var caster  = context.Caster;
        var coroutineKey = $"ai_casting_spell_{context.Caster.unitID}";

        var castingSpellCoroutine = context.gameManager.GetCoroutine(coroutineKey);

        if (castingSpellCoroutine?.IsRunning == true)
        {
            return BTState.Running;
        }
        else if(castingSpellCoroutine?.IsRunning == false)
        {
            context.gameManager.RemoveCoroutine(coroutineKey);
            return BTState.Success; // exit - spell animation finished
        }

        var path = context.MovePath;

        context.gameManager.CreateCoroutine(coroutineKey, CastSpellAI(caster, path, context.SelectedTargetUnit, context.gameManager, context));

        return BTState.Running;
    }

    public IEnumerator CastSpellAI(CharacterUnit caster, Pathfinding.Path path, CharacterUnit target, GameManagerMDD gameManager, BTblackboard context)
    {
        Console.Log("Casitng spell AI");

        caster.LookAtTarget(target.transform.position);

        yield return caster.CastSpellWithMovement(
                caster,
                gameManager.CombatManager,
                path,
                target.GetChestPos(),
                target.GetFeetPos(),
                () =>
                {
                    
                }
            );
    }
}
public class PursueTarget : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        var caster = context.Caster;
        var target = context.SelectedTargetUnit;
        if (caster == null || target == null) return BTState.Failure;

        var coroutineKey = $"npc_pursue_{caster.unitID}";
        var movementCoroutine = context.gameManager.GetCoroutine(coroutineKey);

        if (movementCoroutine?.IsRunning == true)
            return BTState.Running;
        else if (movementCoroutine?.IsRunning == false)
        {
            context.gameManager.RemoveCoroutine(coroutineKey);
            return BTState.Success;
        }

        // Move as close as possible within range
        var path = context.Grid.FindPathTo(target.GetFeetPos(), caster.transform.position, caster.unitID, target.unitID);
        if (path == null) return BTState.Failure;

        context.gameManager.CreateCoroutine(coroutineKey, MoveCoroutine(caster, new Pathfinding.Path { pathNodes = path }));
        return BTState.Running;
    }

    private IEnumerator MoveCoroutine(CharacterUnit caster, Pathfinding.Path path)
    {
        yield return caster.MoveAlongPathRoutine(path);
    }
}

public class EndTurn : BTNode
{
    public override BTState Tick(BTblackboard context)
    {
        context.ResetTransientData();
        //context.gameManager.GetCurrentState().NextTurn();
        return BTState.Success;
    }
}

#endregion


// ====================== BT Blackboard =================== //

/// <summary>
/// Blackboard
/// </summary>
public class BTblackboard
{
    public GameManagerMDD gameManager;
    public CharacterUnit Caster;
    public List<CharacterUnit> PotentialTargets;
    public GridSystem Grid;

    //public Vector3 SelectedTargetPosition;
    public CharacterUnit SelectedTargetPosition;
    public CharacterUnit SelectedTargetUnit = null;
    public Spell SelectedSpell;

    public Pathfinding.Path MovePath;

    // Resourse gathering/mining
    //public GameObject SelectedResource;
    public Transform StockpilePosition;
    //public bool HasResource;
    public Dictionary<CharacterUnit, GameObject> SelectedResource = new Dictionary<CharacterUnit, GameObject>();
    public Dictionary<CharacterUnit, GameObject> AcquiredResource = new Dictionary<CharacterUnit, GameObject>();

    public void ResetTransientData()
    {
        SelectedTargetUnit = null;
        SelectedTargetPosition = null;// Vector3.zero;
        MovePath = null;
    }
}
