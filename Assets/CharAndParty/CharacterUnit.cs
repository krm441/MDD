using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatBlock
{
    public StatBlock() { }
    public StatBlock(StatBlock copy)
    {
        this.Intelligence       = copy.Intelligence;
        this.Willpower          = copy.Willpower;
        this.Devotion           = copy.Devotion;
        this.HP                 = copy.HP;
        this.MaxHP              = copy.MaxHP;        
        this.Speed              = copy.Speed;
        this.Initiative         = copy.Initiative;
        this.ActionPoints       = copy.ActionPoints;
        this.MaxActionPoints    = copy.MaxActionPoints;
        this.StartActionPoints  = copy.StartActionPoints;
    }

    public int Intelligence;
    public int Willpower;
    public int Devotion;
    public int HP;
    public int MaxHP;

    // movement and combat
    public int Speed; // number of tiles the char can walk in one turn
    public int Initiative;
    public int ActionPoints;
    public int MaxActionPoints = 8;
    public int StartActionPoints;
}

[System.Serializable]
public class ArmorStat
{
    public ArmorStat() { }
    public ArmorStat(ArmorStat copy)
    {
        this.magicArmor         = copy.magicArmor;
        this.physicalArmor      = copy.physicalArmor;
        this.moraleLevel        = copy.moraleLevel;
    }

    public int physicalArmor;
    public int magicArmor;
    public int moraleLevel; // this one will be about Crowd Control (CC) spells like charm or madness
}

public class MeshAndPortraits
{
    public string portraitPath;
}

namespace PartyManagement
{
    public class CharacterUnit : MonoBehaviour
    {
        public bool isPlayerControlled = true;
        public bool isMainCharacter = false;

        
        public bool IsDead => attributeSet.stats.HP <= 0;

        public AttributeSet attributeSet;

        MeshAndPortraits meshAndPortraits;
        public Sprite portraitSprite;
        public string unitName;
        public SpellBook spellBook = new SpellBook();
        public Spell currentlySelectedSpell = null;
        public int LignOfSight = 10;

        public void SetActionPoints(int points)
        {
            attributeSet.stats.ActionPoints = points;
        }

        public Spell GetSelectedSpell() => currentlySelectedSpell;

        public void SelectSpell(Spell spell)
        {
            currentlySelectedSpell = spell;
        }

        public void DeselectSpell()
        {
            currentlySelectedSpell = null;
        }

        public MovementController movementController;

        /// <summary>
        /// Coroutine: Moves and deducts AP based on path length
        /// </summary>
        /// <param name="nodes">Path to move</param>
        /// <returns></returns>
        public IEnumerator MoveAlongPathRoutine(Pathfinding.Path nodes)
        {
            if (nodes == null) yield break;
            foreach (var node in nodes.pathNodes)
            {
                Vector3 target = node.worldPos;
                while (Vector3.Distance(transform.position, target) > 0.01f)
                {
                    LookAtTarget(target);
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        target,
                        attributeSet.stats.Speed * Time.deltaTime
                    );
                    yield return null;
                }
            }

            DeductActionPoints(nodes);
        }

        /// <summary>
        /// Coroutine: Moves then casts at targetPoint - then invokes onComplete
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="path"></param>
        /// <param name="targetPoint"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        public IEnumerator CastSpellWithMovement(
            CharacterUnit caster,
            CombatManager combatManager,
            Pathfinding.Path path,
            Vector3 targetPoint,
            Action onComplete
        )
        {
            // variables:
            var spell = caster.GetSelectedSpell();

            // 1- Walk first
            if (path != null)
                yield return StartCoroutine(MoveAlongPathRoutine(path));

            bool done = false; // simple bool will block the immediate return

            // 2- Cast at targetPoint when walk is over
            // onComplete callback is passed to ApplySpell in Combat manager
            combatManager.ApplySpell(this, spell, targetPoint, () =>
            {
                // only after the animation spell is over
                AimingVisualizer.DrawImpactCircle(targetPoint, spell.radius, Color.red);
                DeductActionPoints(spell.apCost);
                done = true;
                onComplete?.Invoke();
            });

            yield return new WaitUntil(() => done);
        }
    


        void Start()
        {
            // Auto-fetch if not set - need revision
            if (movementController == null)
                movementController = GetComponent<MovementController>();
        }

        private void Update()
        {
           
        }

        public void MoveAlongPath(List<Pathfinding.Node> path)
        {
            if(IsDead) return;
            movementController?.MoveAlongPath(path); 
        }

        public void MoveAlongPath(List<Vector3> path) // override using vector3 instead of Nodes
        {
            movementController?.MoveAlongPath(path);
        }

        public void StopMovement()
        {
            movementController?.StopMovement();
        }

        public void LookAtTarget(Vector3 target)
        {
            movementController?.LookAtTarget(target);
        }
       

        public void DeductActionPoints(Pathfinding.Path path)
        {
            // 1) How far did char move
            float distance = path.CalculateDistance(transform.position);

            // 2) Convert to AP cost: 1 AP per stats.Speed units moved
            float rawCost = distance / (float)attributeSet.stats.Speed;

            // 3) Round up
            int apCost = Mathf.CeilToInt(rawCost);

            // 4) Subtract apCost from ActionPoints, clamping at 0
            int newAP = Mathf.Max(0, attributeSet.stats.ActionPoints - apCost);
            SetActionPoints(newAP);
        }

        public void DeductActionPoints(int AP)
        {
            attributeSet.stats.ActionPoints -= AP;
        }

        public void AddActionPoints(int points)
        {
            attributeSet.stats.ActionPoints += points;
        }

        public void AddActionPointsStart()
        {
            if (attributeSet.stats.ActionPoints + attributeSet.stats.StartActionPoints > attributeSet.stats.MaxActionPoints)
            {
                attributeSet.stats.ActionPoints = attributeSet.stats.MaxActionPoints;
            }
            else
            {
                AddActionPoints(attributeSet.stats.StartActionPoints);
            }
        }
    }
}
