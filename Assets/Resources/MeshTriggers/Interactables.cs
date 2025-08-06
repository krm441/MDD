using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public interface IInteractable
{
    void Interact(CharacterUnit agent);
}

public interface IInteractableAction
{
    void Execute(GameObject caller);
}


