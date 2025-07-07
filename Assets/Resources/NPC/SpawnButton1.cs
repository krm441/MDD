using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnButton1 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SpawnParty1()
    {
        EnemyPackSummoner.SpawnDebugPack(new Vector3(20, 0, 20), 3);
    }
}
