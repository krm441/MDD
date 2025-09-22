using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiTesterCor : MonoBehaviour
{
    [SerializeField] private CharacterSpawner spawner;
    [SerializeField] private SpellMap spellMap;

    // Start is called before the first frame update
    void Start()
    {
        spellMap.InitializeSpells();
        spawner.SpawnPartyTypeOne();
    }
}
