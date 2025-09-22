using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.Assertions;

public enum UIStates
{
    None,       // dominant in scinematic transitions
    Exploration,
    Combat,
    Dialogue,   // state where the dialogues with NPC or party happens
}

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject dialoguePanelPrefab;
    [SerializeField] private GameObject combatUIPrefab;
    [SerializeField] private GameObject apUIPrefab;
    [SerializeField] private MenuController menuController;
    [SerializeField] private PanelDialogue pannelDialogue;

    private void Start()
    {
        dialoguePanelPrefab.SetActive(false);
        pannelDialogue.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Assert.IsNotNull(menuController);
            menuController.ShowHideMenu();
        }
    }


    public DialogueUIController LoadDialogueUI(CharacterUnit initiator, CharacterUnit target)
    {
        if (dialoguePanelPrefab == null)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/DialoguePanel");
            dialoguePanelPrefab = Instantiate(prefab);
        }

        dialoguePanelPrefab.SetActive(true);
        combatUIPrefab.SetActive(false);
        apUIPrefab.SetActive(false);

        var controller = dialoguePanelPrefab.GetComponent<DialogueUIController>();
        controller.StartDialogue(initiator, target);
        return controller;
    }

    public void URLopenTest()
    {
        Application.OpenURL("www.google.com");
    }

    public void HideDialogueUI()
    {
        if (dialoguePanelPrefab != null)
        {
            dialoguePanelPrefab.SetActive(false);
        }
        combatUIPrefab.SetActive(true);
        apUIPrefab.SetActive(true);
    }


    private static UIStates currentState = UIStates.None;
    public static void SetState(UIStates state)
    {
        currentState = state;
    }
}
