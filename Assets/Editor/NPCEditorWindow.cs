using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

public class NPCEditorWindow : EditorWindow
{
    private List<NPCDefinition> npcList = new List<NPCDefinition>();
    private Vector2 scroll;
    private string savePath = "Assets/Resources/Data/NPC.json";

    [MenuItem("Tools/NPC Editor")]
    public static void OpenWindow() => GetWindow<NPCEditorWindow>("NPC Editor");

    private void OnEnable()
    {
        LoadFromFile();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("NPC Editor", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < npcList.Count; i++)
        {
            var npc = npcList[i];

            EditorGUILayout.BeginVertical("box");

            npc.id = EditorGUILayout.TextField("ID", npc.id);
            npc.displayName = EditorGUILayout.TextField("Display Name", npc.displayName);
            npc.prefabPath = EditorGUILayout.TextField("Prefab Path", npc.prefabPath);
            npc.visualPath = EditorGUILayout.TextField("Visual Path", npc.visualPath);
            npc.hostility = (Hostility)EditorGUILayout.EnumPopup("Hostility", npc.hostility);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);

            npc.statBlock.Intelligence = EditorGUILayout.IntField("Intelligence", npc.statBlock.Intelligence);
            npc.statBlock.Willpower = EditorGUILayout.IntField("Willpower", npc.statBlock.Willpower);
            npc.statBlock.Devotion = EditorGUILayout.IntField("Devotion", npc.statBlock.Devotion);
            npc.statBlock.HP = EditorGUILayout.IntField("HP", npc.statBlock.HP);
            npc.statBlock.MaxHP = EditorGUILayout.IntField("Max HP", npc.statBlock.MaxHP);
            npc.statBlock.Speed = EditorGUILayout.IntField("Speed", npc.statBlock.Speed);
            npc.statBlock.Initiative = EditorGUILayout.IntField("Initiative", npc.statBlock.Initiative);
            npc.statBlock.ActionPoints = EditorGUILayout.IntField("Action Points", npc.statBlock.ActionPoints);
            npc.statBlock.StartActionPoints = EditorGUILayout.IntField("Start Action Points", npc.statBlock.StartActionPoints);
            npc.statBlock.MaxActionPoints = EditorGUILayout.IntField("Max Action Points", npc.statBlock.MaxActionPoints);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Armor", EditorStyles.boldLabel);
            npc.armorStat.physicalArmor = EditorGUILayout.IntField("Physical Armor", npc.armorStat.physicalArmor);
            npc.armorStat.magicArmor = EditorGUILayout.IntField("Magic Armor", npc.armorStat.magicArmor);
            npc.armorStat.moraleLevel = EditorGUILayout.IntField("Morale Level", npc.armorStat.moraleLevel);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Delete NPC"))
            {
                npcList.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("Add New NPC"))
        {
            npcList.Add(new NPCDefinition());
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Save to JSON"))
        {
            SaveToFile();
        }
    }

    private void SaveToFile()
    {
        string dir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var wrapper = new Wrapper { list = npcList };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(savePath, json);
        AssetDatabase.Refresh();
        Debug.Log($"NPC definitions saved to {savePath}");
    }

    private void LoadFromFile()
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        var wrapper = JsonUtility.FromJson<Wrapper>(json);
        npcList = wrapper.list ?? new List<NPCDefinition>();
    }

    [System.Serializable]
    private class Wrapper { public List<NPCDefinition> list; }
}
