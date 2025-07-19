using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

/// <summary>
/// Represents a possible pack of enemies
/// </summary>
public class EnemyParty : MonoBehaviour 
{
    public List<CharacterUnit> enemiesInPack = new List<CharacterUnit>();

    public GameManagerMDD gameManager;

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
        enemiesInPack = members;
        enemyPartyManager = manager;
        this.playerPartyManager = playerPartyManager;

        manager.RegisterParty(this);
    }

    public enum PartyState
    {
        Idle,
        Combat,
        Destroyed
    }

    public PartyState State = PartyState.Idle;

    private void Update()
    {
        if (State == PartyState.Idle)
            foreach (var enemy in enemiesInPack)
            {
                if (IsPlayerVisible(enemy))
                {
                    Console.Log("Combat triggered by " + enemy.unitName);
                    gameManager.EnterCombat();
                    State = PartyState.Combat;
                    break;
                }
            }
        else if (State == PartyState.Combat)
        {
            // this is where party ai should work
            for (int i = enemiesInPack.Count - 1; i >= 0; i--)
            {
                var enemy = enemiesInPack[i];
                if (enemy.IsDead)
                {
                    GameObject.Destroy(enemy.gameObject);
                    enemiesInPack.RemoveAt(i);
                    //enemyPartyManager.allParties.Remove(enemy);
                    PartyPortraitManagerUI portraitUI = GameManagerMDD.FindObjectOfType<PartyPortraitManagerUI>();
                    portraitUI.RemoveDeadPortraits(gameManager.combatQueue);
                }
            }
            if(enemiesInPack.Count == 0)
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