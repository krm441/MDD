using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

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

    private void Start()
    {
        dialoguePanelPrefab.SetActive(false);
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

        var controller = dialoguePanelPrefab.GetComponent<DialogueUIController>();
        controller.StartDialogue(initiator, target);
        return controller;
    }


    public void HideDialogueUI()
    {
        if (dialoguePanelPrefab != null)
        {
            dialoguePanelPrefab.SetActive(false);
        }
        combatUIPrefab.SetActive(true);
    }


    private static UIStates currentState = UIStates.None;
    public static void SetState(UIStates state)
    {
        currentState = state;
    }
}
