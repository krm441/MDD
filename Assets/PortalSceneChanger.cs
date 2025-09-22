using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public class PortalSceneChanger : MonoBehaviour, IInteractable
{
    [SerializeField] private string NextScene = "Intro";

    public void Interact(CharacterUnit agent, Action ac = null)
    {
        StartCoroutine(ChangeScene(agent));        
    }

    public void CancelInteraction(Action onCancelled = null)
    {

    }

    private IEnumerator ChangeScene(CharacterUnit agent)
    {
        // 1) Move agent to the portal
        yield return agent.MoveTo(transform.position);

        // 2) play animaaion
        yield return agent.PressButtonAnimation();

        // extra - reload the checkpoints
        TutorialScenePersistentData.isUsed = false;
        TutorialScenePersistentData.EnemiesDefeated = false;
        TutorialScenePersistentData.DoorOpened = false;

        // 3) load scene
        SceneChangerTest.LoadScene(NextScene);
    }
}
