using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpellData
{
    public string spellName;
    public int manaCost;
    public int actionPointCost;
    public int range;         // distance from caster
    public int radius;        // area around target point (0 = single target)
    public bool requiresLOS;  // needs line of sight?
    public SpellType type;    // enum: Damage, Heal, Buff, etc.
    public DamageType damageType; // enum: Fire, Ice, Physical
    public int basePower;     // base damage or healing amount
}

public enum SpellType { Damage, Heal, Buff }
public enum DamageType { Fire, Ice, Lightning, Physical }

public class CombatManager //: MonoBehaviour
{
    public void CastSpell(SpellData spell, PartyManagement.CharacterUnit caster, Vector3 targetPosition)
    {

    }

    public void EnterCombat()
    {

    }

    public void Update() { }

    void DeduceCombatQueue()
    {

    }
}
