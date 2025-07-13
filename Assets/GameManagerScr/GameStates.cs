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

    ISubstate GetSubstate();
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
        // next unit
        CharacterUnit unit = turnQueue.Dequeue();

        if (unit.isPlayerControlled)
        {
            PartyManagement.PartyManager.CurrentSelected = unit;
            turnQueue.Enqueue(PartyManagement.PartyManager.CurrentSelected);
            Console.Error("start turn", PartyManagement.PartyManager.CurrentSelected.stats.ActionPoints);
            PartyManagement.PartyManager.CurrentSelected.AddActionPointsStart();

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

    public override void Update()
    {
        GetSubstate()?.Update();

        // 2. Handle End Turn
        if(!enemyTurn)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextTurn();
            }
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
        Console.Error("Exiting Combat");
    }
}