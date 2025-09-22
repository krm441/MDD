using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewGameScr : MonoBehaviour
{
    public void CreatePlayerParty()
    {
        GameSession.playerParty = new PlayerPartyData();
    }
}
