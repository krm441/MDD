using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

public class PartyMerger : MonoBehaviour
{
    [SerializeField] public CharacterUnit character;
    [SerializeField] private CharacterUnitReg registry;

    public bool visited = false;

    private void OnTriggerEnter(Collider other)
    {
        if (visited) return;

        if(other.GetComponent<CharacterUnit>() == character) return;
        var unit = other.GetComponent<CharacterUnit>();
        if(unit != null && unit != character)
        {
            var party = registry.GetContext(unit).parent;
            party.AddMember(character);
            visited = true;
        }
    }

    /*public void Init()
    {
        occupied = true;

        Instantiate(character);
        character.gameObject.transform.position = transform.position;
        character.gameObject.SetActive(true);
    }*/
}
