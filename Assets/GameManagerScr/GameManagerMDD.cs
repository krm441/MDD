using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq; // for sorting
using static UnityEditorInternal.ReorderableList;
using UnityEditor.PackageManager;
using static UnityEngine.UI.CanvasScaler;

public enum GameStateEnum
{
    None,
    Loading,
    Paused,
    Exploration,
    TurnBasedMode,
}

public enum InteractionSubstate
{
    Default,        // exploration
    Interaction,    // with objects
    Casting,        // spellcasting - including non combat state
}

// singleton - persistent
public class GameManagerMDD : MonoBehaviour
{

    public static PlayerData playerData = new PlayerData();
   
    // references for the states:
    public Pathfinding.GridSystem gridSystem; // pathfinder

    /*NOTE: == DONE
    - add scene specific script or serialized field that will govern the FSM sequence
    - based on this variable chose a sequence, or a state to play.
    */
    // controll from outside the scene - for simplicity
    public static GameStateEnum currentStateEnum = GameStateEnum.Exploration;
    public static GameStateEnum GetCurrentState() => currentStateEnum;
    private static Dictionary<GameStateEnum, IGameState> states;
    private static IGameState currentState;

    public static InteractionSubstate interactionSubstate = InteractionSubstate.Default; // exploration mode, click yields pathfinder movement of selected party
    public static InteractionSubstate GetInteraction() => interactionSubstate;

    // Start is called before the first frame update
    void Start()
    {
        states = new Dictionary<GameStateEnum, IGameState>
        {
            { GameStateEnum.Exploration, new ExplorationState(this, gridSystem) },
            { GameStateEnum.TurnBasedMode, new TurnBasedState(this) },
            
        };

        ChangeState(currentStateEnum); // start in exploration in debug
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.Update();
    }

    public static void ChangeState(GameStateEnum newState)
    {
        currentState?.Exit();

        currentStateEnum = newState;
        currentState = states.ContainsKey(newState) ? states[newState] : null;
        currentState?.Enter();
    }
    
    // public methods for button logic
    public static void EnterCombat()
    {
        ChangeState(GameStateEnum.TurnBasedMode);
    }

    public static void ExitCombat()
    {
        ChangeState(GameStateEnum.Exploration);
    }
}


// game states
// interface:
public interface IGameState
{
    void Enter();
    void Update();
    void Exit();
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

        if (clickMarkerPrefab == null)
        {
            clickMarkerPrefab = Resources.Load<GameObject>("Markers/selector1");
            if (clickMarkerPrefab == null)
            {
                Debug.LogWarning("ClickMarker prefab not found in Resources/Markers!");
            }
        }
    }

    private GameObject currentClickMarker;
    private GameObject clickMarkerPrefab;
    public override void Update()
    {
        ClickLogic();
    }

    void ClickLogic()
    {
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        
        if(PartyManagement.PartyManager.IsEmpty()) return; // party not assembled yet - early return

        // HERE: managing the substate interaction
        var subState = GameManagerMDD.GetInteraction();
        switch (subState)
        {
            case InteractionSubstate.Default:
                HandleMovementClick();
                break;

            case InteractionSubstate.Casting:
                HandleSpellCastClick();
                break;
        }
    }

    private void HandleMovementClick()
    {
        if (Input.GetMouseButtonDown(0)) // this needs to be changed to event system unity
        {
            var path = grid.FindPathToClick(PartyManagement.PartyManager.CurrentSelected.transform);
            if (path != null)
            {
                PartyManagement.PartyManager.CurrentSelected.MoveAlongPath(path);
                SpawnClickMarker(grid.LastClickPosition);
            }
        }
    }

    private void HandleSpellCastClick()
    {
        if (Input.GetMouseButtonDown(1)) { GameManagerMDD.interactionSubstate = InteractionSubstate.Default; AimingVisualizer.Hide(); Debug.Log("Cast cancelled"); return; }

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
        }

    }

    private void SpawnClickMarker(Vector3 position)
    {
        if (clickMarkerPrefab == null) return;

        if (currentClickMarker != null)
            GameObject.Destroy(currentClickMarker);

        Quaternion rotation = Quaternion.Euler(90f, 0f, 0f);
        Vector3 pos = position + new Vector3(0f, 0.1f, 0f);

        Debug.Log("marker pos: " + pos);

        currentClickMarker = GameObject.Instantiate(clickMarkerPrefab, pos, rotation);
        GameObject.Destroy(currentClickMarker, 1.5f);
    }

    public override void Exit()
    {
        Console.Log("Exiting Exploration State");
    }
}

// to refractor and use in both turn based state and explore state
public class SpellCastingController
{

}

public class TurnBasedState : GameStateBase 
{
    private Queue<CharacterUnit> turnQueue = new Queue<CharacterUnit>();
    private CharacterUnit currentUnit;
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
        currentUnit = turnQueue.Dequeue();
        turnQueue.Enqueue(currentUnit);
        Console.Error("start turn",  currentUnit.stats.ActionPoints);
        currentUnit.AddActionPointsStart();    //stats.ActionPoints += currentUnit.stats.StartActionPoints;

        // select new unit in the party = selects its abilities in the left icon
        PartyManagement.PartyManager.SelectMember(currentUnit);
        SpellMap.BuildIconBar(currentUnit);
        //Debug.Log($"New Turn: {currentUnit.unitName}");
    }


    private int internalState = 0;
    private List<Vector3> remaining; // remaining path visualisation

    public override void Update()
    {
        Console.LoopLog("UPPPPDAAAATE");

        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (currentUnit == null || !currentUnit.isPlayerControlled) return;

        if (internalState == 0)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit))
            {
                var path = gameManager.gridSystem.FindPathTo(hit.point, currentUnit.transform.position);
                if (path != null)
                {
                    float maxDist = currentUnit.stats.ActionPoints * currentUnit.stats.Speed;
                    //Debug.Log($"Drawing preview line {path.Count}");
                    //Console.Log($"Drawing preview line {path.Count}");
                    //Console.LoopLog("Loop: ", maxDist, currentUnit.stats.ActionPoints, currentUnit.stats.Speed);
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
        else if(internalState == 1)
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

        // 2. Handle End Turn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Console.Log($"{currentUnit.unitName} ends their turn.");
            currentUnit.StopMovement();
            AimingVisualizer.ClearState();
            internalState = 0;
            NextTurn();
        }

        // 3. Add spellcasting and UI interaction here
    }



    public override void Exit()
    {
        // restore to default this
        currentUnit.StopMovement();
        AimingVisualizer.ClearState();
        internalState = 0;

        // restore state
        PartyManager.CurrentSelected = selectedUnitBeforeCombat;
        SpellMap.BuildIconBar(currentUnit);

        Console.Error("Exiting Combat");
    }
}

