using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace PartyManagement
{
    public class PartyManager : MonoBehaviour
    {        
        // ==== Party Logic ====
        public static List<CharacterUnit> partyMembers = new List<CharacterUnit>();
        public static List<CharacterUnit> GetParty() => partyMembers;
        public static int selectedIndex = 0;
        public static CharacterUnit CurrentSelected;// => partyMembers.Count > 0 ? partyMembers[selectedIndex] : null;

        public static void SetMainAsSelected()
        {
            int index = 0;
            foreach(CharacterUnit unit in partyMembers)
            {
                if(unit.isMainCharacter)
                {
                    CurrentSelected = unit;
                    selectedIndex = index;
                    break;
                }
                index++;
            }
        }

        public static bool IsEmpty() => partyMembers.Count == 0;

        public static void AddMember(CharacterUnit newMember)
        {
            if (!partyMembers.Contains(newMember))
            {
                partyMembers.Add(newMember);
                Debug.Log("PartyManager::Party member added: " + newMember.name);
            }
        }

        public static void StopAllMovement()
        {
            foreach(CharacterUnit member in partyMembers)
            {
                member.StopMovement();
            }
        }

        public static void ResetAllActionPoints()
        {
            foreach (CharacterUnit member in partyMembers)
            {
                member.SetActionPoints(0);
            }
        }

        public static void SetStartActionPoints()
        {
            if (partyMembers.Count > 0)
            {
                foreach(var member in partyMembers)
                {
                    member.stats.ActionPoints = member.stats.StartActionPoints;
                }
            }
        }

        public static void SelectMember(int index)
        {
            if (index < 0 || index >= partyMembers.Count)
            {
                Debug.LogWarning($"PartyManager::SelectMember: index {index} is out of bounds (party size: {partyMembers.Count}).");
                return;
            }

            selectedIndex = index;
            CurrentSelected = partyMembers[selectedIndex];
        }

        public static void SelectMember(CharacterUnit member) 
        {
            int index = partyMembers.IndexOf(member);

            if (index >= 0)
            {
                selectedIndex = index;
                CurrentSelected = member;
                Debug.Log($"PartyManager::Selected member: {member.unitName} at index {index}");
            }
            else
            {
                Debug.LogWarning($"PartyManager:: error: {member.unitName} is not in the party.");
            }
        }
    }
}
