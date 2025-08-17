using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float doorOpenDelay = 0.25f; // Time between each door opening
    [SerializeField] private DoorOpener[] doors;

    public void Interact(CharacterUnit agent)
    {
        StartCoroutine(ButtonSequence(agent));
    }

    private IEnumerator ButtonSequence(CharacterUnit agent)
    {
        // 1) Move agent to the button
        yield return agent.MoveTo(transform.position);

        // 2) Play press animation
        yield return agent.PressButtonAnimation();

        // 3) Open doors with staggered delay
        foreach (var door in doors)
        {
            if (door != null)
            {
                door.OpenDoor();
                yield return new WaitForSeconds(doorOpenDelay);
            }
        }
    }
}


