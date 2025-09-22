using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public class SceneManagerMDD : MonoBehaviour
{
    [SerializeField] private DungeonManager DungeonManager;

    [SerializeField] private PartyPlayer PartyManager;

    [SerializeField] private GameObject dungeonManagerPrefab;

    private void Start()
    {
        LoadAll();
    }

    public void LoadAll()
    {
        var dm = Instantiate(dungeonManagerPrefab, transform);
        DungeonManager = dm.GetComponent<DungeonManager>();

        // 1) Load dungeon
        DungeonManager.StartDungeon(GameSession.SelectedDungeon);

        // 2) NavMeshManager
        NavMeshManager.BuildNavMesh();

        // 3) place party at start loacation
        var pos = DungeonManager.GetPlayerStart().worldPos;
        //pos.z = pos.y;
        //pos.y = 0;
        Console.Error("poz", pos);
        PartyManager.LoadParty();
        PartyManager.TeleportParty(pos);
    }
}
