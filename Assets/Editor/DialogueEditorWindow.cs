using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static Codice.Client.Common.Connection.AskCredentialsToUser;

public class DialogueEditorWindow : EditorWindow
{
    private DialogueData dialogueData = new DialogueData { lines = new List<DialogueLine>() };
    private Vector2 scroll;
    private string dialogueId = "new_dialogue";
    private string savePath = "Assets/Resources/Dialogues/";

    [MenuItem("Tools/Dialogue Editor")]
    public static void OpenWindow() => GetWindow<DialogueEditorWindow>("Dialogue Editor");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Dialogue Editor", EditorStyles.boldLabel);
        dialogueId = EditorGUILayout.TextField("Dialogue ID", dialogueId);
        dialogueData.id = dialogueId;

        EditorGUILayout.Space();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < dialogueData.lines.Count; i++)
        {
            var line = dialogueData.lines[i];
            EditorGUILayout.BeginVertical("box");
            line.speaker = EditorGUILayout.TextField("Speaker", line.speaker);
            line.text = EditorGUILayout.TextField("Text", line.text);
            line.voiceClip = EditorGUILayout.TextField("Voice Clip", line.voiceClip);

            if (GUILayout.Button("Delete Line"))
            {
                dialogueData.lines.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add New Line"))
        {
            dialogueData.lines.Add(new DialogueLine());
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Save Dialogue to JSON"))
        {
            SaveDialogue();
        }

        if (GUILayout.Button("Load Dialogue from JSON"))
        {
            LoadDialogue();
        }
    }

    private void SaveDialogue()
    {
        string fullPath = Path.Combine(savePath, dialogueId + ".json");
        Directory.CreateDirectory(savePath);
        string json = JsonUtility.ToJson(dialogueData, true);
        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();
        Debug.Log($"Dialogue saved to {fullPath}");
    }

    private void LoadDialogue()
    {
        string fullPath = Path.Combine(savePath, dialogueId + ".json");
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning($"No dialogue found at {fullPath}");
            return;
        }

        string json = File.ReadAllText(fullPath);
        dialogueData = JsonUtility.FromJson<DialogueData>(json);
    }
}
