using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine;

public class SpellBook
{
    public void AddSpell(Spell spell)
    {
        if(!spells.Contains(spell))
            spells.Add(spell);
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
    //public Animation??
    public int manaCost;
    public int range;
    public int radius;

    public void PlayAnimation()
    {
        // todo - animation play fsm
    }
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

            
            Button button = btn.GetComponent<Button>();
            button.onClick.AddListener(() =>
            {
                unit.SelectSpell(spell);
                GameManagerMDD.interactionSubstate = InteractionSubstate.Casting;
                Debug.Log("Selected spell: " + spell.name);
            });
        }
    }


    public static void InitializeSpells()
    {
        // add to map based on id
        idSpellPairs.Add(0, new Spell
        {
            id = 0,
            name = "FireBall",
            description = "Summon and hurl a huge fireball with area of effect",
            iconPath = "SpellIcons/FireballIcn",
            shotcutKey = "f",
        });

        // add to map based on name
        nameIdPairs.Add(idSpellPairs[0].name, idSpellPairs[0].id);
    }
    public static Dictionary<int, Spell> idSpellPairs = new Dictionary<int, Spell>();
    public static Dictionary<string, int> nameIdPairs = new Dictionary<string, int>();
}