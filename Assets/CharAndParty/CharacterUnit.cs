using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatBlock
{
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
    public int physicalArmor;
    public int magicArmor;
    public int moraleLevel; // this one will be about Crowd Control (CC) spells like charm or madness
}

namespace PartyManagement
{
    public class CharacterUnit : MonoBehaviour
    {
        public bool isPlayerControlled = true;

        public StatBlock stats;
        public ArmorStat armorStat;
        public Sprite portraitSprite;
        public string unitName;
        public SpellBook spellBook = new SpellBook();
        private Spell currentlySelectedSpell = null;
        public int LignOfSight = 10;

        public void SetActionPoints(int points)
        {
            stats.ActionPoints = points;
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

        void Start()
        {
            // Auto-fetch if not set - need revision
            if (movementController == null)
                movementController = GetComponent<MovementController>();
        }

        private void Update()
        {
            if(currentlySelectedSpell != null)
            {
                // play animation
                //currentlySelectedSpell.PlayAnimation();
            }
        }

        public void MoveAlongPath(List<Pathfinding.Node> path)
        {
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

        public void AddActionPoints(int points)
        {
            stats.ActionPoints += points;
        }

        public void AddActionPointsStart()
        {
            if (stats.ActionPoints + stats.StartActionPoints > stats.MaxActionPoints)
            {
                stats.ActionPoints = stats.MaxActionPoints;
            }
            else
            {
                AddActionPoints(stats.StartActionPoints);
            }
        }

        //private Coroutine movementCoroutine;

        // For animation FSM
        //public bool IsMoving => movementCoroutine != null;


        //void Start()
        //{
            //FindObjectOfType<PartyManager>().AddMember(this);
        //}

/*
        public void MoveAlongPath(List<Pathfinding.Node> path)
        {
            // Cancel current movement, if any
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                Debug.Log("new movement");
            }

            // Start new movement
            movementCoroutine = StartCoroutine(FollowPath(path, 3f));
        }

        private IEnumerator FollowPath(List<Pathfinding.Node> path, float speed)
        {
            foreach (Pathfinding.Node node in path)
            {
                Vector3 targetPos = node.worldPos;
                targetPos.y = transform.position.y; // Prevent rotation tilt

                // Determine direction to look at (skip if already at target)
                Vector3 direction = targetPos - transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    // move
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                    yield return null;
                }
            }

            movementCoroutine = null; // Reset after movement ends
        }*/
    }
}
