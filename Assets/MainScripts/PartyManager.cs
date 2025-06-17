using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartyManagement
{
    public class PartyManager : MonoBehaviour
    {
        public void AddMember(CharacterUnit newMember)
        {
            if (!partyMembers.Contains(newMember))
            {
                partyMembers.Add(newMember);
                Debug.Log("Party member added: " + newMember.name);
            }
        }

        public List<CharacterUnit> partyMembers = new List<CharacterUnit>();
        public int selectedIndex = 0;

        public CharacterUnit CurrentSelected => partyMembers[selectedIndex];
    }
}