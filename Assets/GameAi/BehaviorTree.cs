using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEngine;
using System.Linq;
using static UnityEngine.GraphicsBuffer;
using System.IO;
using static UnityEngine.UI.CanvasScaler;
using System.Drawing;
using UnityEngine.AI;
using UnityEngine.SocialPlatforms;
using EventSystemMDD;

namespace AiMdd
{
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
            /*var gamemanager = context.gameManager;
            if (gamemanager.IsCombat())
            {
                // check parent party is combat state
                var parent = context.Caster.parentParty;
                if (parent.isInCombat)
                    return BTState.Failure;
            }*/

            if (context.isCombat)
                return BTState.Failure;

            return BTState.Success;
        }
    }

    public class CheckEnemyInRange : BTNode
    {
        private int partyMask = LayerMask.GetMask("PartyLayer");
        private int obstacleMask = LayerMask.GetMask("Obstacles");
        private static readonly Collider[] partyHits = new Collider[PartyManagement.PartyManager.MaxPartyUnits];

        public override BTState Tick(BTblackboard context)
        {
            var self = context.Caster.transform;
            var radius = context.Caster.LignOfSight;
            var gamemanager = context.gameManager;

            // 1) check sphere
            int count = Physics.OverlapSphereNonAlloc(
                self.position,
                radius,
                partyHits,
                partyMask,
                QueryTriggerInteraction.Ignore
            );

            if (count == 0)
                return BTState.Failure; // early return

            // 2) Check LOS
            Vector3 eye = context.Caster.GetChestPos();
            for (int i = 0; i < count; i++)
            {
                var col = partyHits[i];
                if (!col) continue;

                var targetCU = col.GetComponentInParent<CharacterUnit>();
                if (targetCU == null)
                    continue;

                Vector3 targetPoint = targetCU.GetChestPos();
                Vector3 dir = targetPoint - eye;
                float dist = dir.magnitude;

                bool blocked = Physics.Raycast(
                    eye,
                    dir / dist,
                    dist,
                    obstacleMask,
                    QueryTriggerInteraction.Ignore
                );

                if (blocked)
                    continue;

                // enter combat
                //gamemanager.EnterCombat();

                context.isCombat = true;
                EventSystemMDD.EventSystemMDD.Raise(new EnemySpotterEvent { spotter = context.Caster });

                // delegate Event (will be read by combat manager)
                //throw 1;

                return BTState.Failure;
            }

            //bool inRange = Physics.CheckSphere(self.position, radius, partyMask, QueryTriggerInteraction.Ignore);
            //if(inRange)
            //{
            //    // 2) chek LOS
            //
            //
            //
            //    gamemanager.EnterCombat();
            //    return BTState.Failure;
            //}

            return BTState.Success;
        }
    }

    public class CheckCombatStateTrue : BTNode
    {
        private float timerStart = -1f;

        //public override BTState Tick(BTblackboard context)
        //{
        //    return context.gameManager.IsCombat() ? BTState.Success : BTState.Failure;
        //}

        public override BTState Tick(BTblackboard context)
        {
            /*if (timerStart < 0f)
            { 
                timerStart = Time.time;
                return BTState.Running;
            }

            if (Time.time - timerStart < 1f)
                return BTState.Running;

            // Reset timer for next execution
            timerStart = -1f;*/

            var gamemanager = context.gameManager;
            return gamemanager.IsCombat() ? BTState.Success : BTState.Failure;
        }
        /*
        public  BTState Tickgg(BTblackboard context)
        {

            var gamemanager = context.gameManager;
            if (gamemanager.IsCombat())
            {
                return BTState.Success;
            }

            return BTState.Failure;
        }*/
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

            var coroutineKey = $"npc_move_to_resource_{context.Caster.GetInstanceID()}";

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

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(unit.GetFeetPos(), targetPos, NavMesh.AllAreas, path))//     context.Grid.FindPathTo(targetPos, unit.transform.position, unit.unitID, -1);
                                                                                             //if (path == null) 
                return BTState.Failure;

            //unit.MoveAlongPath(path);
            context.gameManager.CreateCoroutine(coroutineKey, MoveCoroutine(unit, path));
            return BTState.Running;
        }

        private IEnumerator MoveCoroutine(CharacterUnit unit, List<Pathfinding.Node> path)
        {
            yield return unit.WaitForMovement(); // wait until movement is over

            //yield return null;
        }

        private IEnumerator MoveCoroutine(CharacterUnit unit, NavMeshPath path)
        {
            var agent = unit.agent;
            unit.agent.SetPath(path);
            //yield return unit.WaitForMovement(); // wait until movement is over

            if (!unit.agent.isOnNavMesh) yield return null;

            while (agent.pathPending)
                yield return null;

            while (agent.remainingDistance > agent.stoppingDistance || agent.pathPending)
                yield return null;

            while (agent.velocity.sqrMagnitude > 0.01f)
                yield return null;

            //yield return null;
        }
    }

    public class HarvestResource : BTNode
    {
        public BTState Tick5(BTblackboard context)
        {
            var unit = context.Caster;
            if (!context.SelectedResource.TryGetValue(unit, out var resource) || resource == null)
                return BTState.Failure;

            var coroutineKey = $"npc_mining_{unit.GetInstanceID()}";
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

            if (!context.SelectedResource.TryGetValue(unit, out var resource) || resource == null)
                return BTState.Failure;

            var agent = unit.agent;
            var dist = Vector3.Distance(unit.GetFeetPos(), resource.transform.position);
            if (agent.pathPending || dist > agent.stoppingDistance + 0.1f)
                return BTState.Running;

            var key = $"npc_mining_{unit.GetInstanceID()}";
            var co = context.gameManager.GetCoroutine(key);

            if (co?.IsRunning == true) return BTState.Running;
            if (co?.IsRunning == false) { context.gameManager.RemoveCoroutine(key); return BTState.Success; }

            context.gameManager.CreateCoroutine(key, MineAnimation(unit, resource, context));
            return BTState.Running;

            //var resource = context.SelectedResource[unit];
            //
            //if (resource == null) return BTState.Failure;
            //
            //var coroutineKey = $"npc_mining_{context.Caster.unitID}";
            //
            //var castingSpellCoroutine = context.gameManager.GetCoroutine(coroutineKey);
            //
            //if (castingSpellCoroutine?.IsRunning == true)
            //{
            //    return BTState.Running;
            //}
            //else if (castingSpellCoroutine?.IsRunning == false)
            //{
            //    context.gameManager.RemoveCoroutine(coroutineKey);
            //    return BTState.Success;
            //}
            //
            //// play animation or effect
            //context.gameManager.CreateCoroutine(coroutineKey, MineAnimation(unit, resource, context));
            //
            ////context.HasResource = true;
            //return BTState.Running;
        }

        private IEnumerator MineAnimation(CharacterUnit unit, GameObject resource, BTblackboard context)
        {
            Console.Log("Mining...");
            yield return new WaitForSeconds((float)Random.Range(2, 5)); // Simulate mining

            context.AcquiredResource[unit] = resource;



            //GameObject.Destroy(resource); // or disable
        }
    }

    public class MoveToStockpile : BTNode
    {
        public override BTState Tick(BTblackboard context)
        {
            var coroutineKey = $"npc_return_{context.Caster.GetInstanceID()}";

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

            //var path = context.Grid.FindPathTo(targetPos, caster.transform.position, caster.unitID, -1);
            //if (path == null) return BTState.Failure;

            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(caster.GetFeetPos(), targetPos, NavMesh.AllAreas, path))//     context.Grid.FindPathTo(targetPos, unit.transform.position, unit.unitID, -1);
                                                                                               //if (path == null) 
                return BTState.Failure;


            //context.Caster.MoveAlongPath(path); // starts movement
            context.gameManager.CreateCoroutine(coroutineKey, MoveBackToBase(context.Caster, context, path));

            return BTState.Running;
        }

        private IEnumerator MoveBackToBase(CharacterUnit unit, BTblackboard context, NavMeshPath path)
        {
            var agent = unit.agent;
            unit.agent.SetPath(path);
            //yield return unit.WaitForMovement(); // wait until movement is over
            if (!unit.agent.isOnNavMesh) yield return null;

            while (agent.pathPending)
                yield return null;

            while (agent.remainingDistance > agent.stoppingDistance || agent.pathPending)
                yield return null;

            while (agent.velocity.sqrMagnitude > 0.01f)
                yield return null;

            context.AcquiredResource[unit] = null;
            if (context.SelectedResource.TryGetValue(unit, out var res) && res != null)
            {
                ResourceOccupancyManager.Release(res);
                context.SelectedResource[unit] = null;
            }
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

    public class CheckIsMyTurn : BTNode
    {
        public override BTState Tick(BTblackboard context)
        {
            var caster = context.Caster;

            if (caster.isMyTurn)
                return BTState.Success;

            return BTState.Failure;
        }
    }

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

            bool canCast = false;
            if (Physics.Raycast(from, dir, dist, obstaclesLayer))
            {
                // LoS blocked!
                return BTState.Failure;
            }
            // -----------------------------------------------
            float distance = Vector3.Distance(caster.GetFeetPos(), target.GetFeetPos());
            if (dist < spell.range)
            {
                canCast = true;
                var coroutineKey = $"npc_pursue_{caster.GetInstanceID()}";
                context.gameManager.GetCoroutine(coroutineKey)?.Stop();
                caster.agent.isStopped = true;
                return BTState.Success;
            }
            else
            {
                var maxDistance = (caster.attributeSet.stats.ActionPoints - spell.apCost) * caster.attributeSet.stats.Speed;
                maxDistance = spell.range + 1;
                if (context.MovePath == null) context.MovePath = new NavMeshPath();
                if (!NavMesh.CalculatePath(
                    MathMDD.ProjectToNavMesh(caster.GetFeetPos()),
                    MathMDD.ProjectToNavMesh(target.GetFeetPos()),
                    NavMesh.AllAreas, context.MovePath))
                    return BTState.Failure;
                //context.MovePath = 
                //    SpellVisualizer.GetPathAtDistance(
                //    caster.GetChestPos(),
                //    target.GetChestPos(), 
                //    spell.range, 
                //    maxDistance);
                //if (path != null)
                //{
                //    AimingVisualizer.DrawPathPreview(path, maxDistance, false);
                //    inRange = true;
                //}
            }


            //context.MovePath = SpellVisualizer.GetPathAtDistance(
            //    caster.GetFeetPos(),
            //    target.GetFeetPos(),
            //    spell.range);


            //SpellRangeBackend.calculateRangeSpell(
            //spell.range,
            //spell.apCost,
            //stats.ActionPoints,
            //stats.Speed,
            //caster.GetFeetPos(),
            //target.GetFeetPos(),
            //caster, 
            //target,
            //out bool canCast
            //);

            if (canCast)
            {
                var coroutineKey = $"npc_pursue_{caster.GetInstanceID()}";
                context.gameManager.GetCoroutine(coroutineKey)?.Stop();
                caster.agent.isStopped = true;
                return BTState.Success;
            }

            return canCast ? BTState.Success : BTState.Failure;

            return BTState.Failure;
        }
    }

    public class CastSpell : BTNode
    {
        public override BTState Tick(BTblackboard context)
        {
            var caster = context.Caster;
            var coroutineKey = $"ai_casting_spell_{context.Caster.GetInstanceID()}";

            var castingSpellCoroutine = context.gameManager.GetCoroutine(coroutineKey);

            if (castingSpellCoroutine?.IsRunning == true)
            {
                return BTState.Running;
            }
            else if (castingSpellCoroutine?.IsRunning == false)
            {
                context.gameManager.RemoveCoroutine(coroutineKey);
                return BTState.Success; // exit - spell animation finished
            }

            //var path = context.MovePath;

            //context.gameManager.CreateCoroutine(coroutineKey, CastSpellAI(caster, path, context.SelectedTargetUnit, context.gameManager, context));

            context.gameManager.CreateCoroutine(coroutineKey, CastingSubstate.CastSelectedSpell
                    (caster, context.gameManager, null, context.SelectedTargetUnit.GetFeetPos(), null));

            return BTState.Running;
        }

        /*public IEnumerator CastSpellAI(CharacterUnit caster, Pathfinding.Path path, CharacterUnit target, GameManagerMDD gameManager, BTblackboard context)
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
        }*/
    }
    public class PursueTarget : BTNode
    {
        public override BTState Tick(BTblackboard context)
        {
            var caster = context.Caster;
            var target = context.SelectedTargetUnit;
            if (caster == null || target == null) return BTState.Failure;

            var coroutineKey = $"npc_pursue_{caster.GetInstanceID()}";
            var movementCoroutine = context.gameManager.GetCoroutine(coroutineKey);

            if (movementCoroutine?.IsRunning == true)
                return BTState.Running;
            //else if (movementCoroutine?.IsRunning == false)
            //{
            //    context.gameManager.RemoveCoroutine(coroutineKey);
            //    return BTState.Success;
            //}

            // Move as close as possible within range
            //var path = new NavMeshPath();//     context.MovePath;//context.Grid.FindPathTo(target.GetFeetPos(), caster.transform.position, caster.unitID, target.unitID);
            //if (path == null) return BTState.Failure;

            //context.MovePath = path;

            //if (!NavMesh.CalculatePath(caster.GetFeetPos(), target.GetFeetPos(), NavMesh.AllAreas, context.MovePath))

            if (context.MovePath == null) context.MovePath = new NavMeshPath();

            NavMesh.CalculatePath(
                   MathMDD.ProjectToNavMesh(caster.GetFeetPos()),
                   MathMDD.ProjectToNavMesh(target.GetFeetPos()),
                   NavMesh.AllAreas, context.MovePath);


            if (context.MovePath == null) return BTState.Failure;

            context.gameManager.CreateCoroutine(coroutineKey,
                TurnBasedMovement.FollowPath(caster, new List<Vector3>(context.MovePath.corners), 3f));
            // MoveCoroutine(caster, new Pathfinding.Path { pathNodes = path }));
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
            context.gameManager.NextTurn(); // using the facade : NextTurn internally calles the turn based next turn
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
        // rnd
        //public Random rnd = new Random(); // for wating time

        public GameManagerMDD gameManager;
        public CharacterUnit Caster;
        public List<CharacterUnit> PotentialTargets;
        public GridSystem Grid;

        //public Vector3 SelectedTargetPosition;
        public CharacterUnit SelectedTargetPosition;
        public CharacterUnit SelectedTargetUnit = null;
        public Spell SelectedSpell;

        //public Pathfinding.Path MovePath;
        public NavMeshPath MovePath = new NavMeshPath();

        // Resourse gathering/mining
        //public GameObject SelectedResource;
        public Transform StockpilePosition;
        //public bool HasResource;
        public Dictionary<CharacterUnit, GameObject> SelectedResource = new Dictionary<CharacterUnit, GameObject>();
        public Dictionary<CharacterUnit, GameObject> AcquiredResource = new Dictionary<CharacterUnit, GameObject>();

        public bool isCombat = false;
        public void ResetTransientData()
        {
            if(SelectedTargetUnit.IsDead) isCombat = false;
            SelectedTargetUnit = null;
            SelectedTargetPosition = null;// Vector3.zero;
            MovePath = null;
        }
    }
}