using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;



public class CombatManager //: MonoBehaviour
{
    /// <summary>
    /// Casts the spell that was selected
    /// </summary>
    public static void CastCurrentSpell()
    {
        CharacterUnit caster = PartyManager.CurrentSelected;
        Spell spell = caster.GetSelectedSpell();
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        // spell animation
        if (Physics.Raycast(ray, out RaycastHit hit_b, 100f))
        {
            Vector3 hoverPoint = hit_b.point;
            AimingVisualizer.ShowAimingCircle(hoverPoint, spell.radius);
            AimingVisualizer.HighlightTargets(hoverPoint, spell.radius);
        }

        if (Input.GetMouseButtonDown(0))
        {
            AimingVisualizer.Hide();

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (spell == null)
                {
                    Debug.LogWarning("No spell selected.");
                    return;
                }

                float dist = Vector3.Distance(caster.transform.position, hit.point);
                if (dist > spell.range)
                {
                    Debug.Log("Target out of range.");
                    return;
                }

                ApplySpell(caster, spell, hit.point);
                AimingVisualizer.DrawImpactCircle(hit.point, spell.radius);

                // Reset casting state
                caster.DeselectSpell();
                GameManagerMDD.interactionSubstate = InteractionSubstate.Default;
            }
        }
    }

    public static void ApplySpell(CharacterUnit caster, Spell spell, Vector3 targetPosition)
    {
        Debug.Log("SPELL FIRE");

        SpellVisualEffectsManager.LaunchSpellVFX(spell, caster, targetPosition, () =>
        {
            var targets = FindTargetsInRadius(targetPosition, spell.radius);
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
