using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AiMdd;
using PartyManagement;
using UnityEngine;
using UnityEngine.Assertions;

public enum NpcPartyType
{
    Hostile,
    Fiendly
}

public enum PartyState
{
    Idle,
    Combat,
    Destroyed
}

public class PartyNPC : IParty
{
    public NpcPartyType npcType = NpcPartyType.Hostile;
    [SerializeField] protected SpellMap spellMap;

    // Start is called before the first frame update
    void Start()
    {
        Assert.IsNotNull(spellMap);

        // party type
        partyType = PartyTypes.NPC;

        spellMap.InitializeSpells(); // NOTE:: lazy initialisation
        EnemyDatabase.Load(); // lazy init
        OnStart();
    }

    protected virtual void OnStart() { }
    protected virtual void OnUpdate() { }

    public bool IsPartyWiped =>
    partyMembers.Count > 0 && partyMembers.All(u => u == null || u.IsDead);


    // Update is called once per frame
    void Update()
    {
        switch (State)
        {
            case PartyState.Idle:
                {
                    OnUpdate();
                }
                break;
            case PartyState.Destroyed:
                break;
            case PartyState.Combat:
                {
                    for (int i = partyMembers.Count - 1; i >= 0; i--)
                    {
                        var enemy = partyMembers[i];

                        if (enemy.status == CharacterUnitStatus.Dead) continue;

                        if (enemy.IsDead)
                        {
                            // Instead of destroying game object, i will make it 'play dead' the enemy by rotating 90 degrees on X
                            enemy.transform.rotation = Quaternion.Euler(-90f, enemy.transform.rotation.eulerAngles.x, enemy.transform.rotation.eulerAngles.z);
                            enemy.transform.position += Vector3.up;

                            enemy.status = CharacterUnitStatus.Dead;
                            PartyPortraitManagerUI portraitUI = GameManagerMDD.FindObjectOfType<PartyPortraitManagerUI>();
                            portraitUI.RemoveDeadPortraits(GameManagerMDD.FindObjectOfType<GameManagerMDD>().combatQueue);
                        }
                    }
                    if (IsPartyWiped)
                    {
                        State = PartyState.Destroyed;

                        EventSystemMDD.EventSystemMDD.Raise(new EventSystemMDD.PartyWipedEvent { party = this });
                    }
                }
                break;
        }
    }

}

public static class NPCSpawnHelper
{
    public static List<Vector3> GenerateCirclePositions(Vector3 center, float radius, int count)
    {
        List<Vector3> positions = new List<Vector3>();
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            positions.Add(new Vector3(center.x + x, center.y, center.z + z));
        }

        return positions;
    }
}

