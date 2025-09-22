using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TutorialNPCs : DiggersNPCs
{
    public bool isDefeated = false;
    [SerializeField] private PartyPlayer player;

    protected override void OnStart()
    {
        // push event
        EventSystemMDD.EventSystemMDD.PartyWipe += PartyWipe;

        if (isDefeated) return;
        
        base.OnStart();        
    }

    private void PartyWipe(EventSystemMDD.PartyWipedEvent e)
    {
        if (e.party.partyType == PartyTypes.Player)
        {
            return;
        }

        isDefeated = true;

        var reg = FindObjectOfType<CharacterUnitReg>();
        var party = reg.GetContext(player.CurrentSelected).parent;
        CheckPointLoader.SaveCheckPoint(party.GetCurrentCapture(), player.CurrentSelected.transform);
    }
}
