#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class SpellEditorWindow : EditorWindow
{
    private SpellListWrapper spellData;// = new SpellListWrapper();
    private Vector2 scroll;
    private string jsonPath = "Assets/Resources/Data/spells.json";

    [MenuItem("Tools/Spell Editor")]
    public static void ShowWindow()
    {
        GetWindow<SpellEditorWindow>("Spell Editor");
    }

    private void OnEnable()
    {
        LoadSpells(); 
    }

    private void OnGUI()
    {
        if (spellData == null)
            spellData = new SpellListWrapper();

        if (spellData.spells == null)
            spellData.spells = new List<Spell>();

        scroll = EditorGUILayout.BeginScrollView(scroll);

        for (int i = 0; i < spellData.spells.Count; i++)
        {
            EditorGUILayout.BeginVertical("box");
            var spell = spellData.spells[i];

            spell.id = EditorGUILayout.IntField("ID", spell.id);
            spell.name = EditorGUILayout.TextField("Name", spell.name);
            spell.description = EditorGUILayout.TextField("Description", spell.description);
            spell.iconPath = EditorGUILayout.TextField("Icon Path", spell.iconPath);
            spell.shotcutKey = EditorGUILayout.TextField("Shortcut Key", spell.shotcutKey);
            spell.manaCost = EditorGUILayout.IntField("Mana Cost", spell.manaCost);
            spell.range = EditorGUILayout.IntField("Range", spell.range);
            spell.radius = EditorGUILayout.IntField("Radius", spell.radius);
            spell.baseDamage = EditorGUILayout.FloatField("Base damage", spell.baseDamage);
            spell.vfxType = EditorGUILayout.TextField("VFX", spell.vfxType);
            spell.physicsType = (SpellPhysicsType)EditorGUILayout.EnumPopup("Physics Type", spell.physicsType);

            if (GUILayout.Button("Remove Spell"))
            {
                spellData.spells.RemoveAt(i);
                break;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        if (GUILayout.Button("Add New Spell"))
        {
            spellData.spells.Add(new Spell { id = GetNextId() });
        }

        if (GUILayout.Button("Save to JSON"))
        {
            SaveSpells();
        }
    }

    private void LoadSpells()
    {
        if (File.Exists(jsonPath))
        {
            string json = File.ReadAllText(jsonPath);
            spellData = JsonUtility.FromJson<SpellListWrapper>(json);
        }
        else
        {
            spellData = new SpellListWrapper { spells = new List<Spell>() };
        }
    }

    private void SaveSpells()
    {
        string json = JsonUtility.ToJson(spellData, true);
        File.WriteAllText(jsonPath, json);
        AssetDatabase.Refresh();
        Debug.Log("Spells saved to JSON.");
    }

    private int GetNextId()
    {
        int max = -1;
        foreach (var spell in spellData.spells)
        {
            if (spell.id > max)
                max = spell.id;
        }
        return max + 1;
    }
}
#endif
