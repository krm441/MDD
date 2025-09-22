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
            spell.apCost = EditorGUILayout.IntField("AP Cost", spell.apCost);
            spell.range = EditorGUILayout.IntField("Range", spell.range);
            spell.radius = EditorGUILayout.IntField("Radius", spell.radius);
            //spell.baseDamage = EditorGUILayout.FloatField("Base damage", spell.baseDamage);
            spell.vfxType = EditorGUILayout.TextField("VFX", spell.vfxType);
            spell.sfxOnStart = EditorGUILayout.TextField("SFX Start", spell.sfxOnStart);
            spell.sfxOnFly = EditorGUILayout.TextField("SFX Fly", spell.sfxOnFly);
            spell.sfxOnImpact = EditorGUILayout.TextField("SFX Impact", spell.sfxOnImpact);
            spell.physicsType = (SpellPhysicsType)EditorGUILayout.EnumPopup("Physics Type", spell.physicsType);
            spell.dPSType = (SpellDPSType)EditorGUILayout.EnumPopup("DPS Type", spell.dPSType);
            spell.hidden = EditorGUILayout.Toggle("Hidden", spell.hidden);
            spell.friendlyFire = EditorGUILayout.Toggle("Friendly Fire", spell.friendlyFire);

            DrawDamageContainer(spell.baseDamage);

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

    public static void DrawDamageContainer(DamageResistenceContainer dmg, string label = "Base Damage")
    {
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("Physical");
        dmg.Slashing = EditorGUILayout.IntField("  Slashing", dmg.Slashing);
        dmg.Piercing = EditorGUILayout.IntField("  Piercing", dmg.Piercing);
        dmg.Crushing = EditorGUILayout.IntField("  Crushing", dmg.Crushing);

        EditorGUILayout.LabelField("Elemental");
        dmg.Fire = EditorGUILayout.IntField("  Fire", dmg.Fire);
        dmg.Water = EditorGUILayout.IntField("  Water", dmg.Water);
        dmg.Wind = EditorGUILayout.IntField("  Wind", dmg.Wind);
        dmg.Earth = EditorGUILayout.IntField("  Earth", dmg.Earth);

        EditorGUILayout.LabelField("Spiritual");
        dmg.Light = EditorGUILayout.IntField("  Light", dmg.Light);
        dmg.Shadow = EditorGUILayout.IntField("  Shadow", dmg.Shadow);

        EditorGUILayout.LabelField("Heretique");
        dmg.Necrotic = EditorGUILayout.IntField("  Necrotic", dmg.Necrotic);
        dmg.Poison = EditorGUILayout.IntField("  Poison", dmg.Poison);
        dmg.Demonic = EditorGUILayout.IntField("  Demonic", dmg.Demonic);

        EditorGUILayout.LabelField("Support / Healing");
        dmg.Healing = EditorGUILayout.IntField("  Healing", dmg.Healing);
        dmg.MentalFortification = EditorGUILayout.IntField("  Mental Fortification", dmg.MentalFortification);
        dmg.MagicFortification = EditorGUILayout.IntField("  Magic Fortification", dmg.MagicFortification);

        EditorGUI.indentLevel--;
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
