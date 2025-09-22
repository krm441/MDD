using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using PartyManagement;

public class DialogueUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Transform choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private UIManager UIManager;

    public System.Action OnDialogueFinished;

    private DialogueData dialogueData;
    private DialogueNode currentNode;

    private CharacterUnit initiator;
    private CharacterUnit target;

    public void StartDialogue(CharacterUnit initiator, CharacterUnit target)
    {
        this.initiator = initiator;
        this.target = target;

        LoadDialogue($"dialogue_{target.unitName.ToLower()}");
        //LoadDialogue($"dialogue_bob");
        ShowNode(dialogueData.nodes[0]); // start with the first node
    }

    private void LoadDialogue(string dialogueId)
    {
        string path = $"Dialogues/{dialogueId}";
        TextAsset jsonFile = Resources.Load<TextAsset>(path);

        if (jsonFile == null)
        {
            Debug.LogError($"Dialogue file not found: {path}");
            return;
        }

        dialogueData = JsonUtility.FromJson<DialogueData>(jsonFile.text);
    }

    private void ShowNode(DialogueNode node)
    {
        currentNode = node;
        ClearChoices();

        speakerText.text = node.speaker;
        dialogueText.text = node.text;

        if (node.choices != null && node.choices.Count > 0)
        {
            foreach (var choice in node.choices)
            {
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
                TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
                buttonText.text = choice.text;

                Button button = buttonObj.GetComponent<Button>();
                string nextNodeId = choice.nextNodeId;

                button.onClick.AddListener(() => OnChoiceSelected(nextNodeId));
            }
        }
        else
        {
            // Autoadvance if no choices
            Invoke(nameof(EndDialogue), 1f); // delay before closing
        }
    }

    private void OnChoiceSelected(string nextNodeId)
    {
        var nextNode = dialogueData.nodes.Find(n => n.id == nextNodeId);
        if (nextNode != null)
        {
            ShowNode(nextNode);
        }
        else
        {
            Debug.LogWarning($"Next node ID not found: {nextNodeId}");
            EndDialogue();
        }
    }

    private void ClearChoices()
    {
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void EndDialogue()
    {
        UIManager.HideDialogueUI();

        if (OnDialogueFinished != null)
            OnDialogueFinished.Invoke();
    }
}
