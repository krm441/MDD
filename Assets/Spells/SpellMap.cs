using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

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

public enum SpellPhysicsType
{
    Static,
    Linear,
    Parabolic, // arc like
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
    public DamageResistenceContainer baseDamage;
    public int apCost;
    public int manaCost;
    public int range;
    public int radius;
    public string vfxType;
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
    [SerializeField] private static Transform iconBarParent;

    private static bool isInitialized = false;
    private void Start()
    {
        if (!isInitialized)
        {
            InitializeSpells();
            isInitialized = true;
        }

        // Load prefab once
        if (iconBarParent == null)
        {
            //GameObject parentObj = GameObject.Find("PartyPortraitParent");
            GameObject parentObj = GameObject.Find("HorizontalLayout");
            if (parentObj == null)
            {
                Console.Error("Could not find 'HorizontalLayout' in the scene!");
                return;
            }
            iconBarParent = parentObj.transform;
        }
    }

    public void BuildIconBar(PartyManagement.CharacterUnit unit, GameManagerMDD gameManager)
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
                unit.StopMovement();
                unit.SelectSpell(spell);

                gameManager.GetCurrentState().SetCastingSubState();
                //GameManagerMDD.interactionSubstate = InteractionSubstate.Casting;
                Debug.Log("Selected spell: " + spell.name);
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
                UITooltip.Instance.Show(spell.description, Input.mousePosition);
                Debug.Log("entry");
            });
            trigger.triggers.Add(entryEnter);

            // PointerExit: Hide tooltip
            EventTrigger.Entry entryExit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            entryExit.callback.AddListener((eventData) =>
            {
                UITooltip.Instance.Hide();
                Debug.Log("Exit");
            });
            trigger.triggers.Add(entryExit);
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