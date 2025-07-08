using System.Collections;
using System.Collections.Generic;
using PartyManagement;
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

        //OnMove();
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
        AimingVisualizer.ClearState();
        Console.Log("Exited Casting Substate");
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

        currentUnit = PartyManagement.PartyManager.CurrentSelected;
    }

    public override void Update()
    {
        // Check for click, apply spell, update visuals
        //OnCasting();

        if (Input.GetMouseButtonDown(1)) { AimingVisualizer.Hide(); Debug.Log("Cast cancelled"); GameManagerMDD.GetCurrentState().SetMovementSubState(); return; }

        CombatManager.CastCurrentSpell();
    }

    public override void Exit()
    {
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }

    // Private:
    private CharacterUnit currentUnit;
    private int internalState = 0;
    private List<Vector3> remaining; // remaining path visualisation
    private void OnCasting()
    {
        if (internalState == 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var path = gameManager.gridSystem.FindPathTo(hit.point, currentUnit.transform.position);
                if (path != null)
                {
                    float maxDist = currentUnit.stats.ActionPoints * currentUnit.stats.Speed;
                    if (maxDist > 0)
                        AimingVisualizer.DrawPathPreview(currentUnit.transform.position, hit.point, path, maxDist);
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

                currentUnit.MoveAlongPath(path);
                internalState = 1;

                // populate the visible path
                remaining = new List<Vector3>(AimingVisualizer.reachablePath);

                // Subtract AP based on movement length
                float walked = 0f;
                for (int i = 1; i < path.Count; i++)
                    walked += Vector3.Distance(path[i - 1], path[i]);

                int apCost = Mathf.CeilToInt(walked / currentUnit.stats.Speed);
                currentUnit.stats.ActionPoints -= apCost;
                Console.Log($"{currentUnit.unitName} moved {walked:F1} units for {apCost} AP");
            }
        }
        else if (internalState == 1)
        {
            // NOTE: needs more organic tolerence like prevTolerance > currentTolerance

            float threshold = 0.7f; // tolerance for 'reached'

            // If close enough to next point, remove it from the path
            if (remaining.Count > 1)
            {
                float dist = Vector3.Distance(currentUnit.transform.position, remaining[0]);
                if (dist < threshold)
                    remaining.RemoveAt(0);
            }

            //if(remaining.Count > 0) AimingVisualizer.DrawPath(remaining);

            if (Vector3.Distance(currentUnit.transform.position, remaining[remaining.Count - 1]) > threshold)
            {
                AimingVisualizer.DrawPath(remaining);
            }
            else internalState = 2;
        }
        else if (internalState == 2)
        {
            AimingVisualizer.ClearState();
            internalState = 0;
        }
    }
}
