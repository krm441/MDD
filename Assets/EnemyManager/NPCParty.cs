using System.Collections.Generic;
using PartyManagement;
using UnityEngine;



/// <summary>
/// Represents a possible pack of enemies
/// </summary>
public class NPCParty : MonoBehaviour 
{
    public NpcPartyType type = NpcPartyType.Hostile;

    public bool isInCombat = false;

    public List<CharacterUnit> npcsInPack = new List<CharacterUnit>();

    public GameManagerMDD gameManager;

    [SerializeField] private AiMdd.AiManager aiManager;

    private EnemyManager enemyPartyManager;
    private PartyManager playerPartyManager;

    //public EnemyParty(List<CharacterUnit> members, EnemyManager manager, PartyManager playerPartyManager)
    //{
    //    enemiesInPack = members;
    //    manager.RegisterParty(this);
    //    enemyPartyManager = manager;
    //    this.playerPartyManager = playerPartyManager;
    //}

    public void Init(List<CharacterUnit> members, EnemyManager manager, PartyManager playerPartyManager)
    {
        npcsInPack = members;
        enemyPartyManager = manager;
        this.playerPartyManager = playerPartyManager;
        this.aiManager = manager.aiManager;

        foreach (CharacterUnit member in members) member.parentParty = this;

        manager.RegisterParty(this);
    }

    

    public PartyState State = PartyState.Idle;

    private void Update()
    {
        if (State == PartyState.Idle)
        {
           //if(gameManager.IsCombat())
           //{
           //    State = PartyState.Combat;
           //    return;
           //}

            foreach (var enemy in npcsInPack)
                aiManager?.TickAI(enemy);
        }
        // return;
        //foreach (var enemy in enemiesInPack)
        //{
        //    aiManager?.TickAI(enemy);
        //    if (IsPlayerVisible(enemy))
        //    {
        //        Console.Log("Combat triggered by " + enemy.unitName);
        //        foreach (var unit in enemiesInPack) // unelegant solution - need to move coroutines to game manager/ or better a coroutine manager singleton
        //        {
        //            unit.StopMovement();
        //        }
        //        gameManager.StopAllCoroutinesMDD();
        //        gameManager.EnterCombat();
        //        State = PartyState.Combat;
        //        break;
        //    }
        //}
        else if (State == PartyState.Combat)
        {
            // this is where party ai should work
            for (int i = npcsInPack.Count - 1; i >= 0; i--)
            {
                var enemy = npcsInPack[i];
                if (enemy.IsDead)
                {
                    //GameObject.Destroy(enemy.gameObject);
                    // Instead of destroying game object, i will make it 'play dead' the enemy by rotating 90 degrees on X
                    enemy.transform.rotation = Quaternion.Euler(-90f, enemy.transform.rotation.eulerAngles.x, enemy.transform.rotation.eulerAngles.z);
                    enemy.transform.position += Vector3.up;

                    npcsInPack.RemoveAt(i);
                    //enemyPartyManager.allParties.Remove(enemy);
                    PartyPortraitManagerUI portraitUI = GameManagerMDD.FindObjectOfType<PartyPortraitManagerUI>();
                    portraitUI.RemoveDeadPortraits(gameManager.combatQueue);
                }
            }
            if (npcsInPack.Count == 0)
            {
                State = PartyState.Destroyed;
                enemyPartyManager.AllEnemiesDefeated();
            }
        }
        return;
    }

    private bool IsPlayerVisible(CharacterUnit enemy)
    {
        foreach (var player in playerPartyManager.GetParty())
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
public class EnemyPackSummoner// : MonoBehaviour
{
    //public static List<CharacterUnit> activeEnemies = new List<CharacterUnit>();

    private void Update()
    {
       //if(activeEnemies.Count > 0)
       //{
       //    for (int i = activeEnemies.Count - 1; i >= 0; i--)
       //    {
       //        var enemy = activeEnemies[i];
       //        if (enemy.IsDead)
       //        {
       //            Destroy(enemy.gameObject);
       //            activeEnemies.RemoveAt(i);
       //        }
       //    }
       //
       //}
    }



}