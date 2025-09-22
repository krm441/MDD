using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PartyManagement;

public class SpecificTutorial : MonoBehaviour
{
    [SerializeField] private CharacterUnit bob;
    [SerializeField] private PartyPlayer player;
    [SerializeField] private BridgesController bridgesController;
    [SerializeField] private Transform waypointOne;
    [SerializeField] private Transform waypointTwo;
    [SerializeField] private GameManagerMDD gameManager;
    [SerializeField] private bool playScriptedScenerio = true;
    [SerializeField] private IsometricCameraController isometricCameraController;

    private enum TutorialState
    {
        SelfMonologueLineOne,
        SelfMonologueLineTwo,
        Enter,
        BobMove,
        BridgeUp,
        ConversationStart,
        ConversationEnd,
    }
    private TutorialState currentState = TutorialState.Enter;

    private List<TutorialState> statesConsequtive = new List<TutorialState>()
    {
        TutorialState.Enter,
        TutorialState.BobMove,
        TutorialState.BridgeUp,
        TutorialState.ConversationStart,
        TutorialState.ConversationEnd,
    };

    private Dictionary<TutorialState, Action> onEntry;
    private Dictionary<TutorialState, Action> onUpdate;
    private Dictionary<TutorialState, string> conversations;

    // Start is called before the first frame update
    void Start()
    {
        onEntry = new Dictionary<TutorialState, Action>()
        {
            [TutorialState.Enter] = () =>
            {
                isometricCameraController.LerpToCharacter(player.transform, 2f, ()=>
                {
                    bob.LookAtTarget(player.transform.position);
                    gameManager.GetCurrentState().SetSubstate(new DialogueSubState(player.CurrentSelected, bob, gameManager, false));
                });                
            },
        };
        if(playScriptedScenerio) SetState(currentState);
    }

    // Update is called once per frame
    void Update()
    {
        ///fsm.Update();
    }

    private void SetState(TutorialState next)
    {
        currentState = next;
        if (onEntry.TryGetValue(currentState, out var enter)) enter?.Invoke();
    }
}
