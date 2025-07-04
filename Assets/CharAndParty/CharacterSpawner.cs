using UnityEngine;
using PartyManagement;
using System.Collections.Generic;

public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject charPrefab;
    [SerializeField] private Transform[] spawnPoints;     // Allow multiple spawn locations
    [SerializeField] private GameObject capsuleBlue;
    [SerializeField] private GameObject capsuleRed;
    [SerializeField] private GameObject capsuleYellow;

    public Sprite magusPortrait;
    public Sprite warriorPortrait;
    public Sprite clericPortrait;

    public PartyPortraitManagerUI portraitManager;

    //public PartyManager partyManager;

    private void Awake()
    {
        // Re-spawn characters already in the party
        RebuildPartyFromData();
    }

    private void RebuildPartyFromData()
    {
        List<CharacterUnit> party = PartyManager.partyMembers;

        for (int i = 0; i < party.Count && i < spawnPoints.Length; i++)
        {
            var logicData = party[i];

            GameObject obj = Instantiate(charPrefab, spawnPoints[i].position, Quaternion.identity);
            var unit = obj.GetComponent<CharacterUnit>();

            // Copy data from stored unit into new one
            unit.unitName = logicData.unitName;
            unit.stats = logicData.stats;

            // Replace PartyManager reference with the freshly spawned one
            PartyManager.partyMembers[i] = unit;

            Debug.Log($"Re-spawned: {unit.unitName}");
        }
    }

    // debug party spawn: war, cleric, magus - for debugging
    public void SpawnPartyTypeOne()
    {
        SpawnMagusDebug();
        SpawnWarriorDebug();
        SpawnClericDebug();

        // build ui
        portraitManager.BuildPortraitBar();
    }

    // Debug spawner
    public void SpawnMagusDebug()
    {
        GameObject obj = Instantiate(charPrefab, spawnPoints[0].position, Quaternion.identity);
        var unit = obj.GetComponent<CharacterUnit>();

        unit.unitName = "Magus";

        unit.portraitSprite = magusPortrait; 

        /* AS a REMINDER:
        public class StatBlock
        {
            public int Intelligence;
            public int Willpower;
            public int Devotion;
            public int Initiative;
            public int HP;
            public int MaxHP;
        }*/

        unit.stats = new StatBlock
        {
            Intelligence = 17,
            Initiative = 5,
            HP = 100,
            MaxHP = 100
        };

        unit.spellBook.AddSpell(SpellMap.idSpellPairs[0]); // add fireball

        if (capsuleBlue != null)
        {
            GameObject visual = Instantiate(capsuleBlue, obj.transform);
            visual.transform.localRotation = Quaternion.identity;
        }

        PartyManager.AddMember(unit);
        Debug.Log("Spawned and added: Magus");
    }
    
    public void SpawnWarriorDebug()
    {
        GameObject obj = Instantiate(charPrefab, spawnPoints[1].position, Quaternion.identity);
        var unit = obj.GetComponent<CharacterUnit>();

        // somehow add the capsule here

        unit.unitName = "Warrior";

        unit.portraitSprite = warriorPortrait;

         unit.stats = new StatBlock
        {
            Willpower = 17,
            Initiative = 4,
            HP = 200,
            MaxHP = 200
        };

        
        // init body
        if (capsuleRed != null)
        {
            GameObject visual = Instantiate(capsuleRed, obj.transform);
            //visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }

        PartyManager.AddMember(unit);
        Debug.Log("Spawned and added: Magus");
    }

    public void SpawnClericDebug()
    {
        GameObject obj = Instantiate(charPrefab, spawnPoints[2].position, Quaternion.identity);
        var unit = obj.GetComponent<CharacterUnit>();

        unit.unitName = "Cleric";

        unit.portraitSprite = clericPortrait;

        unit.stats = new StatBlock
        {
            Devotion = 17,
            Initiative = 3,
            HP = 150,
            MaxHP = 150
        };

        if (capsuleYellow != null)
        {
            GameObject visual = Instantiate(capsuleYellow, obj.transform);
            visual.transform.localRotation = Quaternion.identity;
        }

        PartyManager.AddMember(unit);
        Debug.Log("Spawned and added: Magus");
    }
}
