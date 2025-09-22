using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public enum GameStateEnum
{
    None,
    Loading,
    Paused,
    Exploration,
    TurnBasedMode,
    ScriptedSequence, // cut-scenes, dialogues etc
    GameOver,
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
    [SerializeField] public SoundPlayer soundPlayer;
    [SerializeField] public CombatQueue combatQueue;
    [SerializeField] public UIManager UIManager;
    [SerializeField] public SpellMap spellMap;
    //[SerializeField] public PartyManagement.PartyManager partyManager;
    [SerializeField] public PartyPlayer partyManager;
    [SerializeField] private GridPathGenerator gridPathGenerator;
    [SerializeField] public IsometricCameraController isometricCamera;

    public PlayerData playerData = new PlayerData();
    [SerializeField] public CombatManager CombatManager;//= new CombatManager();
    [SerializeField] public CombatManagement combatManagement;//= new CombatManager();
    [SerializeField] public CursorManager cursorManager;
    [SerializeField] public PartyPortraitManagerUI partyPortraitManagerUI;

    public AiMdd.AiManager aiManager;
   
    // references for the states:
    public Pathfinding.GridSystem gridSystem; // pathfinder
        
    // controll from outside the scene - for simplicity
    public GameStateEnum currentStateEnum = GameStateEnum.Exploration;
    public GameStateEnum GetCurrentStateType() => currentStateEnum;
    private Dictionary<GameStateEnum, IGameState> states;
    private IGameState currentState;

    public bool IsCombat() => currentStateEnum != GameStateEnum.Exploration;

    public IGameState GetCurrentState() => currentState;

    public void EnterCinematicState()
    {
        ChangeState(GameStateEnum.ScriptedSequence);
    }

    public void NextTurn() // facade of the turn based state
    {
        if (currentState is TurnBasedState tbState)
            tbState.NextTurn();
    }

    // Start is called before the first frame update
    void Start()
    {
        gridPathGenerator?.Initialise();
        states = new Dictionary<GameStateEnum, IGameState>
        {
            { GameStateEnum.Exploration, new ExplorationState(this, gridSystem) },
            { GameStateEnum.TurnBasedMode, new TurnBasedState(this) },            
            { GameStateEnum.ScriptedSequence, new ScriptedSequencesState(this) },            
            { GameStateEnum.GameOver, new GameOverState(this) },            
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
        partyPortraitManagerUI.ClearHorisontal();
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
    public void RemoveCoroutine(string name)
    {
        if (coroutineHandlers.TryGetValue(name, out var handle))
        {
            handle.Stop(); // Just to be safe
            coroutineHandlers.Remove(name);
        }
    }

    public CoroutineHandle GetCoroutine(string name) 
    { 
        if (coroutineHandlers.ContainsKey(name))
            return coroutineHandlers[name];
        return null;
    }

    public void StopAllCoroutinesMDD() // MDD since it is ambiguous with Monobehaviour
    {
        foreach (CoroutineHandle handle in coroutineHandlers.Values)
        {
            handle.Stop();
        }
    }

    public void GameOverLoss()
    {
        Console.Log("Player lose");
        ChangeState(GameStateEnum.GameOver);
    }
}







