using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PartyManagement;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

public class ScinematicIntroController : MonoBehaviour
{
    [SerializeField] private IsometricCameraController cameraMDD;
    [SerializeField] private Transform target;
    [SerializeField] private GameObject panelDialogues;
    [SerializeField] private GameObject buttonFinish;
    [SerializeField] private GameObject buttonNext;
    [SerializeField] private TextMeshProUGUI conversationText;
    [SerializeField] private Transform finalGaze;
    [SerializeField] private UnityEngine.UI.Image portrait;

    [Header("Characters")]
    [SerializeField] private CharacterUnit magus;
    [SerializeField] private CharacterUnit rogue;
    [SerializeField] private CharacterUnit barbarian;
    [SerializeField] private CharacterUnit druid;

    [SerializeField] private GameObject nemessis;

    [Header("Portraits")]
    [SerializeField] private Sprite magusPortrait;
    [SerializeField] private Sprite roguePortrait;
    [SerializeField] private Sprite druPortrait;
    [SerializeField] private Sprite barbarPortrait;
    [SerializeField] private Sprite nemesPortrait;

    private enum IntroState 
    { 
        // intro camera lerp
        StartLerp, 

        // party conversation
        ConversationPatchOne,
        ConversationPatchTwo,
        ConversationPatchThree,
        ConversationPatchFour,

        // nemessis appear
        NemessisAppear,
        PartyCollapse,
        Exit,
    }
    private IntroState currentState;

    private List<IntroState> statesConsequtive = new List<IntroState>()
    { 
        IntroState.StartLerp,
        IntroState.ConversationPatchOne,
        IntroState.ConversationPatchTwo,
        IntroState.ConversationPatchThree,
        IntroState.ConversationPatchFour,
        IntroState.NemessisAppear,
        IntroState.PartyCollapse,
        IntroState.Exit
    };

    private Dictionary<IntroState, Action> onEntry;
    private Dictionary<IntroState, Action> onUpdate;
    private Dictionary<IntroState, string> conversations;

    private void InitStates()
    {
        // entry
        onEntry = new Dictionary<IntroState, Action>()
        {
            [IntroState.StartLerp] = () =>
            {
                cameraMDD.LerpToCharacter(target, 3f, () => { panelDialogues.SetActive(true); NextState(); });
                
            },
            [IntroState.ConversationPatchOne] =     () => { portrait.sprite = magusPortrait; FlushConversation(); },
            [IntroState.ConversationPatchTwo] =     () => { portrait.sprite = roguePortrait; FlushConversation(); },
            [IntroState.ConversationPatchThree] =   () => { portrait.sprite = druPortrait; FlushConversation(); },
            [IntroState.ConversationPatchFour] =    () => { portrait.sprite = barbarPortrait; FlushConversation(); },
            [IntroState.NemessisAppear] =           () => 
            {
                portrait.sprite = nemesPortrait;
                FlushConversation();
                nemessis.SetActive(true);
                var charUnit = nemessis.GetComponentInChildren<CharacterUnit>();
                charUnit.animator.Play("Spawn_Ground");
            },
            [IntroState.PartyCollapse] = () =>
            {
                FlushConversation();
                
                magus.PlayDeathAnimation(() =>
                {
                    Console.Error("back");
                });
                barbarian.PlayDeathAnimation();
                druid.PlayDeathAnimation();
                rogue.PlayDeathAnimation();
            },
            [IntroState.Exit] = () =>
            {
                ChangeButtons();
                FlushConversation();
                var charUnit = nemessis.GetComponentInChildren<CharacterUnit>();
                charUnit.LookAtTarget(finalGaze.position);
                ChangeButtons();
            },
        };

        onUpdate = new Dictionary<IntroState, Action>
        {
            [IntroState.StartLerp] = () =>            {            },
            [IntroState.ConversationPatchOne] = () => {  },
            [IntroState.ConversationPatchTwo] = () => {  },
            [IntroState.ConversationPatchThree] = () => {  },
            [IntroState.ConversationPatchFour] = () => {  },
            [IntroState.NemessisAppear] = () => {  },
        };
    }

    private void InitConversations()
    {
        conversations = new Dictionary<IntroState, string>
        {
            [IntroState.StartLerp] = "",
            [IntroState.ConversationPatchOne] = "I am delighted to have finally completed the Adventurers Academy",
            [IntroState.ConversationPatchTwo] = "Now we are up to the real life of adventurerers",
            [IntroState.ConversationPatchThree] = "I am pleased I have no more exams, and sleepless nights",
            [IntroState.ConversationPatchFour] = "Grr Grr Grrr...",
            [IntroState.NemessisAppear] = "Well well... Good to see you here... Welcome to the infinite nightmare! You will be trapped in my endless illusion",
            [IntroState.PartyCollapse] = "Party is unconcious",
            [IntroState.Exit] = "Time to commence the real life advanture!",
        };
    }

    void Awake()
    {
        nemessis.SetActive(false);
        panelDialogues.SetActive(false);
        buttonFinish.SetActive(false);
        InitStates();
        InitConversations();

        SetState(IntroState.StartLerp);
    }

    void Update()
    {
        if (onUpdate.TryGetValue(currentState, out var tick)) tick?.Invoke();
    }

    private void SetState(IntroState next)
    {
        currentState = next;
        if (onEntry.TryGetValue(currentState, out var enter)) enter?.Invoke();
    }

    public void NextState()
    {
        int currentIndex = statesConsequtive.IndexOf(currentState);
        currentState = statesConsequtive[++currentIndex];
        Assert.IsTrue(currentIndex < statesConsequtive.Count);
        SetState(currentState);
    }

    public void FinishIntroScene()
    {
        SceneManager.LoadScene("Tutorial");
    }

    private void ChangeButtons()
    {
        buttonFinish.SetActive(true);
        buttonNext.SetActive(false);
    }

    private void FlushConversation()
    {
        conversationText.text = conversations[currentState];
    }
}
