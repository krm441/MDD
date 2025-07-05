using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine;

public class SpellBook
{
    public void AddSpell(Spell spell)
    {
        if (!spells.Contains(spell))
            spells.Add(spell);
        else
            Debug.Log(spell.name + " Not found");
    }

    public void RemoveSpell(Spell spell)
    {
        if(spells.Contains(spell))
            spells.Remove(spell);
    }

    public List<Spell> GetAllSpells()
    {
        return new List<Spell>(spells);
    }


    private List<Spell> spells = new List<Spell>();
}

[System.Serializable]
public class Spell
{
    public int id;
    public string name;
    public string description;
    public string iconPath;
    public string shotcutKey;
    public string prefabMeshEffect; // mesh or effect prefab path
    //public Animation??
    public int manaCost;
    public int range;
    public int radius;
}

/// <summary>
/// Simple data structure as a wrapper for JSON serialisation
/// </summary>
[System.Serializable]
public class SpellListWrapper
{
    public List<Spell> spells;
}

// Container that contains spells
public class SpellMap : MonoBehaviour
{
    [SerializeField] private Transform iconBarParent;

    private static bool isInitialized = false;
    private void Start()
    {
        if (!isInitialized)
        {
            InitializeSpells();
            isInitialized = true;
        }
    }

    public void BuildIconBar(PartyManagement.CharacterUnit unit)
    {       
        foreach (Transform child in iconBarParent)
            Destroy(child.gameObject);

        // get the spell book from unit
        // get its icon from Spell.iconPath
        // put it in the Spell Icon Bar
        // add the spell to currently selected by the unit.

        if (unit.spellBook == null) return;
        
        GameObject spellIconPrefab = Resources.Load<GameObject>("SpellIcons/SpellIcnPref");
        
        foreach (Spell spell in unit.spellBook.GetAllSpells())
        {
            
            GameObject btn = Instantiate(spellIconPrefab, iconBarParent);
            Image img = btn.GetComponentInChildren<Image>();
            
            Sprite icon = Resources.Load<Sprite>(spell.iconPath);
            if (icon != null)
                img.sprite = icon;
            else
                Debug.Log("Icon not loaded: " + spell.iconPath);
            
            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                // Stop movement - should be extrapolated to whole party ? 
                unit.StopMovement();
                unit.SelectSpell(spell);
                GameManagerMDD.interactionSubstate = InteractionSubstate.Casting;
                Debug.Log("Selected spell: " + spell.name);
            });
        }
    }


    public static void InitializeSpells()
    {
        if (idSpellPairs.Count > 0) return;

        TextAsset jsonText = Resources.Load<TextAsset>("Data/spells");
        if (jsonText == null)
        {
            Debug.LogError("Spell JSON not found at Resources/Data/spells.json");
            return;
        }

        SpellListWrapper wrapper = JsonUtility.FromJson<SpellListWrapper>(jsonText.text);
        if (wrapper == null || wrapper.spells == null)
        {
            Debug.LogError("Failed to parse spell JSON.");
            return;
        }

        foreach (Spell spell in wrapper.spells)
        {
            if (!idSpellPairs.ContainsKey(spell.id))
            {
                idSpellPairs[spell.id] = spell;
                nameIdPairs[spell.name] = spell.id;
            }
        }

        Debug.Log($"Loaded {wrapper.spells.Count} spells from JSON.");
    }

    public static Dictionary<int, Spell> idSpellPairs = new Dictionary<int, Spell>();
    public static Dictionary<string, int> nameIdPairs = new Dictionary<string, int>();
}