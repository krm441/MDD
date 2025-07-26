using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiTesterCor : MonoBehaviour
{
    [SerializeField] private CharacterSpawner spawner;

    // Start is called before the first frame update
    void Start()
    {
        SpellMap.InitializeSpells();
        spawner.SpawnPartyTypeOne();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
