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
}

public class EnemyManager : MonoBehaviour
{
    private static bool isInitiated = false;
    // Start is called before the first frame update
    void Start()
    {
        EnemyDatabase.Load();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitiated) return;
       //foreach (var party in allParties)
       //{
       //    party.Update(); // checks for visibilityv
       //    if(party.State == EnemyParty.PartyState.Destroyed)
       //    {
       //
       //    }
       //}
        for(int i = allParties.Count - 1; i >= 0; i--)
        {
            var party = allParties[i];
            party.Update(); // checks for visibilityv
            if (party.State == EnemyParty.PartyState.Destroyed)
            {
                allParties.RemoveAt(i);
            }
        }
        if(allParties.Count == 0)
        {
            GameManagerMDD.ExitCombat();
            isInitiated = false;
        }
    }

    public static void RegisterParty(EnemyParty party)
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

    public static void Clear()
    {
        allParties.Clear();
        isInitiated = false;
    }

    // parties that are triggered - and entered the combat state
    private static List<EnemyParty> allParties = new List<EnemyParty>();
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

        Debug.Log($"Loaded {NPCs.Count} enemy definitions.");
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<NPCDefinition> list;
    }
}
