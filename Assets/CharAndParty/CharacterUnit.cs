using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using UnityEngine.AI;

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
    public int maxPhysicalArmor;
    public int magicArmor;
    public int maxMagicArmor;
    public int moraleLevel = 100; // this one will be about Crowd Control (CC) spells like charm or madness
}

public class MeshAndPortraits
{
    public string portraitPath;
}

namespace PartyManagement
{
    public class CharacterUnit : MonoBehaviour
    {
        public NavMeshAgent agent;
        [SerializeField] private Animator animator;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = 1.5f;

            EnsureAgentIsOnNavMesh();
        
            // Auto-fetch if not set - need revision
            //if (movementController == null)
            //    movementController = GetComponent<MovementController>();
            //
            //gridSystem = FindObjectOfType<Pathfinding.GridSystem>();
            //
            //Vector2Int currentGridPos = gridSystem.GetNodeFromWorldPosition(transform.position).gridPos;
            //lastGridPos = currentGridPos;
            //gridSystem.MarkOccupied(currentGridPos, unitID, true); // Mark initial tile as occupied
        }

        public static int unitIDCounter = -1;

        public int unitID = 0;
        private void Awake()
        {
            unitID = ++unitIDCounter;
        }

        public IEnumerator MoveTo(Vector3 targetPos)
        {
            agent.SetDestination(targetPos);
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }
            agent.ResetPath();
        }

        public IEnumerator PressButtonAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("PressButton");
            }
            yield return new WaitForSeconds(1.0f); // Duration of animation
        }
        void EnsureAgentIsOnNavMesh()
        {
            if (agent == null)
            {
                Debug.LogError("Agent is null!");
                return;
            }

            if (!agent.isOnNavMesh)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(agent.transform.position, out hit, 2f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);  // Snap agent to valid NavMesh location
                    Debug.Log("Agent warped to NavMesh.");
                }
                else
                {
                    Debug.LogWarning("Could not find valid NavMesh position near agent.");
                }
            }

        }

        public Vector3 GetChestPos()
        {
            return transform.position + new Vector3(0, 1.5f, 0);
        }

        public Vector3 GetFeetPos()
        {
            return transform.position + new Vector3(0, .6f, 0); // slightly above the mesh
        }

        public bool isPlayerControlled = true;
        public bool isMainCharacter = false;

        public Vector3 GetSpellSpawnLocation()
        {
            return transform.position + new Vector3(0, 1.5f, 0);
        }
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
        public IEnumerator MoveAlongPathRoutine(Pathfinding.Path nodes, float margin = 1f)
        {
            if (nodes == null) yield break;
            foreach (var node in nodes.pathNodes)
            {
                Vector3 target = node.worldPos;
                target.y = transform.position.y; // set y to normal level
                while (Vector3.Distance(transform.position, target) > margin) //0.01
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
            Vector3 targetImpactVFXPoint,
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
                AimingVisualizer.DrawImpactCircle(targetImpactVFXPoint, spell.radius, Color.red);
                DeductActionPoints(spell.apCost);
                done = true;
                onComplete?.Invoke();
            });

            yield return new WaitUntil(() => done);
        }

        private Vector2Int? lastGridPos = null;
        private Pathfinding.GridSystem gridSystem;

        public float footprintWidth = 1f;  
        public float footprintHeight = 1f;
        public List<Vector2Int> lastFootprint = new List<Vector2Int>();

        public List<Vector2Int> GetFootprintGridPositions(Vector3 centerWorldPos)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            foreach (var pt in ComputeFootprintPoints(centerWorldPos))
            {
                Node n = gridSystem.GetNodeFromWorldPosition(pt);
                if (!result.Contains(n.gridPos))
                    result.Add(n.gridPos);
            }
            return result;
        }

        List<Vector3> ComputeFootprintPoints(Vector3 center)
        {
            float w = footprintWidth * 0.5f;
            float h = footprintHeight * 0.5f;
            List<Vector3> points = new List<Vector3>();
            float step = 0.5f; 

            for (float x = -w; x <= w; x += step)
            {
                for (float z = -h; z <= h; z += step)
                {
                    points.Add(center + new Vector3(x, 0, z));
                }
            }
            return points;
        }

        public List<Vector3> ComputeFootprintPoints(float step = 0.5f)
        {
            Vector3 c = transform.position + new Vector3(gridSystem.tileSize, 0, gridSystem.tileSize);

            float w = footprintWidth * 0.5f;
            float h = footprintHeight * 0.5f;
            List<Vector3> result = new List<Vector3>();

            for (float x = -w; x <= w; x += step)
            {
                for (float z = -h; z <= h; z += step)
                {
                    result.Add(c + new Vector3(x, 0, z));
                }
            }
            return result;
        }



        

        public List<Vector2Int> GetSectorCenters()
        {
            // Deltas: [N, NE, E, SE, S, SW, W, NW]
            Vector2Int[] sectorDeltas = new Vector2Int[]
            {
                new Vector2Int(0, 3),    // N
                new Vector2Int(3, 3),    // NE
                new Vector2Int(3, 0),    // E
                new Vector2Int(3, -3),   // SE
                new Vector2Int(0, -3),   // S
                new Vector2Int(-3, -3),  // SW
                new Vector2Int(-3, 0),   // W
                new Vector2Int(-3, 3)    // NW
            };

            // Get grid center under character
            Vector2Int centerGridPos = gridSystem.GetNodeFromWorldPosition(transform.position).gridPos;

            var centers = new List<Vector2Int>();
            foreach (var delta in sectorDeltas)
                centers.Add(centerGridPos + delta);

            return centers;
        }


        //void OnDrawGizmosSelected()
        //{
        //    // draw the sector centers
        //    Gizmos.color = Color.white;
        //    var centers = GetSectorCenters();
        //    foreach (var gridPos in centers)
        //    {
        //        Vector3 world = gridSystem.GetNodeFromWorldPosition(
        //            new Vector3(gridPos.x * gridSystem.tileSize, 0, gridPos.y * gridSystem.tileSize)
        //        ).worldPos + Vector3.up * 0.3f;
        //        Gizmos.DrawSphere(world, gridSystem.tileSize * 0.2f);
        //    }
        //}


        private void Update()
        {
            if (gridSystem == null) return;

            // Unmark all previously occupied nodes
            foreach (var pos in lastFootprint)
                gridSystem.MarkOccupied(pos, -1, false);

            // Compute the current footprint (corners + center)
            var points = ComputeFootprintPoints();

            // Mark all currently occupied nodes
            lastFootprint.Clear();
            foreach (var worldPt in points)
            {
                Node n = gridSystem.GetNodeFromWorldPosition(worldPt);
                if (!lastFootprint.Contains(n.gridPos))
                {
                    gridSystem.MarkOccupied(n.gridPos, unitID, true);
                    lastFootprint.Add(n.gridPos);
                }
            }
        }

        public bool wasCarved = false;
        private Vector3 positionBeforeCarve;

        public void MemorizePosition()
        {
            positionBeforeCarve = transform.position;
        }

        public void ReturnMemorizedPosition()
        {
            transform.position = positionBeforeCarve;
        }

        public void Carve()
        {
            wasCarved = true;
            MemorizePosition();
            agent.GetComponent<NavMeshAgent>().enabled = false;
            agent.GetComponent<NavMeshObstacle>().enabled = true;
        }

        public void Uncarve()
        {
            agent.GetComponent<NavMeshObstacle>().enabled = false;
            agent.GetComponent<NavMeshAgent>().enabled = true;
            if (agent.isOnNavMesh && agent.enabled)
            {
                agent.isStopped = true;
            }
            wasCarved = false;
            ReturnMemorizedPosition();
        }

        public IEnumerator WaitForMovement()
        {
            while (movementController != null && movementController.IsMoving)
                yield return null;
        }


        public void MoveAlongPath(List<Pathfinding.Node> path)
        {
            if(IsDead) return;
            movementController?.MoveAlongPath(path); 
        }

        public void MoveAlongPath(List<Vector3> path) // override using vector3 
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
