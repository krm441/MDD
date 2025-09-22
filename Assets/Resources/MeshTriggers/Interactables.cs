using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public interface IInteractable
{
    void Interact(CharacterUnit agent, Action onCompleted = null);
    void CancelInteraction(Action onCancelled = null);
}

public interface IInteractableAction
{
    void Execute(GameObject caller);
}


