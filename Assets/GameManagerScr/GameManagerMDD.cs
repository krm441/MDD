using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public enum GameStateEnum
{
    None,
    Loading,
    Paused,
    Exploration,
    Combat,
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
    public GameStateEnum currentStateEnum = GameStateEnum.Exploration;
    public GameStateEnum GetCurrentState() => currentStateEnum;
    private Dictionary<GameStateEnum, IGameState> states;
    private IGameState currentState;

    // Start is called before the first frame update
    void Start()
    {
        states = new Dictionary<GameStateEnum, IGameState>
        {
            { GameStateEnum.Exploration, new ExplorationState(this, gridSystem) },
            { GameStateEnum.Combat, new CombatState(this) },
            
        };

        ChangeState(currentStateEnum); // start in exploration in debug
    }

    // Update is called once per frame
    void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(GameStateEnum newState)
    {
        if (currentState != null)
            currentState.Exit();

        currentStateEnum = newState;
        currentState = states.ContainsKey(newState) ? states[newState] : null;
        currentState?.Enter();
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
        Debug.Log("Entering Exploration State");

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
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0)) // this needs to be changed to event system unity
        {
            if(PartyManagement.PartyManager.IsEmpty()) return; // party not assembled yet
            
            var path = grid.FindPathToClick(PartyManagement.PartyManager.CurrentSelected.transform);
            if (path != null)
            {
                PartyManagement.PartyManager.CurrentSelected.MoveAlongPath(path);
                SpawnClickMarker(grid.LastClickPosition);
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
        Debug.Log("Exiting Exploration State");
    }
}

public class CombatState : GameStateBase
{
    public CombatState(GameManagerMDD manager) : base(manager) { }

    public override void Enter()
    {
        Debug.Log("Entering Combat");
        // Initialize turn order, UI, etc.
    }

    public override void Update()
    {
        // Combat turn logic
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            gameManager.ChangeState(GameStateEnum.Exploration);
        }
    }

    public override void Exit()
    {
        Debug.Log("Exiting Combat");
    }
}

