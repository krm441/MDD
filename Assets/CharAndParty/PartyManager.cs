using System.Collections.Generic;
using UnityEngine;

namespace PartyManagement
{
    public class PartyManager : MonoBehaviour
    {        
        // ==== Party Logic ====
        public List<CharacterUnit> partyMembers = new List<CharacterUnit>();
        public List<CharacterUnit> GetParty() => partyMembers;
        public int selectedIndex = 0;
        public CharacterUnit CurrentSelected;// => partyMembers.Count > 0 ? partyMembers[selectedIndex] : null;

        [SerializeField] private SpellMap spellMap;
        [SerializeField] private GameManagerMDD gameManager;

        public void SetMainAsSelected()
        {
            //int index = 0;
            foreach(CharacterUnit unit in partyMembers)
            {
                if(unit.isMainCharacter)
                {
                    //CurrentSelected = unit;
                    //selectedIndex = index;
                    SelectMember(unit);
                    break;
                }
                //index++;
            }
        }

        public bool IsEmpty() => partyMembers.Count == 0;

        public void AddMember(CharacterUnit newMember)
        {
            if (!partyMembers.Contains(newMember))
            {
                partyMembers.Add(newMember);
                Debug.Log("PartyManager::Party member added: " + newMember.name);
            }
        }

        public void StopAllMovement()
        {
            foreach(CharacterUnit member in partyMembers)
            {
                member.StopMovement();
            }
        }

        public void ResetAllActionPoints()
        {
            foreach (CharacterUnit member in partyMembers)
            {
                member.SetActionPoints(0);
            }
        }

        public void SetStartActionPoints()
        {
            if (partyMembers.Count > 0)
            {
                foreach(var member in partyMembers)
                {
                    member.attributeSet.stats.ActionPoints = member.attributeSet.stats.StartActionPoints;
                }
            }
        }

        public void SelectMember(int index)
        {
            if (index < 0 || index >= partyMembers.Count)
            {
                Debug.LogWarning($"PartyManager::SelectMember: index {index} is out of bounds (party size: {partyMembers.Count}).");
                return;
            }

            selectedIndex = index;
            CurrentSelected = partyMembers[selectedIndex];

            spellMap.BuildIconBar(CurrentSelected, gameManager);
        }

        public void SelectMember(CharacterUnit member) 
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

            spellMap.BuildIconBar(member, gameManager);
        }
    }
}
