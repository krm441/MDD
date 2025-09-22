using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using UnityEngine.Profiling;
using System;

[System.Serializable]
public class PlayerPartyData
{
    public int version = 1;
    public List<CharacterMetaData> party;
    public string profileName = "DefaultUser";

    public PlayerPartyData() { }

    public PlayerPartyData(PlayerPartyData other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));

        this.version = other.version;
        this.profileName = other.profileName;

        if (other.party != null)
        {
            this.party = new List<CharacterMetaData>(other.party.Count);
            for (int i = 0; i < other.party.Count; i++)
            {
                var member = other.party[i];
                this.party.Add(member != null ? new CharacterMetaData(member) : null);
            }
        }
        else
        {
            this.party = new List<CharacterMetaData>();
        }
    }
}

public static class SaveSystem
{
    public static string profileName = "DefaultUser";

    public static void Save(PlayerPartyData profile)
    {
        var json = JsonUtility.ToJson(profile, prettyPrint: true);
        string Path = System.IO.Path.Combine(Application.persistentDataPath, profile.profileName + ".json");
        profileName = profile.profileName; // save for Load
        File.WriteAllText(Path, json);
    }

    public static PlayerPartyData Load()
    {
        string Path = System.IO.Path.Combine(Application.persistentDataPath, profileName + ".json");
        if (!File.Exists(Path))
            return new PlayerPartyData(); // defaults

        try
        {
            var json = File.ReadAllText(Path);
            return JsonUtility.FromJson<PlayerPartyData>(json) ?? new PlayerPartyData();
        }
        catch
        {
            // Corrupt/old save: create new
            Console.Error("SaveSystem::Load, fail - creating default party");
            return new PlayerPartyData();
        }
    }
}

namespace PartyManagement
{
    public class PartyManager : MonoBehaviour
    {        
        // ==== Party Logic ====
        public List<CharacterUnit> partyMembers = new List<CharacterUnit>();
        public static readonly int MaxPartyUnits = 16;
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

        public void TeleportParty(Transform destination, float ringRadius = 2.5f)
        {
            TeleportParty(destination.position, ringRadius);
        }

        public void TeleportParty(Vector3 destination, float ringRadius = 2.5f)
        {
            if (destination == null)
            {
                Debug.LogWarning("PartyManager::TeleportParty: destination is null.");
                return;
            }

            if (partyMembers == null || partyMembers.Count == 0)
            {
                Debug.LogWarning("PartyManager::TeleportParty: party is empty.");
                return;
            }

            // 0) stop any current movement to avoid fighting the warp
            StopAllMovement();

            // 1) choose a leader (prefer currently selected, then main, then first)
            CharacterUnit leader = CurrentSelected;
            if (leader == null)
                leader = partyMembers.Find(m => m.isMainCharacter) ?? partyMembers[0];

            // 2) place leader at the exact target
            PlaceOnNavMesh(leader, destination);

            // 3) place others around a ring
            int placed = 0;
            for (int i = 0; i < partyMembers.Count; i++)
            {
                var member = partyMembers[i];
                if (member == leader) continue;

                float t = (placed + 1) / (float)(partyMembers.Count); // spread 0..1
                float angle = t * 360f;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * ringRadius);
                Vector3 candidate = destination + offset;

                PlaceOnNavMesh(member, candidate);
                placed++;
            }

            // Everyone face the portal's forward
           //foreach (var m in partyMembers)
           //{
           //    if (m != null)
           //    {
           //        var lookTarget = destination + destination.forward * 10f;
           //        m.transform.rotation = Quaternion.LookRotation((lookTarget - m.transform.position).normalized, Vector3.up);
           //    }
           //}
        }

        private void PlaceOnNavMesh(CharacterUnit unit, Vector3 pos)
        {
            if (unit == null) return;

            // uncarve
            var obstacle = unit.GetComponent<NavMeshObstacle>();
            if (obstacle) obstacle.enabled = false;

            var agent = unit.agent;
            if (agent != null)
            {
                if (!agent.enabled) agent.enabled = true;

                NavMeshHit hit;
                if (NavMesh.SamplePosition(pos, out hit, 2.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);  // safe teleport on NavMesh
                    agent.ResetPath();
                }
                else
                {
                    // No mesh nearby; hard set as a fallback
                    unit.transform.position = pos;
                }
            }
            else
            {
                unit.transform.position = pos;
            }
        }

        public List<CharacterUnit> GetPlayerControlledUnits()
        {
            return partyMembers;
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
