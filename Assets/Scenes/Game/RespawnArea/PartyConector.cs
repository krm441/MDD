using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PartyManagement;

public class PartyConector : MonoBehaviour
{
    [SerializeField] private List<PartyMerger> mergerList;

    [SerializeField] private PartyPlayer player;

    [SerializeField] private IsometricCameraController controller;
    
    bool init = false;

    private void Start()
    {

        controller.LerpToCharacter(player.transform);
    }

    private void Update()
    {
        if (init) return;

        foreach (var merger in mergerList)
        {
            foreach(var player_ in player.partyMembers)
            {
                if (merger.GetComponentInChildren<CharacterUnit>().unitName == player_.unitName)
                {
                    merger.gameObject.SetActive(false);

                }
            }

            //f (merger.GetComponentInChildren<CharacterUnit>().unitName == player.partyMembers[0].unitName)
            //
            //   merger.gameObject.SetActive(false);
            //   
            //
        }
        
        init = true;
    }
}
