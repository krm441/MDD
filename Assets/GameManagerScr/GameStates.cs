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

        // === 1) marker loading === 

        //if (clickMarkerPrefab == null)
        //{
        //    clickMarkerPrefab = Resources.Load<GameObject>("Markers/selector1");
        //    if (clickMarkerPrefab == null)
        //    {
        //        Debug.LogWarning("ClickMarker prefab not found in Resources/Markers!");
        //    }
        //}

        SetSubstate(new MovementSubstate(gameManager));
    }

    //private GameObject currentClickMarker;
    //private GameObject clickMarkerPrefab;
    public override void Update()
    {
        GetSubstate()?.Update();

        //ClickLogic();
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

    //private void SpawnClickMarker(Vector3 position)
    //{
    //    if (clickMarkerPrefab == null) return;
    //
    //    if (currentClickMarker != null)
    //        GameObject.Destroy(currentClickMarker);
    //
    //    Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
    //    Vector3 pos = position + new Vector3(0f, 0.1f, 0f);
    //
    //    Debug.Log("marker pos: " + pos);
    //
    //    currentClickMarker = GameObject.Instantiate(clickMarkerPrefab, pos, rotation);
    //    GameObject.Destroy(currentClickMarker, 1.5f);
    //}

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

    private void NextTurn()
    {
        // add APs
        //if (currentUnit != null) // could be null, if scene start

        // next unit
        //currentUnit = turnQueue.Dequeue();
        //turnQueue.Enqueue(currentUnit);
        //Console.Error("start turn",  currentUnit.stats.ActionPoints);
        //currentUnit.AddActionPointsStart();    //stats.ActionPoints += currentUnit.stats.StartActionPoints;
        //
        //// select new unit in the party = selects its abilities in the left icon
        //PartyManagement.PartyManager.SelectMember(currentUnit);
        //SpellMap.BuildIconBar(currentUnit);
        //Debug.Log($"New Turn: {currentUnit.unitName}");
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

        // 2. Handle End Turn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Console.Log($"{currentUnit.unitName} ends their turn.");
            //currentUnit.StopMovement();
            //AimingVisualizer.ClearState();
            //internalState = 0;
            SetSubstate(new MovementSubstate(gameManager));
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
        //Console.Error("Exiting Combat");
    }
}