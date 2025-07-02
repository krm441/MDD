using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartyManagement
{
    // singleton
    public class PartyManager : MonoBehaviour
    {
        public static PartyManager Instance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Keep this object across scenes
            }
            else
            {
                Destroy(gameObject); // Enforce singleton
                Debug.Log("PartyManager destroyed");
            }
        }

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