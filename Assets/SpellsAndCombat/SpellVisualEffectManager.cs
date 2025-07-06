using PartyManagement;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FOR FUTURE: when ammount of spell will be too big, i will use this
/// </summary>
[CreateAssetMenu(menuName = "Spells/Spell VFX Data")]
public class SpellVFXData : ScriptableObject
{
    public string vfxId; // vfx id
    public GameObject projectilePrefab;
    public GameObject impactEffect; // this is important, like fire impact leaves a pool of fire
}


public static class SpellVisualEffectsManager
{
    private static Dictionary<string, SpellVFXData> vfxMap;

    // FOR FUTURE
    public static void Initialize()
    {
        vfxMap = new Dictionary<string, SpellVFXData>();
        foreach (var vfx in Resources.LoadAll<SpellVFXData>("VFX"))
        {
            if (!vfxMap.ContainsKey(vfx.vfxId))
                vfxMap[vfx.vfxId] = vfx;
        }
    }

    public static void LaunchSpellVFX(Spell spell, CharacterUnit caster, Vector3 targetPosition, Action onImpact)
    {
        switch (spell.vfxType.ToLowerInvariant())
        {
            case "fireball":
                LaunchProjectile("FireballPref", caster, targetPosition, onImpact);
                break;

            default:
                Debug.LogWarning($"Unknown spell VFX type: {spell.vfxType}. Falling back to default.");
                LaunchProjectile("FireballPref", caster, targetPosition, onImpact); // for now default will be the fireball visuals
                break;
        }
    }

    private static void LaunchProjectile(string prefabName, CharacterUnit caster, Vector3 targetPosition, Action onImpact)
    {
        GameObject prefab = Resources.Load<GameObject>($"Projectiles/{prefabName}");
        if (prefab == null)
        {
            Debug.LogError($"Spell VFX prefab '{prefabName}' not found.");
            return;
        }

        Vector3 origin = caster.transform.position + Vector3.up * 1.5f;
        GameObject proj = GameObject.Instantiate(prefab, origin, Quaternion.identity);

        if (proj.TryGetComponent<ProjectileBallistic>(out var ballistic))
        {
            ballistic.Launch(origin, targetPosition, () => onImpact?.Invoke());
        }
        else
        {
            Debug.LogWarning($"Prefab '{prefabName}' missing ProjectileBallistic.");
            GameObject.Destroy(proj);
        }
    }
}
