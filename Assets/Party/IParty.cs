using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.Assertions;

public enum PartyTypes
{
    None,
    Player,
    NPC,
}

public abstract class IParty : MonoBehaviour 
{
    public List<CharacterUnit> partyMembers = new List<CharacterUnit>();
    [SerializeField] CombatManagement combatManagement;
    public PartyState State = PartyState.Idle;
    public CharacterUnit CurrentSelected;
    public PartyTypes partyType = PartyTypes.None;

    private void Awake()
    {
        Assert.IsNotNull(combatManagement);
        combatManagement.RegisterParty(this);
    }

    [SerializeField] private CharacterUnitReg registry;

    public virtual void AddMember(CharacterUnit unit)
    {
        partyMembers.Add(unit);
    }

    public float GetDistace(Vector3 from)
    {
        return Vector3.Distance(from, this.transform.position);
    }

    public void StopAllMovement()
    {
        foreach (CharacterUnit member in partyMembers)
        {
            member.StopMovement();
        }
    }

    protected void PushCharToReg(CharacterUnit unit)
    {
        if(registry == null)
        {
            registry = FindObjectOfType<CharacterUnitReg>();
            Assert.IsNotNull(registry);
        }

        registry.RegisterCharacterUnit(unit, this);
    }

    public virtual void HandleScriptedAction() { }
    public virtual PlayerPartyData GetCurrentCapture() { return null; }
}
