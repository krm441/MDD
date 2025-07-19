using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Substate also needs a standalone FSM
/// Interface class for substate: casting, movement etc...
/// </summary>
public interface ISubstate
{
    void Enter();
    void Update();
    void Exit();

    InteractionSubstate Type { get; }

    SpellCastingAnimationStates SpellcastingAnimationState { get; set; }
}

public abstract class SubStateBase : ISubstate
{
    protected GameManagerMDD gameManager;
    protected PartyManager partyManager;

    public SubStateBase(GameManagerMDD manager)
    {
        gameManager = manager;
        partyManager = gameManager.partyManager;
    }

    protected InteractionSubstate substate;

    public virtual InteractionSubstate Type => substate;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    public SpellCastingAnimationStates SpellcastingAnimationState { get; set; }
}

public class MovementSubstate : SubStateBase
{
    public MovementSubstate(GameManagerMDD manager) : base(manager) 
    {
        substate = InteractionSubstate.Default;
    }
    public override void Enter()
    {
        Console.Log("Entered Movement Substate");
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        Text statusText = statusTextObject.GetComponent<Text>();
        statusText.text = "SubStatus: Movement";
    }

    public override void Update() 
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (partyManager.CurrentSelected == null || !partyManager.CurrentSelected.isPlayerControlled) return;

        var followPathCoroutine = gameManager.GetCoroutine("party_path");

        if (Input.GetMouseButtonDown(1)) // Cancel with right-click
        {
            if (followPathCoroutine?.IsRunning == true)
            {
                followPathCoroutine.Stop();

                foreach (var unit in partyManager.GetParty())
                {
                    unit.StopMovement();
                }

                AimingVisualizer.Hide();
            }
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var path = gameManager.gridSystem.FindPathToClick(partyManager.CurrentSelected.transform);
            if (path != null)
            {
                
                AimingVisualizer.SpawnClickMarker(gameManager.gridSystem.LastClickPosition);

                // Get leader's final target point 
                Vector3 leaderTarget = path[path.Count - 1].worldPos;
                gameManager.CreateCoroutine("party_path", FollowPartyTogether(leaderTarget, path));
            }
        }
    }

    private List<Vector3> GetFormationTargets(Vector3 leaderTarget, Vector3 leaderForward)
    {
        List<Vector3> formationTargets = new List<Vector3>();
        Vector3 right = Vector3.Cross(Vector3.up, leaderForward).normalized;

        float spacing = 2f; // distance between characters

        // Rhombus formation:

        //     Leader
        // F1    F3    F2
        //       F4


        // F1: back-left
        formationTargets.Add(leaderTarget - leaderForward * spacing + right * -spacing);
        // F2: back-right
        formationTargets.Add(leaderTarget - leaderForward * spacing + right * spacing);
        // F3: directly behind
        formationTargets.Add(leaderTarget - leaderForward * spacing * 1.5f);
        // F4: double back
        formationTargets.Add(leaderTarget - leaderForward * spacing * 2f);

        return formationTargets;
    }

    private IEnumerator FollowPartyTogether(Vector3 leaderTarget, List<Pathfinding.Node> path_leader)
    {
        // set leader to target
        partyManager.CurrentSelected.MoveAlongPath(path_leader);

        var party = partyManager.GetParty();
        var leader = partyManager.CurrentSelected;
        Vector3 leaderForward = (leaderTarget - leader.transform.position).normalized;

        // Followers should look at a forward point, 13 units ahead of the leader's destination
        Vector3 sharedLookTarget = leaderTarget + leaderForward * 13f;

        // Get follower positions behind leader
        List<Vector3> formationTargets = GetFormationTargets(leaderTarget, leaderForward);

        // This variable is used to know whom to apply lookAt to target direction
        List<PartyManagement.CharacterUnit> activeFollowers = new List<PartyManagement.CharacterUnit>();


        // 
        int followerIndex = 0;

        foreach (var follower in party)
        {
            if (follower == leader ) continue;

            if (followerIndex >= formationTargets.Count)
                break;

            Vector3 followerGoal = formationTargets[followerIndex];
            followerIndex++;

            var path = gameManager.gridSystem.FindPathTo(followerGoal, follower.transform.position);
            if (path != null)
            {
                follower.MoveAlongPath(path);
                activeFollowers.Add(follower);
            }
        }

        // Wait until all followers have stopped moving
        yield return new WaitUntil(() => AllFollowersStopped(activeFollowers));

        // Apply look at direction of leader
        foreach (var follower in party)
        {
            follower.LookAtTarget(sharedLookTarget);
        }
    }

    private bool AllFollowersStopped(List<PartyManagement.CharacterUnit> followers)
    {
        foreach (var unit in followers)
        {
            if (unit.movementController != null && unit.movementController.IsMoving)
                return false;
        }
        return true;
    }

    public override void Exit()
    {
        Console.Log($"{partyManager.CurrentSelected.unitName} ends their turn.");
        partyManager.CurrentSelected.StopMovement();
        //AimingVisualizer.ClearState();
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }    
}

