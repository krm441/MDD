using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

/// <summary>
/// Represents a possible pack of enemies
/// </summary>
public class EnemyParty// : MonoBehaviour 
{
    public List<CharacterUnit> enemiesInPack = new List<CharacterUnit>();

    public EnemyParty(List<CharacterUnit> members)
    {
        enemiesInPack = members;
        EnemyManager.RegisterParty(this);
    }

    public enum PartyState
    {
        Idle,
        Combat
    }

    public PartyState State = PartyState.Idle;

    public void Update()
    {
        if (State == PartyState.Idle)
            foreach (var enemy in enemiesInPack)
            {
                if (IsPlayerVisible(enemy))
                {
                    Console.Log("Combat triggered by " + enemy.unitName);
                    GameManagerMDD.EnterCombat();
                    State = PartyState.Combat;
                    break;
                }
            }
        else if (State == PartyState.Combat)
            // this is where party ai should work
            return;
    }

    private bool IsPlayerVisible(CharacterUnit enemy)
    {
        foreach (var player in PartyManager.GetParty())
        {
            float dist = Vector3.Distance(enemy.transform.position, player.transform.position);
            if (dist > enemy.LignOfSight) continue;

            Vector3 dir = (player.transform.position - enemy.transform.position).normalized;
            if (Physics.Raycast(enemy.transform.position + Vector3.up * 1.5f, dir, out RaycastHit hit, dist))
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PartyLayer"))
                {
                    return true; // LOS confirmed
                }
            }
        }

        return false;
    }

}

public enum EnemyPackType
{
    SimpleEnemies,
}

/// <summary>
/// Used in UI or by other scripts to summon enemy pack at certain location
/// </summary>
public class EnemyPackSummoner : MonoBehaviour
{
    public static List<CharacterUnit> activeEnemies = new List<CharacterUnit>();

    public static void SpawnDebugPack(Vector3 center, float areaRadius, string enemyType = "NpcSimple", string partyName = "DebugParty")
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

        // Determine how many enemies based on area size
        float spacing = 2.5f;
        int maxEnemies = Mathf.FloorToInt(Mathf.PI * areaRadius * areaRadius / (spacing * spacing));
        int count = Mathf.Clamp(maxEnemies, 1, 12);

        List<Vector3> spawnPoints = GenerateCirclePositions(center, areaRadius, count);

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
            unit.stats = npcDef.statBlock;
            unit.armorStat = npcDef.armorStat;

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
        EnemyManager.RegisterParty(new EnemyParty(activeEnemies));

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