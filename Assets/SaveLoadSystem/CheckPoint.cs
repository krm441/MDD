using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class CheckPoint
{
    public Vector3 pos;
    public PlayerPartyData metaData;
}

public static class CheckPointLoader
{
    public static CheckPoint lastCheckPoint;

    public static void LoadLastCheckPoint(PartyPlayer party)
    {
        if (lastCheckPoint == null) return;

        party.LoadFromData(lastCheckPoint.metaData);
        party.TeleportParty(lastCheckPoint.pos);
    }

    public static void SaveCheckPoint(PlayerPartyData metaData, Transform transform)
    {
        Assert.IsNotNull(metaData);
        Assert.IsNotNull(transform);
        lastCheckPoint = new CheckPoint { metaData = metaData , pos = transform.position };
    }

}