public class TurnBasedMovement : SubStateBase
{
    public TurnBasedMovement(GameManagerMDD manager) : base(manager)
    {
        substate = InteractionSubstate.TurnBased;
    }

    public override void Enter()
    {
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        Text statusText = statusTextObject.GetComponent<Text>();
        statusText.text = "SubStatus: TurnBasedM";
    }

    public override void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (partyManager.CurrentSelected == null || !partyManager.CurrentSelected.isPlayerControlled) return;

        var followPathCoroutine = gameManager.GetCoroutine("following_path");

        if (Input.GetMouseButtonDown(1)) // Cancel with right-click
        {
            if (followPathCoroutine?.IsRunning == true)
            {
                followPathCoroutine.Stop();
                AimingVisualizer.Hide();
            }
            return;
        }

        // Prevent path preview while moving
        if (followPathCoroutine?.IsRunning == true)
        {
            AimingVisualizer.Hide();
            return;
        }

        // Allow path preview
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Pathfinding.Path path = null;
        bool inRange = false;

        if (Physics.Raycast(ray, out var hit))
        {
            path = SpellVisualizer.VisualizePath(
                partyManager.CurrentSelected.transform.position,
                hit.point,
                partyManager.CurrentSelected.attributeSet.stats.ActionPoints,
                partyManager.CurrentSelected.attributeSet.stats.Speed,
                out inRange
            );
        }

        // Left click to move
        if (Input.GetMouseButtonDown(0) && path != null)
        {
            AimingVisualizer.Hide();

            // Start following coroutine and register it
            gameManager.CreateCoroutine("following_path", FollowPathCoroutine(path, 3f));
        }
    }

    private IEnumerator FollowPathCoroutine(Pathfinding.Path path, float speed)
    {
        var unit = partyManager.CurrentSelected;
        var pathNodes = path.pathNodes;
        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerStep = 1f / unit.attributeSet.stats.Speed;

        foreach (var node in pathNodes)
        {
            Vector3 targetPos = node.worldPos;

            unit.LookAtTarget(targetPos); // Facing the target
            remainingAP -= costPerStep;
            unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);

            while (Vector3.Distance(unit.transform.position, targetPos) > 0.05f)
            {
                if (Input.GetMouseButtonDown(1)) // Cancel move
                {                    
                    Console.Log("Movement cancelled.", unit.attributeSet.stats.ActionPoints);
                    yield break;
                }

                unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }

           
            if (remainingAP < 0)
            {
                Console.Log("Ran out of AP.");
                break;
            }
        }

        // Finalize state
        unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);
        Console.Log($"{unit.unitName} finished movement. Remaining AP: {unit.attributeSet.stats.ActionPoints}");
        AimingVisualizer.Hide();
    }
     
    public override void Exit() 
    { 
        internalState = 0; remaining?.Clear(); 
        AimingVisualizer.Hide();
        //AimingVisualizer.ClearState();
        gameManager.StopAllCoroutinesMDD();
        Console.Log("Exiting turn based mode");
    }

    private int internalState = 0;
    private List<Vector3> remaining;    
    
}

public class CastingSubstate : SubStateBase
{
    public CastingSubstate(GameManagerMDD manager) : base(manager)
    {
        substate = InteractionSubstate.Casting;
    }

    public override void Enter()
    {
        Console.Log("Entered Casting Substate");
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        Text statusText = statusTextObject.GetComponent<Text>();
        statusText.text = "SubStatus: Casting";
    }

    public override void Update()
    {
        if (Input.GetMouseButtonDown(1)) { AimingVisualizer.Hide(); Debug.Log("Cast cancelled"); gameManager.GetCurrentState().SetMovementSubState(); return; }

        var castingSpellCoroutine = gameManager.GetCoroutine("casting_spell");

        // Prevent path preview while moving
        if (castingSpellCoroutine?.IsRunning == true)
        {
            AimingVisualizer.Hide();
            return;
        }

        bool inRange = false;
        Pathfinding.Path path = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            // for optimisation, to alter the state of line renderer only if user is aiming
            bool mouseMoved = MouseTracker.MouseMovedThisFrame;
            bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

            if (mouseMoved || mouseClicked)
            {
                path = SpellVisualizer.VisualizeSpell(
                    partyManager.CurrentSelected.GetSelectedSpell(),
                    -1,
                    partyManager.CurrentSelected.attributeSet.stats.Speed,
                    partyManager.CurrentSelected.transform.position,
                    hit.point,
                    out inRange
                    );
            }
        }

