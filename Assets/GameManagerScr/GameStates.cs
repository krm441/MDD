using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq; // for sorting
using UnityEngine.UI;
using UnityEngine.AI;
using static UnityEngine.UI.CanvasScaler;

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
    protected PartyManager partyManager;

    public GameStateBase(GameManagerMDD manager)
    {
        gameManager = manager;
        partyManager = manager.partyManager;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
    //public virtual void NextTurn() { }

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

        // ui debug
        GameObject statusTextObject = GameObject.Find("StatusText");
        if (statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "Status: Exploration";
        }


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
    //public Queue<CharacterUnit> turnQueue = new Queue<CharacterUnit>();

    private CharacterUnit selectedUnitBeforeCombat; // used on exit to set it as active again

    private SpellMap spellMap;

    public TurnBasedState(GameManagerMDD manager) : base(manager) 
    {
        spellMap = manager.spellMap;
    }

    //private CombatManager combatManager = new CombatManager();

    private void HandleCombatEnded()
    {
        gameManager.StartCoroutine(ExitCombatAfterDelay());
    }

    private IEnumerator ExitCombatAfterDelay()
    {
        // short wait for all animations to finish
        yield return new WaitForSeconds(1f);
        yield return new WaitUntil(() => !gameManager.AreAnyCombatCoroutinesRunning()); // also ends all coroutines if not ended
        gameManager.ExitCombat(); 
    }


    public override void Enter()
    {
        Debug.Log("Entering Combat");

        // ui debug
        GameObject statusTextObject = GameObject.Find("StatusText");
        if (statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "Status: Combat";
        }
       

        EnemyManager.OnAllEnemiesDefeated += HandleCombatEnded;

        // save the active unit
        selectedUnitBeforeCombat = partyManager.CurrentSelected;

        //var party = PartyManager.GetParty();
        //turnQueue = new Queue<CharacterUnit>(party.OrderByDescending(p => p.stats.Initiative));

        partyManager.StopAllMovement();
        partyManager.ResetAllActionPoints(); // zero them

        // Concat the multiparty in one array
        var combatants = partyManager.GetParty()
            .Concat(EnemyManager.GetEnemies())
            .OrderByDescending(p => p.attributeSet.stats.Initiative);

        // making all agents into obstacles, for pathfinder to carve path around them
        foreach (var combatant in combatants) 
        {
            combatant.Carve();
            //var agent = combatant.agent;
            //agent.isStopped = true;
            //agent.GetComponent<NavMeshAgent>().enabled = false;
            //agent.GetComponent<NavMeshObstacle>().enabled = true;
        } 

        gameManager.combatQueue.unitQueue = new Queue<CharacterUnit>(combatants);

        // set queue in manager = for reference
        //CombatManager.turnQueue = turnQueue;

        // portrait queue building
        PartyPortraitManagerUI.BuildTurnQueuePortraits(gameManager.combatQueue.unitQueue);

        

        NextTurn();
    }

    private bool enemyTurn = false;

    public void NextTurn()
    {
        // if last turn - return ai agent to defaults
        if(partyManager.CurrentSelected != null)
        {
            partyManager.CurrentSelected.Carve();
           //NavMeshAgent agent_ = partyManager.CurrentSelected.agent;
           //
           //agent_.GetComponent<NavMeshAgent>().enabled = false;
           //agent_.GetComponent<NavMeshObstacle>().enabled = true;
            //agent_.GetComponent<NavMeshObstacle>().carving = true;
        }
        
        
        var turnQueue = gameManager.combatQueue.unitQueue;

        // next unit
        //CharacterUnit unit = turnQueue.Dequeue();

        CharacterUnit unit = null;

        // keep skipping until finds a live unit or queue is empty
        while (gameManager.combatQueue.unitQueue.Count > 0)
        {
            var candidate = gameManager.combatQueue.unitQueue.Dequeue();
            if (!candidate.IsDead)
            {
                unit = candidate;
                break;
            }
        }

        // if no live units left
        if (unit == null)
        {
            Debug.LogWarning("No valid units left in turn queue.");
            return;
        }

        //unit.GetComponent<NavMeshAgent>().enabled = true;

        // Setup nav mesh agent parameters
        unit.Uncarve();
        NavMeshAgent agent = unit.agent;
        //agent.isStopped = false;
        //agent.GetComponent<NavMeshObstacle>().enabled = false;
        //agent.GetComponent<NavMeshAgent>().enabled = true;
        //if (agent.isOnNavMesh && agent.enabled)
        //{
        //    agent.isStopped = true;
        //}

        if (unit.isPlayerControlled)
        {
            partyManager.CurrentSelected = unit;
            turnQueue.Enqueue(partyManager.CurrentSelected);
            Console.Error("start turn", partyManager.CurrentSelected.attributeSet.stats.ActionPoints);
            partyManager.CurrentSelected.AddActionPointsStart();

            // select new unit in the party = selects its abilities in the left icon
            partyManager.SelectMember(partyManager.CurrentSelected);
            spellMap.BuildIconBar(partyManager.CurrentSelected, gameManager);
            //Console.ScrLog($"New Turn Player: {partyManager.CurrentSelected.unitName}", "\nturn Q volume:", turnQueue.Count);

            // set substate to turn movement
            SetSubstate(new TurnBasedMovement(gameManager));

            enemyTurn = false;
        }
        else
        {
            spellMap.HideIconBar();
            partyManager.CurrentSelected = unit;
            enemyTurn = true;
            SetSubstate(new AITurnSubstate(gameManager, EndTurnAI));
            turnQueue.Enqueue(unit);
            Console.Error($"New Turn AI: {unit.unitName}", "\nturn Q volume:", turnQueue.Count);
        }

        // lerp camera to unit
        gameManager.isometricCamera.LerpToCharacter(unit.transform);

        // sfx end of turn effect
        SoundPlayer.PlayClipAtPoint("EndTurnType1", unit.transform.position);
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
        // remove event
        EnemyManager.OnAllEnemiesDefeated -= HandleCombatEnded;

        // Clean turn queue
        gameManager.combatQueue.unitQueue.Clear();

        // restore ap
        gameManager.partyManager.SetStartActionPoints();

        // uncarve - and let walk
        foreach (var item in gameManager.partyManager.partyMembers)
        {
            item.Uncarve();
        }

        Console.Error("Exiting Combat");
    }
}

public class ScriptedSequencesState : GameStateBase
{
    public ScriptedSequencesState(GameManagerMDD gameManager) : base(gameManager) { }
}