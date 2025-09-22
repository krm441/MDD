using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Special derivation of ButtonInteractable - specific for the tutorial scene
/// Used by the check point loader
/// </summary>
public class TutorialDoorOpener : ButtonInteractable
{
    public override void Interact(CharacterUnit agent, Action onCompleted)
    {
        this.onCompleted = onCompleted;

        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(ButtonSequence(agent, () => 
            { 
                TutorialScenePersistentData.DoorOpened = true;
                TutorialScenePersistentData.isUsed = true;

                // save check point
                var reg = FindObjectOfType<CharacterUnitReg>();
                var party = reg.GetContext(agent).parent;
                CheckPointLoader.SaveCheckPoint(party.GetCurrentCapture(), agent.transform);

                // raise event and popup the combat tutorial
                EventSystemMDD.EventSystemMDD.Raise(new EventSystemMDD.ShowCombatTutorialEvent { });
            }));
    }

    public void InstantOpen()
    {
        foreach (var door in doors)
        {
            if (door != null)
            {
                door.OpenDoorInstant();
            }
        }
    }
}
