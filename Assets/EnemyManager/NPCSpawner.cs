using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnLocations;
    [SerializeField] private EnemyManager enemyManager;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayedSpawn());
    }

    private IEnumerator DelayedSpawn()
    {
        yield return new WaitUntil(() => enemyManager != null); // wait till enemy manager initializes

        if (enemyManager != null)
            enemyManager.SpawnDebugPack(spawnLocations[0].position, 3);
        else
            Console.Error("NPCSpawner::DelayedSpawn: fail");
    }
}
