using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttributeSet
{
    public StatBlock stats;
    public ArmorStat armorStat;
    public DamageResistenceContainer resistances;

    public AttributeSet() { }

    public AttributeSet(AttributeSet other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));

        stats = other.stats != null ? new StatBlock(other.stats) : null;
        armorStat = other.armorStat != null ? new ArmorStat(other.armorStat) : null;
        resistances = other.resistances != null ? new DamageResistenceContainer(other.resistances) : null;
    }
} 
