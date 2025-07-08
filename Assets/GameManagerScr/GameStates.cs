using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq; // for sorting

// game states
// interface:
public interface IGameState
{
    void Enter();
    void Update();
    void Exit();

    // dop
    void SetCastingSubState();
    void SetMovementSubState();

    void SetSubstate(ISubstate newSubstate);
}

// base class
public abstract class GameStateBase : IGameState
{
    protected GameManagerMDD gameManager;

    public GameStateBase(GameManagerMDD manager)
    {
        gameManager = manager;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }

    // ==== Substates ====
    protected ISubstate currentSubstate;

    public ISubstate GetSubstate() => currentSubstate;
    public void SetSubstate(ISubstate newSubstate)
    {
        currentSubstate?.Exit();
        currentSubstate = newSubstate;
        currentSubstate?.Enter();
    }

    public virtual void SetCastingSubState()
    {
        if (currentSubstate.Type != InteractionSubstate.Casting)
        {
            SetSubstate(new CastingSubstate(gameManager));
        }
    }

    public virtual void SetMovementSubState()
    {
        if (currentSubstate.Type != InteractionSubstate.Default)
        {
            SetSubstate(new MovementSubstate(gameManager));
        }
    }
}

public class ExplorationState : GameStateBase
{
    private Pathfinding.GridSystem grid;

    public ExplorationState(GameManagerMDD manager, Pathfinding.GridSystem gridSystem) : base(manager)
    {
        grid = gridSystem;
    }

    public override void Enter()
    {
        Console.Log("Entering Exploration State");


        SetSubstate(new MovementSubstate(gameManager));
    }

   
    public override void Update()
    {
        GetSubstate()?.Update();
    }

    

    public void ChangeSubstateMoveCasting()
    {
        if (currentSubstate != null)
        {
            if (currentSubstate.Type == InteractionSubstate.Default || currentSubstate.Type == InteractionSubstate.Interaction)
            {
                SetSubstate(new CastingSubstate(gameManager));
            }
            else
            {
                SetSubstate(new MovementSubstate(gameManager));
            }
        }
    }

    //void ClickLogic()
    //{
    //    if (EventSystem.current.IsPointerOverGameObject())
    //        return;
    //
    //    if (PartyManagement.PartyManager.IsEmpty()) return; // party not assembled yet - early return
    //
    //    // HERE: managing the substate interaction
    //    var subState = GameManagerMDD.GetInteraction();
    //    switch (subState)
    //    {
    //        case InteractionSubstate.Default:
    //            HandleMovementClick();
    //            break;
    //
    //        case InteractionSubstate.Casting:
    //            HandleSpellCastClick();
    //            break;
    //    }
    //}

    private void HandleMovementClick()
    {
        if (Input.GetMouseButtonDown(0)) // this needs to be changed to event system unity
        {
            var path = grid.FindPathToClick(PartyManagement.PartyManager.CurrentSelected.transform);
            if (path != null)
            {
                PartyManagement.PartyManager.CurrentSelected.MoveAlongPath(path);
                //SpawnClickMarker(grid.LastClickPosition);
            }
        }
    }

    private void HandleSpellCastClick()
    {
        //if (Input.GetMouseButtonDown(1)) { GameManagerMDD.interactionSubstate = InteractionSubstate.Default; AimingVisualizer.Hide(); Debug.Log("Cast cancelled"); return; }

       // CombatManager.CastCurrentSpell();
        /*
        CharacterUnit caster = PartyManager.CurrentSelected;
        Spell spell = caster.GetSelectedSpell();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // spell animation
        if (Physics.Raycast(ray, out RaycastHit hit_b, 100f))
        {
            Vector3 hoverPoint = hit_b.point;
            AimingVisualizer.ShowAimingCircle(hoverPoint, spell.radius);
            AimingVisualizer.HighlightTargets(hoverPoint, spell.radius);
        }

        if (Input.GetMouseButtonDown(0))
        {
            AimingVisualizer.Hide();

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (spell == null)
                {
                    Debug.LogWarning("No spell selected.");
                    return;
                }

                float dist = Vector3.Distance(caster.transform.position, hit.point);
                if (dist > spell.range)
                {
                    Debug.Log("Target out of range.");
                    return;
                }

                CombatManager.ApplySpell(caster, spell, hit.point);
                AimingVisualizer.DrawImpactCircle(hit.point, spell.radius);

                // Reset casting state
                caster.DeselectSpell();
                GameManagerMDD.interactionSubstate = InteractionSubstate.Default;
            }
        }*/

    }

    public override void Exit()
    {
        Console.Log("Exiting Exploration State");
    }
}

public class TurnBasedState : GameStateBase
{
    private Queue<CharacterUnit> turnQueue = new Queue<CharacterUnit>();

