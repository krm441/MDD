using System.Collections.Generic;
using UnityEngine;

namespace PartyManagement
{
    public class PartyManager : MonoBehaviour
    {        
        // ==== Party Logic ====
        public static List<CharacterUnit> partyMembers = new List<CharacterUnit>();
        public static int selectedIndex = 0;
        public static CharacterUnit CurrentSelected => partyMembers.Count > 0 ? partyMembers[selectedIndex] : null;

        public static void AddMember(CharacterUnit newMember)
        {
            if (!partyMembers.Contains(newMember))
            {
                partyMembers.Add(newMember);
                Debug.Log("Party member added: " + newMember.name);
            }
        }
    }
}
