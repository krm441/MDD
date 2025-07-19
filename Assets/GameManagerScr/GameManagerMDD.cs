using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;
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
    TurnBased,      // movement in turn based mode
    CombatCasting,  // cast spells in combat (AP deduced, special logic)

    // AI states
    AI_Turn,        // invoked in turn based mode, platform for AI turn based BTs
}

public enum SpellCastingAnimationStates
{
    None,
    Animation,
    Finished
}

// singleton - persistent
public class GameManagerMDD : MonoBehaviour
{
    [SerializeField] public CombatQueue combatQueue;
    [SerializeField] public SpellMap spellMap;
    [SerializeField] public PartyManagement.PartyManager partyManager;

    public PlayerData playerData = new PlayerData();
    public CombatManager CombatManager = new CombatManager();
   
    // references for the states:
    public Pathfinding.GridSystem gridSystem; // pathfinder
        
    // controll from outside the scene - for simplicity
    public GameStateEnum currentStateEnum = GameStateEnum.Exploration;
    public GameStateEnum GetCurrentStateType() => currentStateEnum;
    private Dictionary<GameStateEnum, IGameState> states;
    private IGameState currentState;

    public IGameState GetCurrentState() => currentState;

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
        MouseTracker.Update();
    }

    public void ChangeState(GameStateEnum newState)
    {
        currentState?.Exit();

        currentStateEnum = newState;
        currentState = states.ContainsKey(newState) ? states[newState] : null;
        currentState?.Enter();
    }
    
    // public methods for button logic
    public void EnterCombat()
    {
        ChangeState(GameStateEnum.TurnBasedMode);
    }

    public void ExitCombat()
    {
        if (currentStateEnum == GameStateEnum.Exploration) return;

        ChangeState(GameStateEnum.Exploration);

        // set the main character as selected
        partyManager.SetMainAsSelected();

        // clear turn based queue
        PartyPortraitManagerUI.ClearHorisontal();
    }

    public bool AreAnyCombatCoroutinesRunning()
    {
        return coroutineHandlers.Any(pair =>
            pair.Value.IsRunning
        );
    }

    // Coroutine handlers
    private Dictionary<string, CoroutineHandle> coroutineHandlers = new Dictionary<string, CoroutineHandle>();

    public void CreateCoroutine(string name, IEnumerator coroutine)
    {
        coroutineHandlers[name] = new CoroutineHandle(this, coroutine);
    }

    public CoroutineHandle GetCoroutine(string name) 
    { 
        if (coroutineHandlers.ContainsKey(name))
            return coroutineHandlers[name];
        return null;
    }

    public void StopAllCoroutinesMDD() // MDD since it is ambiguous with Monobehaviour
    {
        foreach(CoroutineHandle handle in coroutineHandlers.Values)
        {
            handle.Stop();
        }
    }
}







