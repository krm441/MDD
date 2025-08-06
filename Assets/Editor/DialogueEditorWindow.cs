using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class DialogueEditorWindow : EditorWindow
{
    private List<DialogueData> allDialogues = new List<DialogueData>();
    private string dialoguesFolder = "Assets/Resources/Dialogues";
    private Vector2 scroll;
    private Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();


    [MenuItem("Tools/Dialogue Editor")]
    public static void OpenWindow() => GetWindow<DialogueEditorWindow>("Dialogue Editor");

    private void OnEnable()
    {
        LoadAllDialogues();
    }

    private void OnGUI()
    {
        if (allDialogues.Count == 0)
        {
            EditorGUILayout.HelpBox("No dialogues found. Place JSON files in 'Assets/Resources/Dialogues'.", MessageType.Info);
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);

        foreach (var dialogue in allDialogues)
        {
            if (!foldoutStates.ContainsKey(dialogue.id))
                foldoutStates[dialogue.id] = false;

            foldoutStates[dialogue.id] = EditorGUILayout.Foldout(foldoutStates[dialogue.id], $"Dialogue: {dialogue.id}", true, EditorStyles.foldoutHeader);

            if (!foldoutStates[dialogue.id])
                continue;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Dialogue ID: " + dialogue.id, EditorStyles.boldLabel);

            for (int i = 0; i < dialogue.nodes.Count; i++)
            {
                var node = dialogue.nodes[i];
                EditorGUILayout.BeginVertical("box");

                node.id = EditorGUILayout.TextField("Node ID", node.id);
                node.speaker = EditorGUILayout.TextField("Speaker", node.speaker);
                node.text = EditorGUILayout.TextField("Text", node.text);
                node.voiceClip = EditorGUILayout.TextField("Voice Clip", node.voiceClip);

                if (node.choices == null)
                    node.choices = new List<DialogueChoice>();

                EditorGUILayout.LabelField("Choices:", EditorStyles.boldLabel);
                for (int j = 0; j < node.choices.Count; j++)
                {
                    var choice = node.choices[j];
                    EditorGUILayout.BeginHorizontal();
                    choice.text = EditorGUILayout.TextField("Text", choice.text);
                    choice.nextNodeId = EditorGUILayout.TextField("Next", choice.nextNodeId);

                    if (GUILayout.Button("X", GUILayout.Width(20)))
                    {
                        node.choices.RemoveAt(j);
                        break;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Add Choice"))
                    node.choices.Add(new DialogueChoice());

                if (GUILayout.Button("Delete Node"))
                {
                    dialogue.nodes.RemoveAt(i);
                    break;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            if (GUILayout.Button("Add Node"))
                dialogue.nodes.Add(new DialogueNode { id = "new", choices = new List<DialogueChoice>() });

            if (GUILayout.Button("Save Dialogue"))
                SaveDialogue(dialogue);

            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadAllDialogues()
    {
        allDialogues.Clear();

        if (!Directory.Exists(dialoguesFolder))
        {
            Debug.LogWarning("Dialogue folder not found: " + dialoguesFolder);
            return;
        }

        string[] files = Directory.GetFiles(dialoguesFolder, "*.json");

        foreach (var file in files)
        {
            string json = File.ReadAllText(file);
            DialogueData data = JsonUtility.FromJson<DialogueData>(json);
            if (data != null && !string.IsNullOrEmpty(data.id))
            {
                allDialogues.Add(data);
            }
        }

        Debug.Log($"Loaded {allDialogues.Count} dialogue(s) from {dialoguesFolder}");
    }

    private void SaveDialogue(DialogueData dialogue)
    {
        string path = Path.Combine(dialoguesFolder, dialogue.id + ".json");
        string json = JsonUtility.ToJson(dialogue, true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
        Debug.Log($"Saved: {path}");
    }
}
