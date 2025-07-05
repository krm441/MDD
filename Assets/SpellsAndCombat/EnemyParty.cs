using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

/// <summary>
/// Represents a possible pack of enemies
/// </summary>
public class EnemyParty : MonoBehaviour 
{
    public List<CharacterUnit> enemiesInPack;

    public void Update()
    {
        // check line of sight - prefereably as a party... or maybe as a loop of each enemy LOS
    }
}

public enum EnemyPackType
{
    SimpleEnemies,
}

/// <summary>
/// Used in UI or by other scripts to summon enemy pack at certain location
/// </summary>
public class EnemyPackSummoner
{
    public void SummonPack(EnemyPackType type, int numberOfEnemies, Vector3 location)
    {

    }
}