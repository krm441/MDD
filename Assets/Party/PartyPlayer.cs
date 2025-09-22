using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;

public class PartyPlayer : IParty
{
    [SerializeField] private GameObject charPrefab;
    [SerializeField] private List<Transform> spawnPoints;
    [SerializeField] private SpellMap spellMap;
    private GameManagerMDD gameManager;

    [Header("Characters Prefabs")]
    [SerializeField] private GameObject magusPrefab;
    [SerializeField] private GameObject barbarianPrefab;
    [SerializeField] private GameObject druidPrefab;
    [SerializeField] private GameObject rangerPrefab;

    [Header("Load Order Controll")]
    public bool LoadOnStart = true;

    // for serialisation (and checkpoints)
    private PlayerPartyData playerPartyCopy;
    public override PlayerPartyData GetCurrentCapture() // for save/load captured state
    {
        var ret = new PlayerPartyData();
        List<CharacterMetaData> meta = new List<CharacterMetaData>();
        foreach(CharacterUnit unit in partyMembers)
        {
            meta.Add(unit.CaptureState());
        }
        ret.version = playerPartyCopy.version;
        ret.party = meta;
        ret.profileName = playerPartyCopy.profileName;

        return ret;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!LoadOnStart) return;
        LoadParty();
    }

    public override void AddMember(CharacterUnit unit)
    {
        base.AddMember(unit);

        LoadByName(unit);

        BuildUI();

        PushCharToReg(unit);
    }

    public void LoadParty()
    {
        // party type
        partyType = PartyTypes.Player;

        // spell map init
        //spellMap = FindObjectOfType<SpellMap>();
        Assert.IsNotNull(spellMap);
        gameManager = FindObjectOfType<GameManagerMDD>();
        Assert.IsNotNull(gameManager);

        spellMap.InitializeSpells(); // NOTE:: lazy initialisation

        if (GameSession.playerParty == null)
            GameSession.playerParty = new PlayerPartyData();

        if (TutorialScenePersistentData.isUsed)
        {

            return;
        }

        // DEBUG:
        if (GameSession.playerParty.party == null || GameSession.playerParty.party.Count == 0)
        {
            var mock_data = CharacterMetaDataLoader.Load("Ranger");
            mock_data.isMainCharacter = true;
            PlayerPartyData party = new PlayerPartyData();
            party.party = new System.Collections.Generic.List<CharacterMetaData>();
            party.party.Add(mock_data);
            GameSession.playerParty = party;

        }

        var data = GameSession.playerParty;

        LoadFromData(data);


        // capture first check point
        CheckPointLoader.SaveCheckPoint(data, transform);
    }

    void LoadByName(CharacterUnit unit)
    {
        var data = CharacterMetaDataLoader.Load(unit.unitName);

        unit.unitName = data.unitName;

        unit.isMainCharacter = data.isMainCharacter;

        //unit.portraitSprite = Resources.Load<Sprite>("Sprites/" + data.portraitPrefabName);
        //unit.portraitSprite.name = data.portraitPrefabName;

        unit.attributeSet = data.attributeSet;

        foreach (var spellName in data.spells)
        {
            unit.spellBook.AddSpell(spellMap.GetSpellByName(spellName));
        }

        GameSession.playerParty.party.Add(data);
    }

    public void LoadFromData(PlayerPartyData data)
    {
        spellMap.InitializeSpells();

        foreach (var unit in partyMembers)
        {
            Destroy(unit.gameObject);
        }

        partyMembers.Clear();

        foreach (var unitData in data.party)
        {
            SpawnFromData(unitData);
        }

        BuildUI();

        // push to registry
        foreach (var unit in partyMembers)
        {
            PushCharToReg(unit);
        }

        playerPartyCopy = data;
    }

    public void SpawnFromData(CharacterMetaData data)
    {
        GameObject obj = null;

        if(data.unitName == "Magus")
            obj = Instantiate(magusPrefab, spawnPoints[0].position, Quaternion.identity);
        else if (data.unitName == "Barbarian")
            obj = Instantiate(barbarianPrefab, spawnPoints[0].position, Quaternion.identity);
        else if (data.unitName == "Cleric")
            obj = Instantiate(druidPrefab, spawnPoints[0].position, Quaternion.identity);
        else if (data.unitName == "Ranger")
            obj = Instantiate(rangerPrefab, spawnPoints[0].position, Quaternion.identity);

        obj.transform.rotation = spawnPoints[0].rotation;
        var unit = obj.GetComponent<CharacterUnit>();

        unit.unitName = data.unitName;
        obj.name = data.unitName;

        unit.isMainCharacter = data.isMainCharacter;

        //unit.portraitSprite = Resources.Load<Sprite>("Sprites/" + data.portraitPrefabName);
        //unit.portraitSprite.name = data.portraitPrefabName;

        unit.attributeSet = data.attributeSet;

        foreach (var spellName in data.spells)
        {
            unit.spellBook.AddSpell(spellMap.GetSpellByName(spellName));
        }

        // weapons
        //unit.weapon = new Weapon { type = WeaponType.Melee_Slice, power = 10, apCost = 2 };

        //var visual = Resources.Load<GameObject>("Meshes/" + data.rigMeshName);
        unit.rigMeshName = data.rigMeshName;
        //Console.Error(this, this.GetInstanceID(), "Meshes/" + data.rigMeshName);
        //Assert.IsNotNull(visual);
        //var go = Instantiate(visual, obj.transform);
        //go.transform.rotation = obj.transform.rotation;
        //visual.transform.localRotation = Quaternion.identity;

        partyMembers.Add(unit);
        Console.Log("Spawned and added:", unit.unitName);
    }

    private void BuildUI()
    {
        FindObjectOfType<PartyPortraitManagerUI>().BuildPortraitBar(partyMembers);
        CurrentSelected = partyMembers[0];
        spellMap.BuildIconBar(CurrentSelected, gameManager);
        FindObjectOfType<FooterController>().Setup(CurrentSelected);
    }

    // MOVEMENT //
    
    public void TeleportParty(Transform destination, float ringRadius = 2.5f)
    {
        TeleportParty(destination.position, ringRadius);
    }

    public void TeleportParty(Vector3 destination, float ringRadius = 2.5f)
    {
        if (destination == null)
        {
            Debug.LogWarning("PartyManager::TeleportParty: destination is null.");
            return;
        }

        if (partyMembers == null || partyMembers.Count == 0)
        {
            Debug.LogWarning("PartyManager::TeleportParty: party is empty.");
            return;
        }

        // 0) stop any current movement to avoid fighting the warp
        StopAllMovement();

        // 1) choose a leader (prefer currently selected, then main, then first)
        CharacterUnit leader = CurrentSelected;
        if (leader == null)
            leader = partyMembers.Find(m => m.isMainCharacter) ?? partyMembers[0];

        // 2) place leader at the exact target
        PlaceOnNavMesh(leader, destination);

        // 3) place others around a ring
        int placed = 0;
        for (int i = 0; i < partyMembers.Count; i++)
        {
            var member = partyMembers[i];
            if (member == leader) continue;

            float t = (placed + 1) / (float)(partyMembers.Count); // spread 0..1
            float angle = t * 360f;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * ringRadius);
            Vector3 candidate = destination + offset;

            PlaceOnNavMesh(member, candidate);
            placed++;
        }
    }
    private void PlaceOnNavMesh(CharacterUnit unit, Vector3 pos)
    {
        if (unit == null) return;

        // uncarve
        var obstacle = unit.GetComponent<NavMeshObstacle>();
        if (obstacle) obstacle.enabled = false;

        var agent = unit.agent;
        if (agent != null)
        {
            if (!agent.enabled) agent.enabled = true;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);  // safe teleport on NavMesh
                agent.ResetPath();
            }
            else
            {
                // No mesh nearby; hard set as a fallback
                unit.transform.position = pos;
            }
        }
        else
        {
            unit.transform.position = pos;
        }
    }
    // APs //
    public void ResetAllActionPoints()
    {
        foreach (CharacterUnit member in partyMembers)
        {
            member.SetActionPoints(0);
        }
    }
    public void SetStartActionPoints()
    {
        if (partyMembers.Count > 0)
        {
            foreach (var member in partyMembers)
            {
                member.attributeSet.stats.ActionPoints = member.attributeSet.stats.StartActionPoints;
            }
        }
    }

    // Selection // 
    public void SelectMember(CharacterUnit member)
    {
        int index = partyMembers.IndexOf(member);

        if (index >= 0)
        {
            CurrentSelected = member;
            Debug.Log($"PartyManager::Selected member: {member.unitName} at index {index}");
        }
        else
        {
            Debug.LogWarning($"PartyManager:: error: {member.unitName} is not in the party.");
        }

        Assert.IsNotNull(spellMap);

        spellMap.BuildIconBar(member, gameManager);

        // footer setup
        UnityEngine.Object.FindObjectOfType<FooterController>().Setup(member);

    }
    public void SetMainAsSelected()
    {
        foreach (CharacterUnit unit in partyMembers)
        {
            if (unit.isMainCharacter)
            {
                SelectMember(unit);
                break;
            }
        }
    }

    // Movement //
    private bool IsPartyInFormation(float maxDistance = 4f)
    {
        var party = partyMembers;//  partyManager.GetParty();

        for (int i = 0; i < party.Count; i++)
        {
            for (int j = i + 1; j < party.Count; j++)
            {
                float distance = Vector3.Distance(party[i].transform.position, party[j].transform.position);
                if (distance > maxDistance)
                    return false;
            }
        }

        return true;
    }
    public void GetIntoFormation()
    {
        if (IsPartyInFormation()) // early return
        {
            return;
        }

        List<CharacterUnit> party = partyMembers;
        var leader = CurrentSelected;

        Vector3 leaderForward = leader.transform.forward;
        Vector3 leaderTarget = leader.transform.position;
        Vector3 sharedLookTarget = leaderTarget + leaderForward * 13f;



        List<Vector3> formationTargets = GetFormationTargets(leaderTarget, leaderForward);

        int followerIndex = 0;
        foreach (var follower in party)
        {
            if (follower == leader) continue;
            if (followerIndex >= formationTargets.Count) break;

            Vector3 targetPos = formationTargets[followerIndex++];
            //follower.agent.isStopped = false;
            //follower.agent.SetDestination(targetPos);
            follower.WalkTo(targetPos);
        }
    }
    private List<Vector3> GetFormationTargets(Vector3 leaderTarget, Vector3 leaderForward)
    {
        List<Vector3> formationTargets = new List<Vector3>();
        Vector3 right = Vector3.Cross(Vector3.up, leaderForward).normalized;

        float spacing = 2f; // distance between characters

        // Rhombus formation:

        //     Leader
        // F1    F3    F2
        //       F4


        // F1: back-left
        formationTargets.Add(leaderTarget - leaderForward * spacing + right * -spacing);
        // F2: back-right
        formationTargets.Add(leaderTarget - leaderForward * spacing + right * spacing);
        // F3: directly behind
        formationTargets.Add(leaderTarget - leaderForward * spacing * 1.5f);
        // F4: double back
        formationTargets.Add(leaderTarget - leaderForward * spacing * 2f);

        return formationTargets;
    }
}