    private CharacterUnit selectedUnitBeforeCombat; // used on exit to set it as active again

    public TurnBasedState(GameManagerMDD manager) : base(manager) { }

    private CombatManager combatManager = new CombatManager();

    public override void Enter()
    {
        Debug.Log("Entering Combat");
        combatManager.EnterCombat();

        // save the active unit
        selectedUnitBeforeCombat = PartyManager.CurrentSelected;

        //var party = PartyManager.GetParty();
        //turnQueue = new Queue<CharacterUnit>(party.OrderByDescending(p => p.stats.Initiative));

        PartyManager.StopAllMovement();
        PartyManager.ResetAllActionPoints(); // zero them

        // Concat the multiparty in one array
        var combatants = PartyManager.GetParty()
            .Concat(EnemyManager.GetEnemies())
            .OrderByDescending(p => p.stats.Initiative);

        turnQueue = new Queue<CharacterUnit>(combatants);

        // portrait queue building
        PartyPortraitManagerUI.BuildTurnQueuePortraits(turnQueue);

        

        NextTurn();
    }

    private bool enemyTurn = false;

    private void NextTurn()
    {
        // add APs
        //if (PartyManagement.PartyManager.CurrentSelected != null) // could be null, if scene start

        // next unit
        CharacterUnit unit = turnQueue.Dequeue();
        if (unit.isPlayerControlled)
        {
            PartyManagement.PartyManager.CurrentSelected = unit;
            turnQueue.Enqueue(PartyManagement.PartyManager.CurrentSelected);
            Console.Error("start turn", PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints);
            PartyManagement.PartyManager.CurrentSelected.AddActionPointsStart();    //stats.ActionPoints += currentUnit.stats.StartActionPoints;

            // select new unit in the party = selects its abilities in the left icon
            PartyManagement.PartyManager.SelectMember(PartyManagement.PartyManager.CurrentSelected);
            SpellMap.BuildIconBar(PartyManagement.PartyManager.CurrentSelected);
            Console.ScrLog($"New Turn Player: {PartyManagement.PartyManager.CurrentSelected.unitName}", "\nturn Q volume:", turnQueue.Count);

            // set substate to turn movement
            SetSubstate(new TurnBasedMovement(gameManager));

            enemyTurn = false;
        }
        else
        {
            enemyTurn = true;
            SetSubstate(new AITurnSubstate(gameManager, EndTurnAI));
            turnQueue.Enqueue(unit);
            Console.ScrLog($"New Turn AI: {unit.unitName}", "\nturn Q volume:", turnQueue.Count);
        }
    }

    public override void SetCastingSubState()
    {
        if (currentSubstate.Type != InteractionSubstate.Casting)
        {
            SetSubstate(new TurnBasedCasting(gameManager));
        }
    }

    public void EndTurnAI()
    {
        Console.ScrLog("turn AI end");
        NextTurn();
    }



    private void OnCast()
    {
        if (Input.GetMouseButtonDown(1))
        {
            //GameManagerMDD.interactionSubstate = InteractionSubstate.Default;
            AimingVisualizer.Hide();
            Debug.Log("Cast cancelled");
            transitionBool = true;
            return;
        }

        CombatManager.CastCurrentSpell();
    }

    private bool transitionBool = true;

    public override void Update()
    {
        //Console.LoopLog("UPPPPDAAAATE");

        GetSubstate()?.Update();

        // 2. Handle End Turn
        if(!enemyTurn)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //Console.Log($"{currentUnit.unitName} ends their turn.");
                //currentUnit.StopMovement();
                //AimingVisualizer.ClearState();
                //internalState = 0;
                //SetSubstate(new MovementSubstate(gameManager));
                NextTurn();
            }

        //if (EventSystem.current.IsPointerOverGameObject()) return;
        //if (currentUnit == null || !currentUnit.isPlayerControlled) return;
        //
        //var subState = GameManagerMDD.GetInteraction();
        //switch (subState)
        //{
        //    case InteractionSubstate.Default:
        //        OnMove();
        //        break;
        //
        //    case InteractionSubstate.Casting:
        //        Debug.Assert(false, "not ready");
        //        if (transitionBool)
        //        {
        //            currentUnit.StopMovement();
        //            AimingVisualizer.ClearState();
        //            internalState = 0;
        //
        //            transitionBool = false; // clear state
        //        }
        //        OnCast();
        //        break;
        //}        




    }



    public override void Exit()
    {
        // restore to default this
        //currentUnit.StopMovement();
        //AimingVisualizer.ClearState();
        //internalState = 0;
        //
        //// restore state
        //PartyManager.CurrentSelected = selectedUnitBeforeCombat;
        //SpellMap.BuildIconBar(currentUnit);
        //
        //Console.Error("Exiting Combat");
    }
}