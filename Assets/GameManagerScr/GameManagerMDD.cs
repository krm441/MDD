using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
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

// singleton - persistent
public class GameManagerMDD : MonoBehaviour
{

    public static PlayerData playerData = new PlayerData();
   
    // references for the states:
    public Pathfinding.GridSystem gridSystem; // pathfinder
        
    // controll from outside the scene - for simplicity
    public static GameStateEnum currentStateEnum = GameStateEnum.Exploration;
    public static GameStateEnum GetCurrentStateType() => currentStateEnum;
    private static Dictionary<GameStateEnum, IGameState> states;
    private static IGameState currentState;

    public static IGameState GetCurrentState() => currentState;

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







