using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public class CombatQueue : MonoBehaviour
{
    [SerializeField]
    private GameManagerMDD gameManager;

    public CharacterUnit current;

    public Queue<CharacterUnit> unitQueue;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
