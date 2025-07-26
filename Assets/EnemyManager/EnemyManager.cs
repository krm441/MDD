using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public enum Hostility
{
    Friendly,
    Hostile,
    NotHostile
}

[System.Serializable]
public class NPCDefinition
{
    public string id;
    public string displayName;
    public string prefabPath;
    public string visualPath;
    public string portraitPath;
    public Hostility hostility;
    public ArmorStat armorStat = new ArmorStat();
    public StatBlock statBlock = new StatBlock();
    public MeshAndPortraits meshAndPortraits = new MeshAndPortraits();
    public DamageResistenceContainer damageResistenceContainer = new DamageResistenceContainer();
}

public class EnemyManager : MonoBehaviour
{
    private bool isInitiated = false;

    [SerializeField] private PartyManager partyManager;
    [SerializeField] public AiManager aiManager;

    /// <summary>
    /// Gloval game-wide signal. Static for simplicity
    /// </summary>
    public static event Action OnAllEnemiesDefeated;
    // Start is called before the first frame update
    void Start()
    {
        EnemyDatabase.Load();
    }

    public void AllEnemiesDefeated()
    {
        OnAllEnemiesDefeated?.Invoke();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitiated) return;
      // //foreach (var party in allParties)
      // //{
      // //    party.Update(); // checks for visibilityv
      // //    if(party.State == EnemyParty.PartyState.Destroyed)
      // //    {
      // //
      // //    }
      // //}
      //  for(int i = allParties.Count - 1; i >= 0; i--)
      //  {
      //      var party = allParties[i];
      //      party.Update(); // checks for visibilityv
      //      if (party.State == EnemyParty.PartyState.Destroyed)
      //      {
      //          allParties.RemoveAt(i);
      //      }
      //  }
      //  //if(allParties.Count == 0)
      //  //{
      //  //    StartCoroutine(ExitCombatAfterDelay());
      //  //    isInitiated = false;
      //  //}
        if (allParties.Count == 0)
        {
            OnAllEnemiesDefeated?.Invoke();
            isInitiated = false;
        }
    }

    public void RegisterParty(EnemyParty party)
    {
        if (!allParties.Contains(party))
        {
            allParties.Add(party);
            isInitiated = true;
        }
    }

    public static List<CharacterUnit> GetEnemies()
    {
        List<CharacterUnit> ret = new List<CharacterUnit>();
        foreach (var party in allParties)
            ret.AddRange(party.enemiesInPack);
        return ret;
    }

    public void Clear()
    {
        allParties.Clear();
        isInitiated = false;
    }

    // parties that are triggered - and entered the combat state
    private static List<EnemyParty> allParties = new List<EnemyParty>();

    ///////////////////////////////////////////////////////////////////////// 
    ///spawner///

    

    public void SpawnDebugPack(Vector3 center, float areaRadius, string enemyType = "NpcSimple", string partyName = "DebugParty")
    {
        if (!EnemyDatabase.NPCs.TryGetValue(enemyType, out var npcDef))
        {
            Console.Error($"Enemy type '{enemyType}' not found in EnemyDatabase.");
            return;
        }

        //GameObject prefab = Resources.Load<GameObject>(npcDef.prefabPath);
        GameObject prefab = Resources.Load<GameObject>("Prefabs/CharPrefab");
        GameObject visual = Resources.Load<GameObject>("Visuals/" + npcDef.visualPath);

        if (prefab == null || visual == null)
        {
            Console.Error("Could not load prefab or visual for enemy type", enemyType);
            return;
        }

        // Root holder for all enemy parties
        GameObject root = GameObject.Find("EnemyParties") ?? new GameObject("EnemyParties");

        // Create a holder for this specific pack
        GameObject partyHolder = new GameObject(partyName);
        partyHolder.transform.parent = root.transform;
        //var enemyParty = partyHolder.AddComponent<EnemyParty>();
        
        

        // Determine how many enemies based on area size
        float spacing = 2.5f;
        int maxEnemies = Mathf.FloorToInt(Mathf.PI * areaRadius * areaRadius / (spacing * spacing));
        int count = Mathf.Clamp(maxEnemies, 1, 12);

        List<Vector3> spawnPoints = GenerateCirclePositions(center, areaRadius, count);
        List<CharacterUnit> activeEnemies = new List<CharacterUnit>();

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = spawnPoints[i];

            GameObject obj = GameObject.Instantiate(prefab, pos, Quaternion.identity, partyHolder.transform);

            // Set the layer to HostileNPCs
            obj.layer = LayerMask.NameToLayer("HostileNPCs");

            // Set all children recursively - since the model will be made from many meshes
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("HostileNPCs");
            }

            CharacterUnit unit = obj.GetComponent<CharacterUnit>();
            if (unit == null)
            {
                Debug.LogError("CharPrefab missing CharacterUnit script.");
                continue;
            }

            // Assign properties from JSON
            unit.unitName = $"{npcDef.displayName}_{i}";
            unit.isPlayerControlled = false;
            unit.attributeSet.stats = new StatBlock(npcDef.statBlock);
            unit.attributeSet.armorStat = new ArmorStat(npcDef.armorStat);
            unit.attributeSet.resistances = new DamageResistenceContainer(npcDef.damageResistenceContainer);

            // register BTs
            aiManager.SetupAI_BT(unit);

            // spell
            unit.spellBook.AddSpell(SpellMap.idSpellPairs[3]); // basic piercing arrow

            // portrait
            Sprite portrait = Resources.Load<Sprite>(npcDef.portraitPath);
            if (portrait == null)
                Debug.LogWarning($"Portrait not found for {npcDef.portraitPath}");
            else
                unit.portraitSprite = portrait;

            GameObject vis = GameObject.Instantiate(visual, obj.transform);
            vis.transform.localRotation = Quaternion.identity;

            activeEnemies.Add(unit);
            Console.Log($"Spawned enemy {unit.unitName}");
        }

        // register in EnemyManager
        //RegisterParty(new EnemyParty(activeEnemies, this, partyManager));
        EnemyParty enemyParty = partyHolder.AddComponent<EnemyParty>();
        enemyParty.gameManager = FindObjectOfType<GameManagerMDD>();
        enemyParty.Init(activeEnemies, this, partyManager);

        //enemyParty.Init();
        // enemyParty.gameManager = FindObjectOfType<GameManagerMDD>();

    }
    private static List<Vector3> GenerateCirclePositions(Vector3 center, float radius, int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            positions.Add(new Vector3(center.x + x, center.y, center.z + z));
        }

        return positions;
    }

}

public static class EnemyDatabase
{
    public static Dictionary<string, NPCDefinition> NPCs = new Dictionary<string, NPCDefinition>();

    public static void Load()
    {
        if (NPCs.Count > 0) return;

        TextAsset json = Resources.Load<TextAsset>("Data/NPC");
        if (json == null)
        {
            Debug.LogError("Could not load NPC.json from Resources/Data/");
            return;
        }

        var enemyList = JsonUtility.FromJson<Wrapper>(json.text);
        foreach (var e in enemyList.list)
            NPCs[e.id] = e;

        //Debug.Log($"Loaded {NPCs.Count} enemy definitions.");
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<NPCDefinition> list;
    }
}
