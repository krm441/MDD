using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.ShaderData;
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
    public AttributeSet Target;
    public Spell Spell;
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
        result.Slashing = (int)ApplyResistance(result.Slashing, context.Target.resistances.Slashing);
        result.Piercing = (int)ApplyResistance(result.Piercing, context.Target.resistances.Piercing);
        result.Crushing = (int)ApplyResistance(result.Crushing, context.Target.resistances.Crushing);

        // Elemental
        result.Fire     = (int)ApplyResistance(result.Fire,     context.Target.resistances.Fire);
        result.Water    = (int)ApplyResistance(result.Water,    context.Target.resistances.Water);
        result.Wind     = (int)ApplyResistance(result.Wind,     context.Target.resistances.Wind);
        result.Earth    = (int)ApplyResistance(result.Earth,    context.Target.resistances.Earth);

        // Spiritual
        result.Light    = (int)ApplyResistance(result.Light,    context.Target.resistances.Light);
        result.Shadow   = (int)ApplyResistance(result.Shadow,   context.Target.resistances.Shadow);

        // Heretique
        result.Necrotic = (int)ApplyResistance(result.Necrotic, context.Target.resistances.Necrotic);
        result.Poison   = (int)ApplyResistance(result.Poison,   context.Target.resistances.Poison);
        result.Demonic  = (int)ApplyResistance(result.Demonic,  context.Target.resistances.Demonic);

        // Healing
        result.Healing              = (int)ApplyResistance(result.Healing,              context.Target.resistances.Healing);
        result.MentalFortification  = (int)ApplyResistance(result.MentalFortification,  context.Target.resistances.MentalFortification);
        result.MagicFortification   = (int)ApplyResistance(result.MagicFortification,   context.Target.resistances.MagicFortification);

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

        ApplyDamagePhysical(context.Target, totalPhy);
        ApplyDamageMagical(context.Target, totalMag);
        Heal(context.Target, damage.Healing);
    }

    private void Heal(AttributeSet target, int healing)
    {
        target.stats.HP += healing;
    }

    private void ApplyDamagePhysical(AttributeSet target, int totalPhy)
    {
        int damage = target.armorStat.physicalArmor - totalPhy;

        // apply for HP
        target.stats.HP -= target.armorStat.physicalArmor - damage;

        // apply for armor
        target.armorStat.physicalArmor = Mathf.Max(target.armorStat.physicalArmor - damage, 0);
    }

    private void ApplyDamageMagical(AttributeSet target, int totalMag)
    {
        int damage = target.armorStat.magicArmor - totalMag;

        // apply for HP
        target.stats.HP -= target.armorStat.magicArmor - damage;

        // apply for armor
        target.armorStat.magicArmor = Mathf.Max(target.armorStat.magicArmor - damage, 0);
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
