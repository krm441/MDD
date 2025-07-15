using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
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

    //bool AnimationFinished { get; set; }
    SpellCastingAnimationStates SpellcastingAnimationState { get; set; }
}

public abstract class SubStateBase : ISubstate
{
    protected GameManagerMDD gameManager;

    public SubStateBase(GameManagerMDD manager)
    {
        gameManager = manager;
    }

    protected InteractionSubstate substate;

    public virtual InteractionSubstate Type => substate;

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    //public bool AnimationFinished { get; set; }

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
    }

    public override void Update() 
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (PartyManagement.PartyManager.CurrentSelected == null || !PartyManagement.PartyManager.CurrentSelected.isPlayerControlled) return;
                
        if (Input.GetMouseButtonDown(0)) // this needs to be changed to event system unity
        {
            var path = gameManager.gridSystem.FindPathToClick(PartyManagement.PartyManager.CurrentSelected.transform);
            if (path != null)
            {
                PartyManagement.PartyManager.CurrentSelected.MoveAlongPath(path);
                AimingVisualizer.SpawnClickMarker(gameManager.gridSystem.LastClickPosition);
            }
        }
    }

    public override void Exit()
    {
        Console.Log($"{PartyManagement.PartyManager.CurrentSelected.unitName} ends their turn.");
        PartyManagement.PartyManager.CurrentSelected.StopMovement();
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

    public override void Enter() { }
    public override void Update() 
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (PartyManagement.PartyManager.CurrentSelected == null || !PartyManagement.PartyManager.CurrentSelected.isPlayerControlled) return;

        //OnMove();

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Pathfinding.Path path = null;
        bool inRange = false;
        if (Physics.Raycast(ray, out var hit))
        {
            path = SpellVisualizer.VisualizePath(
            PartyManagement.PartyManager.CurrentSelected.transform.position,
            hit.point,
            PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints,
            PartyManagement.PartyManager.CurrentSelected.stats.Speed,
            out inRange
            );
        }

        Console.ScrLoopLog("APs:", PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints);

        if (Input.GetMouseButtonDown(0))
        {
            if (inRange && path != null)
            {
                PartyManagement.PartyManager.CurrentSelected.DeductActionPoints(path);
                PartyManagement.PartyManager.CurrentSelected.MoveAlongPath(path.pathNodes);
            }
        }
    }

    public override void Exit() 
    { 
        internalState = 0; remaining?.Clear(); 
        AimingVisualizer.Hide(); 
        //AimingVisualizer.ClearState(); 
    }

    private int internalState = 0;
    private List<Vector3> remaining;
    private void OnMove()
    {
        if (internalState == 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var path = gameManager.gridSystem.FindPathTo(hit.point, PartyManagement.PartyManager.CurrentSelected.transform.position);
                if (path != null)
                {
                    float maxDist = PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints * PartyManagement.PartyManager.CurrentSelected.stats.Speed;
                    if (maxDist > 0)
                        AimingVisualizer.DrawPathPreview(PartyManagement.PartyManager.CurrentSelected.transform.position, hit.point, path, maxDist);
                }
                else
                {
                    Console.Error("path null");
                }
            }


            // 1. Handle Movement
            if (Input.GetMouseButtonDown(0))
            {
                var path = new List<Vector3>(AimingVisualizer.reachablePath);   //  gameManager.gridSystem.FindPathToClick(currentUnit.transform);

                if (path == null || path.Count == 0)
                {
                    Console.Error("No valid path.");
                    return;
                }

                PartyManagement.PartyManager.CurrentSelected.MoveAlongPath(path);
                internalState = 1;

                // populate the visible path
                remaining = new List<Vector3>(AimingVisualizer.reachablePath);

                // Subtract AP based on movement length
                float walked = 0f;
                for (int i = 1; i < path.Count; i++)
                    walked += Vector3.Distance(path[i - 1], path[i]);

                int apCost = Mathf.CeilToInt(walked / PartyManagement.PartyManager.CurrentSelected.stats.Speed);
                PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints -= apCost;
                Console.Log($"{PartyManagement.PartyManager.CurrentSelected.unitName} moved {walked:F1} units for {apCost} AP");
            }
        }
        else if (internalState == 1)
        {
            // NOTE: needs more organic tolerence like prevTolerance > currentTolerance

            float threshold = 0.7f; // tolerance for 'reached'

            // If close enough to next point, remove it from the path
            if (remaining.Count > 1)
            {
                float dist = Vector3.Distance(PartyManagement.PartyManager.CurrentSelected.transform.position, remaining[0]);
                if (dist < threshold)
                    remaining.RemoveAt(0);
            }

            //if(remaining.Count > 0) AimingVisualizer.DrawPath(remaining);

            if (Vector3.Distance(PartyManagement.PartyManager.CurrentSelected.transform.position, remaining[remaining.Count - 1]) > threshold)
            {
                AimingVisualizer.DrawPath(remaining);
            }
            else internalState = 2;
        }
        else if (internalState == 2)
        {
            AimingVisualizer.Hide();
            internalState = 0;
        }
    }
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
    }

    public override void Update()
    {
        if (Input.GetMouseButtonDown(1)) { AimingVisualizer.Hide(); Debug.Log("Cast cancelled"); GameManagerMDD.GetCurrentState().SetMovementSubState(); return; }

        //CombatManager.VisualiseSpellImpactArea();

        bool inRange = false;
        Pathfinding.Path path = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var hit))
        {
            if (GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState != SpellCastingAnimationStates.Animation)
            {
                // for optimisation, to alter the state of line renderer only if user is aiming
                bool mouseMoved = MouseTracker.MouseMovedThisFrame;
                bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

                if (mouseMoved || mouseClicked)
                {
                    path = SpellVisualizer.VisualizeSpell(
                        PartyManagement.PartyManager.CurrentSelected.GetSelectedSpell(),
                        -1,
                        PartyManagement.PartyManager.CurrentSelected.stats.Speed,
                        PartyManagement.PartyManager.CurrentSelected.transform.position,
                        hit.point,
                        out inRange
                        );
                }
            }
            else
            {
                AimingVisualizer.Hide();
                path = null;
                inRange = true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (inRange)
            {
                var caster = PartyManagement.PartyManager.CurrentSelected;
                var target = hit.point;

                GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Animation;

                // This will first walk the nodes
                // Then call CombatManager.CastSpell
                // then deduct AP, then finally invoke callback
                gameManager.StartCoroutine(
                    caster.CastSpellWithMovement(
                        caster,
                        //actualPath, 
                        path,
                        target,
                        () => {
                            // only runs after move + cast are done (when spell animation is over)
                            AimingVisualizer.Hide();
                            //GameManagerMDD.GetCurrentState().GetSubstate().AnimationFinished = true;
                            GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Finished;


                            // Special case : restore char's ap to maximin
                            caster.SetActionPoints(caster.stats.StartActionPoints);
                        }
                    )
                );
            }
        }
        if (GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState == SpellCastingAnimationStates.Finished)
        {
            GameManagerMDD.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            //AnimationFinished = false;
            GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.None;
        }

        //if (Input.GetMouseButtonDown(0))
        //{
        //    CombatManager.CastSelectedSpell();
        //    GameManagerMDD.GetCurrentState().SetMovementSubState();
        //}
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
    }

    //private 

    public override void Update()
    {
        // early return
        if(PartyManagement.PartyManager.CurrentSelected.GetSelectedSpell().apCost > PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints)
        {
            GameManagerMDD.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }


        if (Input.GetMouseButtonDown(1))
        {
            AimingVisualizer.Hide();
            GameManagerMDD.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }

        bool inRange = false;
        Pathfinding.Path path = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out var hit))
        {
            if (GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState != SpellCastingAnimationStates.Animation)
            {
                // for optimisation, to alter the state of line renderer only if user is aiming
                bool mouseMoved = MouseTracker.MouseMovedThisFrame;
                bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

                if (mouseMoved || mouseClicked)
                {
                    path = SpellVisualizer.VisualizeSpell(
                        PartyManagement.PartyManager.CurrentSelected.GetSelectedSpell(),
                        PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints,
                        PartyManagement.PartyManager.CurrentSelected.stats.Speed,
                        PartyManagement.PartyManager.CurrentSelected.transform.position,
                        hit.point,
                        out inRange
                        );
                }
            }
            else
            {
                AimingVisualizer.Hide();
                path = null;
                inRange = true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (inRange)
            {
                var caster = PartyManagement.PartyManager.CurrentSelected;
                var target = hit.point;

                GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Animation;

                // This will first walk the nodes
                // Then call CombatManager.CastSpell
                // then deduct AP, then finally invoke callback
                gameManager.StartCoroutine(
                    caster.CastSpellWithMovement(
                        caster,
                        path,//new Pathfinding.Path { pathNodes = path.pathNodes }, // passing path as copy
                        target,
                        () => {
                            // only runs after move + cast are done (when spell animation is over)
                            AimingVisualizer.Hide();
                            //GameManagerMDD.GetCurrentState().GetSubstate().AnimationFinished = true;
                            GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Finished;
                        }
                    )
                );
            }            
        }
        if (GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState == SpellCastingAnimationStates.Finished)
        {
            GameManagerMDD.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            //AnimationFinished = false;
            GameManagerMDD.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.None;
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

    public override void Enter() { }
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