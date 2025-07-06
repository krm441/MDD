using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.EventSystems;
using static UnityEditorInternal.ReorderableList;

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
    public GameStateEnum currentStateEnum = GameStateEnum.Exploration;
    public GameStateEnum GetCurrentState() => currentStateEnum;
    private Dictionary<GameStateEnum, IGameState> states;
    private IGameState currentState;

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
        if (Input.GetMouseButtonDown(1)) { GameManagerMDD.interactionSubstate = InteractionSubstate.Default; HideAimingCircle(); Debug.Log("Cast cancelled"); return; }

        CharacterUnit caster = PartyManager.CurrentSelected;
        Spell spell = caster.GetSelectedSpell();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // spell animation
        if (Physics.Raycast(ray, out RaycastHit hit_b, 100f))
        {
            Vector3 hoverPoint = hit_b.point;
            UpdateAimingCircle(hoverPoint, spell.radius);

            ShowAimingCircle(hoverPoint, spell.radius); // if not shown yet
        }

        if (Input.GetMouseButtonDown(0))
        {
            HideAimingCircle();

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
                DrawCircle(hit.point, spell.radius);

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

    public static void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        GameObject circleObj = new GameObject("SpellRangeCircle");
        LineRenderer lr = circleObj.AddComponent<LineRenderer>();

        lr.positionCount = segments + 1;
        lr.loop = true;
        lr.widthMultiplier = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = Color.red;

        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + center;
            lr.SetPosition(i, pos);
        }

        GameObject.Destroy(circleObj, 1.5f); // destroy after 1.5 sec
    }

    private static GameObject aimCircle;

    private static List<Renderer> currentlyHighlighted = new List<Renderer>();
    private static Color highlightEmission = Color.white * 0.2f; // mild glow

    public static void ShowAimingCircle(Vector3 center, float radius, int segments = 32)
    {
        if (aimCircle != null) return;
        
        aimCircle = new GameObject("AimCircle");
        var lr = aimCircle.AddComponent<LineRenderer>();

        lr.positionCount = segments + 1;
        lr.loop = true;
        lr.widthMultiplier = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;

        UpdateAimingCircle(center, radius, segments);
    }
    public static void UpdateAimingCircle(Vector3 center, float radius, int segments = 32)
    {
        ClearHighlight();

        if (aimCircle == null) return;

        // Highlight new objects
        Collider[] hits = Physics.OverlapSphere(center, radius, LayerMask.GetMask("PartyLayer", "Destructibles"));

        foreach (Collider col in hits)
        {
            Renderer rend = col.GetComponentInChildren<Renderer>();
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.EnableKeyword("_EMISSION");
                rend.material.SetColor("_EmissionColor", highlightEmission);
                currentlyHighlighted.Add(rend);
            }
        }

        var lr = aimCircle.GetComponent<LineRenderer>();
        for (int i = 0; i <= segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius + center;
            lr.SetPosition(i, pos);
        }
    }

    private static void ClearHighlight()
    {
        // Clear previous highlights
        foreach (Renderer rend in currentlyHighlighted)
        {
            if (rend != null && rend.material.HasProperty("_EmissionColor"))
            {
                rend.material.SetColor("_EmissionColor", Color.black);
                rend.material.DisableKeyword("_EMISSION");
            }
        }
        currentlyHighlighted.Clear();
    }

    public static void HideAimingCircle()
    {
        if (aimCircle != null)
        {
            ClearHighlight();

            GameObject.Destroy(aimCircle);
            aimCircle = null;
        }
    }


    public override void Exit()
    {
        Debug.Log("Exiting Exploration State");
    }
}

public class TurnBasedState : GameStateBase
{
    public TurnBasedState(GameManagerMDD manager) : base(manager) { }

    private CombatManager combatManager = new CombatManager();

    public override void Enter()
    {
        Debug.Log("Entering Combat");
        combatManager.EnterCombat();
    }

    public override void Update()
    {
        combatManager.Update();
    }

    public override void Exit()
    {
        Debug.Log("Exiting Combat");
    }
}

