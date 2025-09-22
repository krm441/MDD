using UnityEngine;
using PartyManagement;
using System.Collections.Generic;
using static UnityEngine.UI.CanvasScaler;
using System.Threading;
using System.Reflection;

public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject charPrefab;
    [SerializeField] private Transform[] spawnPoints;     // Allow multiple spawn locations
    [SerializeField] private GameObject capsuleBlue;
    [SerializeField] private GameObject capsuleRed;
    [SerializeField] private GameObject capsuleYellow;
    [SerializeField] private GameManagerMDD gameManager;
    [SerializeField] private SpellMap spellMap;
    [SerializeField] private PartyManager partyManager;

    public Sprite magusPortrait;
    public Sprite warriorPortrait;
    public Sprite clericPortrait;

    [SerializeField]
    public PartyPortraitManagerUI portraitManager;

    //public PartyManager partyManager;

    private void Awake()
    {
        // Re-spawn characters already in the party
        RebuildPartyFromData();
    }

    private void Start()
    {
        //SpellMap.InitializeSpells();
        //SpawnPartyTypeOne();
        //SpawnPartyFromData();
    }

    private void RebuildPartyFromData()
    {
        List<CharacterUnit> party = partyManager.partyMembers;

        for (int i = 0; i < party.Count && i < spawnPoints.Length; i++)
        {
            var logicData = party[i];

            GameObject obj = Instantiate(charPrefab, spawnPoints[i].position, Quaternion.identity);
            var unit = obj.GetComponent<CharacterUnit>();

            // Copy data from stored unit into new one
            unit.unitName = logicData.unitName;
            unit.attributeSet.stats = logicData.attributeSet.stats;

            // Replace PartyManager reference with the freshly spawned one
            partyManager.partyMembers[i] = unit;

            Debug.Log($"Re-spawned: {unit.unitName}");
        }
    }

    public void SpawnPartyFromData()
    {
        if (GameSession.playerParty == null)
            GameSession.playerParty = new PlayerPartyData();

        

        // DEBUG:
        if (GameSession.playerParty.party == null || GameSession.playerParty.party.Count == 0)
        {
            var mock_data = CharacterMetaDataLoader.Load("Magus");
            mock_data.isMainCharacter = true;
            PlayerPartyData party = new PlayerPartyData();
            party.party = new System.Collections.Generic.List<CharacterMetaData>();
            party.party.Add(mock_data);
            GameSession.playerParty = party;

        }

        var data = GameSession.playerParty;

        foreach (var unitData in data.party)
        {
            SpawnCharFromData(unitData);
        }

        

        BuildUI();
    }

    private void SpawnCharFromData(CharacterMetaData data)
    {
        // instantiate player party go
        GameObject GO = new GameObject("PlayerPartyRoot");
        PartyPlayer partyPlayer = GO.AddComponent<PartyPlayer>();

        GameObject obj = Instantiate(charPrefab, spawnPoints[0].position, Quaternion.identity);
        obj.transform.SetParent(GO.transform);
        var unit = obj.GetComponent<CharacterUnit>();

        unit.unitName = data.unitName;
        obj.name = data.unitName;

        unit.isMainCharacter = data.isMainCharacter;

        unit.portraitSprite = Resources.Load<Sprite>("Sprites/" + data.portraitPrefabName);

        unit.attributeSet = data.attributeSet;

        foreach(var spellName in data.spells)
        {
            unit.spellBook.AddSpell(spellMap.GetSpellByName(spellName));
        }

        // weapons
        unit.weapon = new Weapon { type = WeaponType.Melee_Slice, power = 10 };

        var visual = Resources.Load<GameObject>("Meshes/" + data.rigMeshName);
        Instantiate(visual, obj.transform);
        visual.transform.localRotation = Quaternion.identity;

        //partyManager.AddMember(unit);
        partyPlayer.partyMembers.Add(unit);
        Console.Log("Spawned and added:", unit.unitName);
    }

    // debug party spawn: war, cleric, magus - for debugging
    public void SpawnPartyTypeOne()
    {
        SpawnMagusDebug();
        SpawnWarriorDebug();
        SpawnClericDebug();

        BuildUI();
    }

    private void BuildUI()
    {
        // build ui
        portraitManager.BuildPortraitBar();
        partyManager.SelectMember(partyManager.GetParty()[0]);
        spellMap.BuildIconBar(partyManager.GetParty()[0], gameManager);
    }

    // Debug spawner
    public void SpawnMagusDebug()
    {
        GameObject obj = Instantiate(charPrefab, spawnPoints[0].position, Quaternion.identity);
        var unit = obj.GetComponent<CharacterUnit>();


        unit.unitName = "Magus";
        obj.name = "Magus";

        // this is main character for now
        unit.isMainCharacter = true;

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

        unit.attributeSet = new AttributeSet();

        unit.attributeSet.stats = new StatBlock
        {
            Intelligence = 17,
            Initiative = 5,
            ActionPoints = 0,
            StartActionPoints = 4,
            Speed = 3,
            HP = 100,
            MaxHP = 100
        };

        unit.attributeSet.resistances = new DamageResistenceContainer();
        //{
        //
        //};

        unit.attributeSet.armorStat = new ArmorStat
        {
            maxMagicArmor = 100,
            magicArmor = 100,
            physicalArmor = 100,
            maxPhysicalArmor = 100,
            moraleLevel = 100,
        };

        unit.spellBook.AddSpell(spellMap.idSpellPairs[0]); // basic magic cast
        unit.spellBook.AddSpell(spellMap.idSpellPairs[3]); // basic arrow cast

        if (capsuleBlue != null)
        {
            GameObject visual = Instantiate(capsuleBlue, obj.transform);
            visual.transform.localRotation = Quaternion.identity;
        }

        partyManager.AddMember(unit);
        Debug.Log("Spawned and added: Magus");
    }
    
    public void SpawnWarriorDebug()
    {
        GameObject obj = Instantiate(charPrefab, spawnPoints[1].position, Quaternion.identity);
        var unit = obj.GetComponent<CharacterUnit>();

               
        unit.unitName = "Warrior";
        obj.name = "Warrior";

        unit.portraitSprite = warriorPortrait;

        unit.attributeSet = new AttributeSet();

        unit.attributeSet.stats = new StatBlock
        {
            Willpower = 17,
            Initiative = 4,
            Speed = 4,
            ActionPoints = 0,
            StartActionPoints = 4,
            HP = 200,
            MaxHP = 200
        };

        unit.attributeSet.armorStat = new ArmorStat
        {
            maxMagicArmor = 100,
            magicArmor = 100,
            physicalArmor = 100,
            maxPhysicalArmor = 100,
            moraleLevel = 100,
        };

        unit.attributeSet.resistances = new DamageResistenceContainer();

        unit.spellBook.AddSpell(spellMap.idSpellPairs[1]); // basic melee cast
        unit.spellBook.AddSpell(spellMap.idSpellPairs[3]); // basic arrow cast

        // init body
        if (capsuleRed != null)
        {
            GameObject visual = Instantiate(capsuleRed, obj.transform);
            //visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }

        partyManager.AddMember(unit);
        Debug.Log("Spawned and added: Warior");
    }

    public void SpawnClericDebug()
    {
        GameObject obj = Instantiate(charPrefab, spawnPoints[2].position, Quaternion.identity);
        var unit = obj.GetComponent<CharacterUnit>();


        unit.unitName = "Cleric";
        obj.name = "Cleric";

        unit.portraitSprite = clericPortrait;

        unit.attributeSet = new AttributeSet();

        unit.attributeSet.stats = new StatBlock
        {
            Devotion = 17,
            Initiative = 3,
            ActionPoints = 0,
            StartActionPoints = 4,
            Speed = 2,
            HP = 150,
            MaxHP = 150
        };

        unit.attributeSet.armorStat = new ArmorStat 
        {
            maxMagicArmor = 100,
            magicArmor = 100,
            physicalArmor = 100,
            maxPhysicalArmor = 100,
            moraleLevel = 100,
        };

        unit.attributeSet.resistances = new DamageResistenceContainer();

        unit.spellBook.AddSpell(spellMap.idSpellPairs[2]); // basic heal streamlet

        if (capsuleYellow != null)
        {
            GameObject visual = Instantiate(capsuleYellow, obj.transform);
            visual.transform.localRotation = Quaternion.identity;
        }

        partyManager.AddMember(unit);
        Debug.Log("Spawned and added: Cleric");
    }
}
