using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType
{
    None,
    Shield,
    Melee_Slice, Melee_Stab,
    Ranged
}

[System.Serializable]
public class Weapon
{
    public WeaponType type = WeaponType.None;
    public int power = 0;
    public float range = 8f;
    public int slots = 1;
    public int apCost = 2;
    public string SFX = "none";
    public string VFX = "none";
}
