using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerMDD : MonoBehaviour
{
    [SerializeField] private DungeonManager DungeonManager;

    [SerializeField] private PartyPlayer PartyManager;

    [SerializeField] private GameObject dungeonManagerPrefab;

    [SerializeField] private GameObject globalLight;

    [SerializeField] private IsometricCameraController controller;

    private void Start()
    {
        
    }

    public void LoadAll()
    {
        var dm = Instantiate(dungeonManagerPrefab, transform);
        DungeonManager = dm.GetComponent<DungeonManager>();

        // 1) Load dungeon
        DungeonManager.StartDungeon(GameSession.SelectedDungeon);

        if (GameSession.SelectedDungeon == DungeonType.BSP)
            globalLight.gameObject.SetActive(false);
        else
            globalLight.gameObject.SetActive(true);

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

    bool snaped = false;
    private void LateUpdate()
    {
        if (snaped) return;

        LoadAll();

        var pos = DungeonManager.GetPlayerStart().worldPos;
        controller.LerpToCharacter(pos);

        snaped = true;
    }

    public void GoHome()
    {
        SceneManager.LoadScene("RespawnArea");
    }
}