        if (Input.GetMouseButtonDown(0) && inRange)
        {
            var caster = partyManager.CurrentSelected;
            var target = hit.point;
            gameManager.CreateCoroutine("casting_spell", CastSpell(caster, path, target, gameManager));
        }
    }

    public IEnumerator CastSpell(CharacterUnit caster, Pathfinding.Path path, Vector3 target, GameManagerMDD gameManager)
    {
        Console.Log("Casitng spell called");
        
        yield return caster.CastSpellWithMovement(
                caster,
                gameManager.CombatManager,
                path,
                target,
                () =>
                {
                    // only runs after move + cast are done (when spell animation is over)
                    AimingVisualizer.Hide();
                    //GameManagerMDD.GetCurrentState().GetSubstate().AnimationFinished = true;
                    gameManager.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Finished;


                    // Special case : restore char's ap to maximin
                    //caster.SetActionPoints(caster.stats.StartActionPoints);
                }
            );

        gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
    }

    public override void Exit()
    {
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }   
}

public class TurnBasedCasting : SubStateBase
{
    public TurnBasedCasting(GameManagerMDD manager) : base(manager)
    {
        substate = InteractionSubstate.CombatCasting;
    }

    public override void Enter()
    {
        Console.Log("Entered CombatCasting Substate");
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        Text statusText = statusTextObject.GetComponent<Text>();
        statusText.text = "SubStatus: TurnCasting";
    }

    //private 

    public IEnumerator CastSpell(CharacterUnit caster, Pathfinding.Path path, Vector3 target, GameManagerMDD gameManager)
    {
        Console.Log("Casitng spell called");

        yield return caster.CastSpellWithMovement(
                caster,
                gameManager.CombatManager,
                path,
                target,
                () =>
                {
                    // only runs after move + cast are done (when spell animation is over)
                    AimingVisualizer.Hide();
                    //GameManagerMDD.GetCurrentState().GetSubstate().AnimationFinished = true;
                    gameManager.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Finished;


                    // Special case : restore char's ap to maximin
                    //caster.SetActionPoints(caster.stats.StartActionPoints);
                }
            );

        gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
    }

    public override void Update()
    {
        // early return
        if (partyManager.CurrentSelected.GetSelectedSpell().apCost > partyManager.CurrentSelected.attributeSet.stats.ActionPoints)
        {
            gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }

        var castingSpellCoroutine = gameManager.GetCoroutine("casting_spell");

        // Prevent path preview while moving
        if (castingSpellCoroutine?.IsRunning == true)
        {
            AimingVisualizer.Hide();
            return;
        }

        bool inRange = false;
        Pathfinding.Path path = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            // for optimisation, to alter the state of line renderer only if user is aiming
            bool mouseMoved = MouseTracker.MouseMovedThisFrame;
            bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

            if (mouseMoved || mouseClicked)
            {
                path = SpellVisualizer.VisualizeSpell(
                    partyManager.CurrentSelected.GetSelectedSpell(),
                    -1,
                    partyManager.CurrentSelected.attributeSet.stats.Speed,
                    partyManager.CurrentSelected.transform.position,
                    hit.point,
                    out inRange
                    );
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            AimingVisualizer.Hide();
            gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }

        if (Input.GetMouseButtonDown(0) && inRange)
        {
            var caster = partyManager.CurrentSelected;
            var target = hit.point;
            gameManager.CreateCoroutine("casting_spell", CastSpell(caster, path, target, gameManager));
        }
    }

    public override void Exit()
    {
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }
}

public class AITurnSubstate : SubStateBase
{
    private Action endTurn;

    public AITurnSubstate(GameManagerMDD manager, Action onCompleteCallback) : base(manager)
    {
        substate = InteractionSubstate.AI_Turn;
        endTurn = onCompleteCallback;
    }

    public override void Enter()
    {
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        Text statusText = statusTextObject.GetComponent<Text>();
        statusText.text = "SubStatus: AITurn";
    }
    public override void Update() 
    {
        Console.Log("AI turn");

        if(!called)
            TimerUtility.WaitAndDo(gameManager, 2f, endTurn);
        called = true;
    }

    private bool called = false;

    public override void Exit() { }
}