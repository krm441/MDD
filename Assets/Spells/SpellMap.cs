using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using EventSystemMDD;
using UnityEngine.Assertions;
using System;

public class SpellBook
{
    public void AddSpell(Spell spell)
    {
        if (!spells.Contains(spell))
            spells.Add(spell);
        else
            Debug.Log(spell.name + " Not found");
    }

    public Spell GetSpell(string spellName)
    {
        if (string.IsNullOrWhiteSpace(spellName))
            return null;

        for (int i = 0; i < spells.Count; i++)
        {
            var s = spells[i];
            if (s != null &&
                !string.IsNullOrEmpty(s.name) &&
                string.Equals(s.name, spellName, StringComparison.OrdinalIgnoreCase))
            {
                return s;
            }
        }

        Debug.LogWarning($"Spell '{spellName}' not found.");
        return null;
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

public enum SpellPhysicsType
{
    Static,
    Linear,
    Parabolic, // arc like
    Chain
}

public enum SpellDPSType
{
    AOE,
    DOT,
    Melee,
    CC,
}

[System.Serializable]
public class DamageResistenceContainer
{
    public DamageResistenceContainer() { }
    public DamageResistenceContainer(DamageResistenceContainer other)
    {
        Slashing = other.Slashing;
        Piercing = other.Piercing;
        Crushing = other.Crushing;

        Fire = other.Fire;
        Water = other.Water;
        Wind = other.Wind;
        Earth = other.Earth;

        Light = other.Light;
        Shadow = other.Shadow;

        Necrotic = other.Necrotic;
        Poison = other.Poison;
        Demonic = other.Demonic;

        Healing = other.Healing;
        MentalFortification = other.MentalFortification;
        MagicFortification = other.MagicFortification;
    }

    // Physical
    public int Slashing = 0;
    public int Piercing = 0;
    public int Crushing = 0;
    public int TotalPhysical => Slashing + Piercing + Crushing;

    // Elemental
    public int Fire = 0;
    public int Water = 0;
    public int Wind = 0;
    public int Earth = 0;
    public int TotalElemental => Fire + Water + Wind + Earth;

    // Spiritual
    public int Light = 0;
    public int Shadow = 0;
    public int TotalSpiritual => Light + Shadow;

    // Heretique
    public int Necrotic = 0;
    public int Poison = 0;
    public int Demonic = 0;
    public int TotalHeretique => Necrotic + Poison + Demonic;

    // Healing
    public int Healing = 0;
    public int MentalFortification = 0;
    public int MagicFortification = 0;

    public DamageResistenceContainer Clone() => (DamageResistenceContainer)this.MemberwiseClone();
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
    public SpellPhysicsType physicsType;
    public SpellDPSType dPSType;
    public DamageResistenceContainer baseDamage = new DamageResistenceContainer();
    public int apCost;
    public int manaCost;
    public int range;
    public int radius;
    public string vfxType;
    public string sfxOnStart;
    public string sfxOnFly;
    public string sfxOnImpact;
    public bool hidden;
    public bool friendlyFire;
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
    //[SerializeField] 
    //private Transform iconBarParent;

    [SerializeField] private List<SpellIconMDD> spellIcons = new List<SpellIconMDD>();

    [SerializeField] private GameObject iconBarParent;

    //private static bool isInitialized = false;
    private bool isInitialized = false;
    private void Start()
    {
        Console.Log("init");

        Assert.IsNotNull(iconBarParent, "HIT");

        if (!isInitialized)
        {
            InitializeSpells();
            isInitialized = true;
        }

        Assert.IsNotNull(iconBarParent);
    }

    public SpellIconMDD FindIcon(string iconName)
    {
        return spellIcons.FirstOrDefault(i =>
            i != null && i.name.Equals(iconName, StringComparison.OrdinalIgnoreCase));
    }

    public void HideIconBar()
    {
        foreach (Transform child in iconBarParent.transform)
            Destroy(child.gameObject);
    }


    public void BuildIconBar(PartyManagement.CharacterUnit unit, GameManagerMDD gameManager)
    {        
         Assert.IsNotNull(iconBarParent);
        

        foreach (Transform child in iconBarParent.transform)
            Destroy(child.gameObject);

        // get the spell book from unit
        // get its icon from Spell.iconPath
        // put it in the Spell Icon Bar
        // add the spell to currently selected by the unit.

        if (unit.spellBook == null) return;
        
        GameObject spellIconPrefab = Resources.Load<GameObject>("SpellIcons/SpellIcnPref");
        
        foreach (Spell spell in unit.spellBook.GetAllSpells())
        {
            if(spell.hidden) continue; // no need for icon

            // 1) find prefab and instantiate
            spellIconPrefab = FindIcon(spell.iconPath).gameObject;

            GameObject btn = Instantiate(spellIconPrefab, iconBarParent.transform);
            var icon = btn.GetComponent<SpellIconMDD>();
            icon.toolTipText = spell.description;

            // 2) listeners - includes tooltip
            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                EventSystemMDD.ButtonEvent buttonEvent = new EventSystemMDD.ButtonEvent
                {
                    eventType = EventSystemMDD.EventType.SpellClick,
                    spell = spell,
                    targetUnit = unit,
                };

                EventSystemMDD.EventSystemMDD.Raise(buttonEvent);
                
            });

            // tool tip:
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            // initialize = since it returns null
            if (trigger == null)
                trigger = btn.AddComponent<EventTrigger>();
            // PointerEnter: Show tooltip
            EventTrigger.Entry entryEnter = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            entryEnter.callback.AddListener((eventData) =>
            {
                //UITooltip.Instance.Show(spell.description, Input.mousePosition);
                //Debug.LogError("entry");
                Assert.IsNotNull(icon);
                icon.ShowToolTip();
            });
            trigger.triggers.Add(entryEnter);

            // PointerExit: Hide tooltip
            EventTrigger.Entry entryExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            entryExit.callback.AddListener((eventData) =>
            {
                //UITooltip.Instance.Hide();
                //Debug.LogError("Exit");
                icon.HideToolTip();
            });
            trigger.triggers.Add(entryExit);
        }
    }

    public Spell GetSpellByName(string name)
    {
        return idSpellPairs[nameIdPairs[name]];
    }

    public void InitializeSpells()
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

    public Dictionary<int, Spell> idSpellPairs = new Dictionary<int, Spell>();
    public Dictionary<string, int> nameIdPairs = new Dictionary<string, int>();
}