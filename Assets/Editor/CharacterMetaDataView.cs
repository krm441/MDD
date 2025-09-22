using System.Collections.Generic;
using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using System.IO;

public class CharacterMetaDataEditorWindow : EditorWindow
{
    private CharacterMetaData data = new CharacterMetaData
    {
        attributeSet = new AttributeSet
        {
            stats = new StatBlock(),
            armorStat = new ArmorStat(),
            resistances = new DamageResistenceContainer()
        }
    };

    private Vector2 scroll;
    private string currentPath;

    private bool foldIdentity = true;
    private bool foldStats = true;
    private bool foldArmor = true;
    private bool foldRes = true;
    private bool foldSpells = true;
    private bool autoClamp = true;

    [MenuItem("Tools/Character MetaData Editor")]
    public static void Open()
    {
        var win = GetWindow<CharacterMetaDataEditorWindow>("Character MetaData");
        win.minSize = new Vector2(560, 560);
        win.Show();
    }

    private void OnGUI()
    {
        DrawToolbar();

        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.Space();

        foldIdentity = EditorGUILayout.Foldout(foldIdentity, "Identity", true);
        if (foldIdentity) DrawIdentity(data);

        EditorGUILayout.Space(6);

        EnsureAttributeSet();
        foldStats = EditorGUILayout.Foldout(foldStats, "Stats (StatBlock)", true);
        if (foldStats) DrawStatBlock(data.attributeSet.stats);

        EditorGUILayout.Space(6);

        foldArmor = EditorGUILayout.Foldout(foldArmor, "Armor (ArmorStat)", true);
        if (foldArmor) DrawArmorStat(data.attributeSet.armorStat);

        EditorGUILayout.Space(6);

        foldRes = EditorGUILayout.Foldout(foldRes, "Resistances (DamageResistenceContainer)", true);
        if (foldRes) DrawResistances(data.attributeSet.resistances);

        EditorGUILayout.Space(6);

        foldSpells = EditorGUILayout.Foldout(foldSpells, "Spells (List<string>)", true);
        if (foldSpells) DrawSpells(data.spells);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            string.IsNullOrEmpty(currentPath) ? "No file loaded. Use Save or Save As to write a JSON file."
                                               : $"Editing: {currentPath}",
            MessageType.Info);
    }

    // ================= Toolbar / File I-O ================= //

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("New", EditorStyles.toolbarButton))
        {
            data = new CharacterMetaData
            {
                attributeSet = new AttributeSet
                {
                    stats = new StatBlock(),
                    armorStat = new ArmorStat(),
                    resistances = new DamageResistenceContainer()
                }
            };
            currentPath = null;
        }

        if (GUILayout.Button("Load JSON...", EditorStyles.toolbarButton))
        {
            var path = EditorUtility.OpenFilePanel("Load Character JSON", Application.dataPath + "Resources/Data", "json");
            if (!string.IsNullOrEmpty(path)) LoadFromJson(path);
        }

        if (GUILayout.Button("Save", EditorStyles.toolbarButton))
        {
            if (string.IsNullOrEmpty(currentPath)) SaveAsJson();
            else SaveToJson(currentPath);
        }

        if (GUILayout.Button("Save As...", EditorStyles.toolbarButton))
        {
            SaveAsJson();
        }

        GUILayout.FlexibleSpace();

        autoClamp = GUILayout.Toggle(autoClamp, "Auto-Clamp", EditorStyles.toolbarButton);

        EditorGUILayout.EndHorizontal();
    }

    private void SaveAsJson()
    {
        var defaultDir = Path.Combine(Application.dataPath, "Resources/Data");
        if (!Directory.Exists(defaultDir)) Directory.CreateDirectory(defaultDir);

        var name = string.IsNullOrWhiteSpace(data.unitName) ? "Character" : SanitizeFileName(data.unitName);
        var path = EditorUtility.SaveFilePanel("Save Character JSON", defaultDir, name, "json");
        if (!string.IsNullOrEmpty(path))
        {
            SaveToJson(path);
            TryRefreshIfInsideAssets(path);
        }
    }

    private void SaveToJson(string path)
    {
        try
        {
            NormalizeData(data);

            var json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            currentPath = path;
            ShowNotification(new GUIContent("Saved"));
        }
        catch (Exception ex)
        {
            Debug.LogError($"Save failed: {ex}");
            EditorUtility.DisplayDialog("Save Error", ex.Message, "OK");
        }
    }

    private void LoadFromJson(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonUtility.FromJson<CharacterMetaData>(json);
            if (loaded == null) throw new Exception("JSON does not match CharacterMetaData format.");

            data = loaded;
            EnsureAttributeSet(); // ensure nested objects are allocated
            currentPath = path;

            ShowNotification(new GUIContent("Loaded"));
            TryRefreshIfInsideAssets(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Load failed: {ex}");
            EditorUtility.DisplayDialog("Load Error", ex.Message, "OK");
        }
    }

    private void TryRefreshIfInsideAssets(string path)
    {
        var inAssets = path.Replace('\\', '/').StartsWith(Application.dataPath.Replace('\\', '/'));
        if (inAssets) AssetDatabase.Refresh();
    }

    // ================= Sections ================= //

    private void DrawIdentity(CharacterMetaData m)
    {
        EditorGUI.indentLevel++;
        m.unitName = EditorGUILayout.TextField("Unit Name", m.unitName);
        m.isMainCharacter = EditorGUILayout.Toggle("Is Main Character", m.isMainCharacter);
        m.portraitPrefabName = EditorGUILayout.TextField("Portrait Prefab Name", m.portraitPrefabName);
        m.rigMeshName = EditorGUILayout.TextField("Rig Mesh Name", m.rigMeshName);
        EditorGUI.indentLevel--;
    }

    private void DrawStatBlock(StatBlock s)
    {
        EditorGUI.indentLevel++;
        s.Intelligence = EditorGUILayout.IntField("Intelligence", s.Intelligence);
        s.Willpower = EditorGUILayout.IntField("Willpower", s.Willpower);
        s.Devotion = EditorGUILayout.IntField("Devotion", s.Devotion);

        EditorGUILayout.Space(2);
        s.HP = EditorGUILayout.IntField("HP", s.HP);
        s.MaxHP = EditorGUILayout.IntField("Max HP", s.MaxHP);

        EditorGUILayout.Space(2);
        s.Speed = EditorGUILayout.IntField("Speed (tiles/turn)", s.Speed);
        s.Initiative = EditorGUILayout.IntField("Initiative", s.Initiative);
        s.ActionPoints = EditorGUILayout.IntField("Action Points", s.ActionPoints);
        s.MaxActionPoints = EditorGUILayout.IntField("Max Action Points", s.MaxActionPoints);
        s.StartActionPoints = EditorGUILayout.IntField("Start Action Points", s.StartActionPoints);

        if (autoClamp)
        {
            s.MaxHP = Math.Max(0, s.MaxHP);
            s.HP = Mathf.Clamp(s.HP, 0, s.MaxHP);
            s.MaxActionPoints = Math.Max(0, s.MaxActionPoints);
            s.ActionPoints = Mathf.Clamp(s.ActionPoints, 0, s.MaxActionPoints);
            s.StartActionPoints = Mathf.Clamp(s.StartActionPoints, 0, s.MaxActionPoints);
            s.Speed = Math.Max(0, s.Speed);
            s.Initiative = Math.Max(0, s.Initiative);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawArmorStat(ArmorStat a)
    {
        EditorGUI.indentLevel++;
        a.physicalArmor = EditorGUILayout.IntField("Physical Armor", a.physicalArmor);
        a.maxPhysicalArmor = EditorGUILayout.IntField("Max Physical Armor", a.maxPhysicalArmor);
        a.magicArmor = EditorGUILayout.IntField("Magic Armor", a.magicArmor);
        a.maxMagicArmor = EditorGUILayout.IntField("Max Magic Armor", a.maxMagicArmor);
        a.moraleLevel = EditorGUILayout.IntSlider("Morale Level", a.moraleLevel, 0, 100);

        if (autoClamp)
        {
            a.maxPhysicalArmor = Math.Max(0, a.maxPhysicalArmor);
            a.physicalArmor = Mathf.Clamp(a.physicalArmor, 0, a.maxPhysicalArmor);

            a.maxMagicArmor = Math.Max(0, a.maxMagicArmor);
            a.magicArmor = Mathf.Clamp(a.magicArmor, 0, a.maxMagicArmor);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawResistances(DamageResistenceContainer r)
    {
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("Physical", EditorStyles.boldLabel);
        r.Slashing = EditorGUILayout.IntField("Slashing", r.Slashing);
        r.Piercing = EditorGUILayout.IntField("Piercing", r.Piercing);
        r.Crushing = EditorGUILayout.IntField("Crushing", r.Crushing);
        EditorGUILayout.LabelField($"Total Physical: {r.TotalPhysical}");

        EditorGUILayout.Space(2);

        EditorGUILayout.LabelField("Elemental", EditorStyles.boldLabel);
        r.Fire = EditorGUILayout.IntField("Fire", r.Fire);
        r.Water = EditorGUILayout.IntField("Water", r.Water);
        r.Wind = EditorGUILayout.IntField("Wind", r.Wind);
        r.Earth = EditorGUILayout.IntField("Earth", r.Earth);
        EditorGUILayout.LabelField($"Total Elemental: {r.TotalElemental}");

        EditorGUILayout.Space(2);

        EditorGUILayout.LabelField("Spiritual", EditorStyles.boldLabel);
        r.Light = EditorGUILayout.IntField("Light", r.Light);
        r.Shadow = EditorGUILayout.IntField("Shadow", r.Shadow);
        EditorGUILayout.LabelField($"Total Spiritual: {r.TotalSpiritual}");

        EditorGUILayout.Space(2);

        EditorGUILayout.LabelField("Heretique", EditorStyles.boldLabel);
        r.Necrotic = EditorGUILayout.IntField("Necrotic", r.Necrotic);
        r.Poison = EditorGUILayout.IntField("Poison", r.Poison);
        r.Demonic = EditorGUILayout.IntField("Demonic", r.Demonic);
        EditorGUILayout.LabelField($"Total Heretique: {r.TotalHeretique}");

        EditorGUILayout.Space(2);

        EditorGUILayout.LabelField("Healing & Fortification", EditorStyles.boldLabel);
        r.Healing = EditorGUILayout.IntField("Healing", r.Healing);
        r.MentalFortification = EditorGUILayout.IntField("Mental Fortification", r.MentalFortification);
        r.MagicFortification = EditorGUILayout.IntField("Magic Fortification", r.MagicFortification);

        if (autoClamp)
        {
            ClampAllResistances(r, -100, 100);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawSpells(List<string> list)
    {
        if (list == null) data.spells = list = new List<string>();

        EditorGUI.indentLevel++;

        for (int i = 0; i < list.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            list[i] = EditorGUILayout.TextField($"[{i}]", list[i]);
            if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0) Swap(list, i, i - 1);
            if (GUILayout.Button("↓", GUILayout.Width(24)) && i < list.Count - 1) Swap(list, i, i + 1);
            if (GUILayout.Button("X", GUILayout.Width(24))) { list.RemoveAt(i); GUIUtility.ExitGUI(); }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Spell")) list.Add(string.Empty);
        if (GUILayout.Button("Sort & Deduplicate"))
        {
            data.spells = list = list
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    // ================= Helpers ================= //

    private void EnsureAttributeSet()
    {
        if (data.attributeSet == null)
            data.attributeSet = new AttributeSet();

        if (data.attributeSet.stats == null)
            data.attributeSet.stats = new StatBlock();

        if (data.attributeSet.armorStat == null)
            data.attributeSet.armorStat = new ArmorStat();

        if (data.attributeSet.resistances == null)
            data.attributeSet.resistances = new DamageResistenceContainer();
    }

    private static void NormalizeData(CharacterMetaData d)
    {
        d.unitName = (d.unitName ?? "").Trim();
        d.portraitPrefabName = (d.portraitPrefabName ?? "").Trim();
        d.rigMeshName = (d.rigMeshName ?? "").Trim();

        if (d.spells == null) d.spells = new List<string>();
        d.spells = d.spells
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void ClampAllResistances(DamageResistenceContainer r, int min, int max)
    {
        r.Slashing = Mathf.Clamp(r.Slashing, min, max);
        r.Piercing = Mathf.Clamp(r.Piercing, min, max);
        r.Crushing = Mathf.Clamp(r.Crushing, min, max);

        r.Fire = Mathf.Clamp(r.Fire, min, max);
        r.Water = Mathf.Clamp(r.Water, min, max);
        r.Wind = Mathf.Clamp(r.Wind, min, max);
        r.Earth = Mathf.Clamp(r.Earth, min, max);

        r.Light = Mathf.Clamp(r.Light, min, max);
        r.Shadow = Mathf.Clamp(r.Shadow, min, max);

        r.Necrotic = Mathf.Clamp(r.Necrotic, min, max);
        r.Poison = Mathf.Clamp(r.Poison, min, max);
        r.Demonic = Mathf.Clamp(r.Demonic, min, max);

        r.Healing = Mathf.Clamp(r.Healing, min, max);
        r.MentalFortification = Mathf.Clamp(r.MentalFortification, min, max);
        r.MagicFortification = Mathf.Clamp(r.MagicFortification, min, max);
    }

    private static void Swap<T>(IList<T> list, int a, int b) { var t = list[a]; list[a] = list[b]; list[b] = t; }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((name ?? "").Where(c => !invalid.Contains(c)).ToArray());
    }
}