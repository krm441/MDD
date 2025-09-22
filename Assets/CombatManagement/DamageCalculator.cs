using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


// Chain of Responsibility Pattern: for damage calculation


/* For reference:

    // Physical
    public int Slashing = 0;
    public int Piercing = 0;
    public int Crushing = 0;

    // Elemental
    public int Fire = 0;
    public int Water = 0;
    public int Wind = 0;
    public int Soil = 0; // earth damage

    // Spiritual
    public int Light = 0;
    public int Shadow = 0;

    // Heretique
    public int Necrotic = 0;
    public int Poison = 0;
    public int Demonic = 0;

    // Healing
    public int Healing = 0;
    public int MentalFortification = 0;
    public int MagicFortification = 0;
*/

public class DamageContext
{
    public CharacterUnit Caster;
    public CharacterUnit Target;
    public Spell Spell;
    public CombatManager CombatManager;
}

/// <summary>
/// Modifiers are stored in global scope and serve the scalability of game difficulty level
/// </summary>
public static class DamageGlobals
{
    public static int DamageModifier = 50;
}

interface IDamageModifier
{
    DamageResistenceContainer Apply(DamageResistenceContainer damage, DamageContext context);
}

class CritModifier : IDamageModifier
{
    public DamageResistenceContainer Apply(DamageResistenceContainer damage, DamageContext context)
    {
        return damage;
    }
}

class BuffCaster : IDamageModifier
{
    public DamageResistenceContainer Apply(DamageResistenceContainer damage, DamageContext context)
    {
        return damage;
    }
}

class ResistanceModifier : IDamageModifier
{
    public DamageResistenceContainer Apply(DamageResistenceContainer damage, DamageContext context)
    {
        var result = damage.Clone();

        // Physical
        result.Slashing = (int)ApplyResistance(result.Slashing, context.Target.attributeSet.resistances.Slashing);
        result.Piercing = (int)ApplyResistance(result.Piercing, context.Target.attributeSet.resistances.Piercing);
        result.Crushing = (int)ApplyResistance(result.Crushing, context.Target.attributeSet.resistances.Crushing);

        // Elemental
        result.Fire     = (int)ApplyResistance(result.Fire,     context.Target.attributeSet.resistances.Fire);
        result.Water    = (int)ApplyResistance(result.Water,    context.Target.attributeSet.resistances.Water);
        result.Wind     = (int)ApplyResistance(result.Wind,     context.Target.attributeSet.resistances.Wind);
        result.Earth    = (int)ApplyResistance(result.Earth,    context.Target.attributeSet.resistances.Earth);

        // Spiritual
        result.Light    = (int)ApplyResistance(result.Light,    context.Target.attributeSet.resistances.Light);
        result.Shadow   = (int)ApplyResistance(result.Shadow,   context.Target.attributeSet.resistances.Shadow);

        // Heretique
        result.Necrotic = (int)ApplyResistance(result.Necrotic, context.Target.attributeSet.resistances.Necrotic);
        result.Poison   = (int)ApplyResistance(result.Poison,   context.Target.attributeSet.resistances.Poison);
        result.Demonic  = (int)ApplyResistance(result.Demonic,  context.Target.attributeSet.resistances.Demonic);

        // Healing
        result.Healing              = (int)ApplyResistance(result.Healing,              context.Target.attributeSet.resistances.Healing);
        result.MentalFortification  = (int)ApplyResistance(result.MentalFortification,  context.Target.attributeSet.resistances.MentalFortification);
        result.MagicFortification   = (int)ApplyResistance(result.MagicFortification,   context.Target.attributeSet.resistances.MagicFortification);

        return result;
    }

    private float ApplyResistance(float baseDamage, float resistance)
    {
        float multiplier = 1f - (resistance / (resistance + DamageGlobals.DamageModifier));
        return baseDamage * multiplier;
    }
}


public class DamageCalculator
{
    List<IDamageModifier> modifiers = new List<IDamageModifier>()
    {
        new CritModifier(),
        new BuffCaster(),
        new ResistanceModifier(),
    };

    public void CalculateDamage(DamageContext context)
    {
        DamageResistenceContainer damage = context.Spell.baseDamage; // Base damage

        foreach (var mod in modifiers)
        {
            damage = mod.Apply(damage, context);
        }

        int totalPhy = damage.TotalPhysical;
        int totalMag = damage.TotalElemental;

        ApplyDamagePhysical(context.Target, totalPhy, context);
        ApplyDamageMagical(context.Target, totalMag, context);
        Heal(context.Target, damage.Healing, context);
    }

    private void Heal(CharacterUnit target, int healing, DamageContext context)
    {
        if (healing <= 0) return; // early return

        target.attributeSet.stats.HP += healing;
        context.CombatManager.ShowHealing(healing, target.transform.position + new Vector3(-0.5f, 0), Color.green);
    }

    private void ApplyDamagePhysical(CharacterUnit target, int totalPhy, DamageContext context)
    {
        if (totalPhy <= 0) return; // early return

        int armor = target.attributeSet.armorStat.physicalArmor;

        // Calculate damage that armor can absorb
        int damageToArmor = Mathf.Min(armor, totalPhy);
        int damageToHP = totalPhy - damageToArmor;

        // Apply armor damage
        target.attributeSet.armorStat.physicalArmor = armor - damageToArmor;

        // Apply remaining to HP
        target.attributeSet.stats.HP -= damageToHP;

        if (damageToHP > 0)
            context.CombatManager.ShowDamage(damageToHP * -1, target.transform.position + new Vector3(0.5f, 0), Color.red);
        else
            context.CombatManager.ShowDamage(damageToArmor * -1, target.transform.position + new Vector3(0.5f, 0), Color.grey);
    }

    private void ApplyDamageMagical(CharacterUnit target, int totalMag, DamageContext context)
    {
        if(totalMag <= 0) return; // early return

        int armor = target.attributeSet.armorStat.magicArmor;

        // Calculate damage that armor can absorb
        int damageToArmor = Mathf.Min(armor, totalMag);
        int damageToHP = totalMag - damageToArmor;

        // Apply armor damage
        target.attributeSet.armorStat.magicArmor = armor - damageToArmor;

        // Apply remaining to HP
        target.attributeSet.stats.HP -= damageToHP;

        if(damageToHP > 0)
            context.CombatManager.ShowDamage(damageToHP * -1, target.transform.position + new Vector3(0.5f, 0), Color.red);
        else
            context.CombatManager.ShowDamage(damageToArmor * -1, target.transform.position + new Vector3(0.5f, 0), Color.blue);
    }

    /// <summary>
    /// Physical - willpower
    /// </summary>
    private void SeverConnectionVsAnscestors() { }

    /// <summary>
    /// Magical
    /// </summary>
    private void SeverConnectionVsElementals() { }

    /// <summary>
    /// Spiritual - heretique
    /// </summary>
    private void SeverConnectionVsLightAndDarkness() { }
}
