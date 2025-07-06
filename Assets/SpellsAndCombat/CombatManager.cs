using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;



public class CombatManager //: MonoBehaviour
{
    public static void ApplySpell(CharacterUnit caster, Spell spell, Vector3 targetPosition)
    {
        Debug.Log("SPELL FIRE");

        GameObject fireballPrefab = Resources.Load<GameObject>("Projectiles/FireballPref"); // for now just the fire spell
        if (fireballPrefab == null)
        {
            Debug.LogError("Missing fireball prefab at Resources/Projectiles/FireballPref");
            return;
        }

        Vector3 origin = caster.transform.position + Vector3.up * 1.5f;
        GameObject projectile = GameObject.Instantiate(fireballPrefab, origin, Quaternion.identity);

        ProjectileBallistic ballistic = projectile.GetComponent<ProjectileBallistic>();
        ballistic.Launch(origin, targetPosition, () =>
        {
            List<GameObject> targets = FindTargetsInRadius(targetPosition, spell.radius);
        
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    int damage = CalculateDamage(caster, spell, target);

                    if (target.TryGetComponent<CharacterUnit>(out var unit))// ||
                     //   target.transform.parent?.TryGetComponent<CharacterUnit>(out unit) == true) 
                    {
                        unit.stats.HP -= damage;
                        Debug.Log($"{caster.unitName} hit {unit.unitName} for {damage} damage.");
                    }
                }
            }
            else
            {
                Debug.Log("ApplySpell::No hits");
            }
        });
    }

    private static int CalculateDamage(CharacterUnit caster, Spell spell, GameObject target)
    {
        // example logic - mage for now, then adds to willpower. or just maybe should be converted to main stat
        return spell.manaCost + caster.stats.Intelligence; 
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="radius"></param>
    /// <param name="losSource">Lign of Sight</param>
    /// <returns></returns>
    private static List<GameObject> FindTargetsInRadius(Vector3 pos, float radius, Vector3? losSource = null) 
    {
        List<GameObject> validTargets = new List<GameObject>();
        LayerMask affectedLayers = LayerMask.GetMask("PartyLayer", "Destructibles");
        Collider[] hits = Physics.OverlapSphere(pos, radius, affectedLayers);

        Debug.Log($"Checking radius {radius} at {pos}");
        foreach (var hit in hits)
        {
            Debug.Log($"Hit: {hit.name}");
        }

        foreach (var hit in hits)
        {
            if (losSource.HasValue)
            {
                if (!HasLineOfSight(losSource.Value, hit.transform.position))
                    continue;
            }

            validTargets.Add(hit.gameObject);
        }

        return validTargets;

        //return null;
    }
    private static bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        return !Physics.Linecast(from + Vector3.up, to + Vector3.up, LayerMask.GetMask("Obstacles"));
    }


    public void EnterCombat()
    {

    }

    public void Update() { }

    void DeduceCombatQueue()
    {

    }
}
