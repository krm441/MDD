using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public class SceneChangerIntro : MonoBehaviour, IInteractable
{
    [SerializeField] private string NextScene = "BSP";
    [SerializeField] private SceneChangerTest sceneChanger;

    public void Interact(CharacterUnit agent, Action ac = null)
    {
        if(NextScene == "BSP")
            sceneChanger.LoadGame_BSP();
        else if (NextScene == "CA")
            sceneChanger.LoadGame_CA();
        else if (NextScene == "GG")
            sceneChanger.LoadGame_GG();
    }

    public void CancelInteraction(Action onCancelled = null)
    { 
    
    }
    
}
