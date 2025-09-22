using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private float doorOpenDelay = 0.25f; // Time between each door opening
    [SerializeField] protected DoorOpener[] doors;

    protected Coroutine coroutine;
    protected Action onCompleted;
    protected Action onCancel;

    public virtual void Interact(CharacterUnit agent, Action onCompleted)
    {
        this.onCompleted = onCompleted;

        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(ButtonSequence(agent));
    }

    public void CancelInteraction(Action onCancelled)
    {
        this.onCancel = onCancelled;

        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
            this.onCancel?.Invoke();
        }
    }

    protected IEnumerator ButtonSequence(CharacterUnit agent, Action lateAction = null)
    {
        // 1) Move agent to the button
        yield return agent.MoveTo(transform.position, 2f);

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

        coroutine = null;

        this.onCompleted?.Invoke();

        lateAction?.Invoke();
    }
}


