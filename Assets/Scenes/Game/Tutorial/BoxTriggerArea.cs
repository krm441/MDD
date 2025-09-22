using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class BoxTriggerArea : MonoBehaviour
{
    [SerializeField] private SkeletalParty party;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.GetMask("PartyLayer")) return;

        SpawnSkeletonParty();
    }

    private void SpawnSkeletonParty()
    {
        Console.Error("spawned");
        party.Spawn();
    }
}

