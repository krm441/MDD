using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using Pathfinding;
using UnityEngine;
using static VoronoiLayoutGenerator;



public class CombatManager : MonoBehaviour
{
    //private bool inCombat = false;
    //public bool InCombat() => inCombat;
    //public void SetInCombat() {  inCombat = true; }
    //public void SetOutOfCombat() {  inCombat = false; }

    //public static Queue<CharacterUnit> turnQueue;

    [SerializeField] private GameObject floatingTextPrefab;

    public void ShowDamage(int damage, Vector3 position, Color color)
    {
        Vector3 offset = new Vector3(0, 2.5f, 0);
        Vector3 spawnPosition = position + offset;

        GameObject textObj = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
        textObj.GetComponent<FloatingDamageText>().Setup(damage.ToString(), color);
    }

    public void ShowHealing(int healing, Vector3 position, Color color)
    {
        Vector3 offset = new Vector3(0, 2.5f, 0);
        Vector3 spawnPosition = position + offset;

        GameObject textObj = Instantiate(floatingTextPrefab, spawnPosition, Quaternion.identity);
        textObj.GetComponent<FloatingDamageText>().Setup("+" + healing.ToString(), color);
    }

    public IEnumerator WalkAndMelee(CharacterUnit attacker, CharacterUnit target, Action onComplete)
    {
        var stoppingDistance = target.GetRadius() + attacker.GetRadius();

        yield return attacker.MoveTo(target.GetFeetPos(), stoppingDistance);

        var spell = new Spell { baseDamage = new DamageResistenceContainer { Slashing = attacker.weapon.power } };

        damageCalculator.CalculateDamage(new DamageContext { Caster = attacker, Spell = spell, Target = target, CombatManager = this });

        onComplete?.Invoke();
    }

    public void ApplySpell(GameManagerMDD gameManager, CharacterUnit caster, Spell spell, Vector3 targetPosition, Action onImpactComplete)
    {
        Debug.Log("SPELL FIRE");

        // Rotate to face the target:
        caster.LookAtTarget(targetPosition);

        // SFX - start
        gameManager.soundPlayer.PlayClipAtPoint(spell.sfxOnStart, targetPosition);

        // VFX
        SpellVisualEffectsManager.LaunchSpellVFX(spell, caster, targetPosition, () =>
        {

            LayerMask affectedLayers = LayerMask.GetMask("PartyLayer", "Destructibles", "HostileNPCs");
            if(caster.gameObject.layer == LayerMask.NameToLayer("PartyLayer"))
                if(!spell.friendlyFire)
                    affectedLayers = LayerMask.GetMask("Destructibles", "HostileNPCs");

            var targets = FindTargetsInRadius(targetPosition, spell.radius, affectedLayers);
            if (targets != null && targets.Count > 0)
            {
                foreach (var target in targets)
                {
                    if (spell.dPSType == SpellDPSType.Melee)
                    {
                        if (
                            target.TryGetComponent<CharacterUnit>(out var isSelf) ||
                            target.transform.parent?.TryGetComponent<CharacterUnit>(out isSelf) == true
                        )
                        {
                            if(isSelf == caster)
                            {
                                continue;
                            }
                        }
                    }

                    

                   if (target.TryGetComponent<CharacterUnit>(out var unit) ||
                       target.transform.parent?.TryGetComponent<CharacterUnit>(out unit) == true)
                   {
                        CalculateDamage(caster, spell, unit);
                       
                   }


                    //Console.Error("HITS", damage);
                    //if (target.TryGetComponent<CharacterUnit>(out var unit) ||
                    //                                                           target.transform.parent?.TryGetComponent<CharacterUnit>(out unit) == true) 
                    //{
                    //    CalculateDamage(caster, spell, target)
                    //    unit.stats.HP -= damage;
                    //    Console.Error($"{caster.unitName} hit {unit.unitName} for {damage} damage.");
                    //}
                }
            }
            else
            {
                Debug.Log("ApplySpell::No hits");
            }

            // SFX - finish
            gameManager.soundPlayer.PlayClipAtPoint(spell.sfxOnImpact, targetPosition);

            onImpactComplete?.Invoke();
        });       
    }

    private void CalculateDamage(CharacterUnit caster, Spell spell, CharacterUnit target)
    {
        damageCalculator.CalculateDamage(new DamageContext { Caster = caster, Spell = spell, Target = target, CombatManager = this});

        // example logic - mage for now, then adds to willpower. or just maybe should be converted to main stat
        //return spell.manaCost + caster.stats.Intelligence; 
        //return (int)spell.baseDamage; // basic example damage for testing

        /* Better example 
        int baseDamage = weaponDamage * criticalChance;
        int totalDamage = baseDamage * (attack / (attack + defence);
        */

    }

    public DamageCalculator damageCalculator = new DamageCalculator();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="radius"></param>
    /// <param name="losSource">Lign of Sight</param>
    /// <returns></returns>
    private static List<GameObject> FindTargetsInRadius(Vector3 pos, float radius, LayerMask affectedLayers, Vector3? losSource = null) 
    {
        List<GameObject> validTargets = new List<GameObject>();
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
}
