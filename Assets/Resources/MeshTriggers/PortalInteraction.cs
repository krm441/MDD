using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEngine.GraphicsBuffer;

public class PortalInteraction : MonoBehaviour, IInteractable
{
    [Header("Destination")]
    [SerializeField] private Transform target;     // set this in the Inspector
    [SerializeField] private float ringRadius = 2.5f;

    [Header("Camera")]
    [SerializeField] private IsometricCameraController isometricCamera; // for snap

    [Header("PartyManager")]
    public PartyPlayer PartyManager;

    private Coroutine coroutine;
    private Action onFinish;

    void Start()
    {
        if(PartyManager == null) PartyManager = FindObjectOfType<PartyPlayer>();
        Assert.IsNotNull(PartyManager);
    }

    public void Interact(CharacterUnit agent, Action onFinish = null)
    {
        if (target == null || PartyManager == null || agent == null)
        {
            Debug.LogWarning("PortalInteraction:: missing target or PartyManager, or agent is NULL");
            return;
        }

        coroutine = StartCoroutine(Teleport(agent));
        this.onFinish = onFinish;
    }

    public void CancelInteraction(Action onCancelled = null)
    {
        if(coroutine != null) StopCoroutine(coroutine);
        onCancelled?.Invoke();
    }

    private IEnumerator Teleport(CharacterUnit agent)
    {
        // 1) Move agent to the porta;l
        yield return agent.MoveTo(transform.position);

        // 2_) play animaaion
        yield return agent.PressButtonAnimation();

        // 3) bon voyage
        PartyManager.TeleportParty(target, ringRadius);

        // 4) snap camera
        isometricCamera.SnapToCharacter(agent.transform);

        // 5) on finish - return to movement substate
        onFinish?.Invoke();
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(target.position, ringRadius);
        Gizmos.DrawRay(target.position, target.forward * 1.5f);
    }
#endif
}
