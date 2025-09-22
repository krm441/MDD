using System;
using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;
using UnityEngine.AI;
using System.IO;
using static UnityEngine.UI.CanvasScaler;
using System.Linq;

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

/// <summary>
/// Uset for save/load
/// </summary>
[System.Serializable]
public class CharacterMetaData
{
    public string unitName;
    public bool isMainCharacter = false;
    public string portraitPrefabName = "DefaultPortrait";
    public AttributeSet attributeSet;
    public List<string> spells = new List<string>();
    public string rigMeshName = "DefaultCapsule";

    public CharacterMetaData() { }

    public CharacterMetaData(CharacterMetaData other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));

        unitName = other.unitName;
        isMainCharacter = other.isMainCharacter;
        portraitPrefabName = other.portraitPrefabName;
        rigMeshName = other.rigMeshName;

        // Deepcopy spells list
        spells = other.spells != null ? new List<string>(other.spells) : new List<string>();

        attributeSet = other.attributeSet != null ? new AttributeSet(other.attributeSet) : null;
    }
}

public static class CharacterMetaDataLoader
{

    public static CharacterMetaData Load(string charName)
    {
        //string path = System.IO.Path.Combine(Application.persistentDataPath, charName + ".json");
        //string path = "Assets/Resources/Data/" + charName + ".json";
        //Console.Error(path);

        //var json = File.ReadAllText(path);
        var json = Resources.Load<TextAsset>("Data/" + charName);
        var data = JsonUtility.FromJson<CharacterMetaData>(json.text);
        if (data == null) throw new InvalidOperationException("Failed to parse CharacterMetaData JSON.");
        Initialize(data);
        return data;
    }

    private static void Initialize(CharacterMetaData m)
    {
        if (m.attributeSet == null) m.attributeSet = new AttributeSet();
        if (m.attributeSet.stats == null) m.attributeSet.stats = new StatBlock();
        if (m.attributeSet.armorStat == null) m.attributeSet.armorStat = new ArmorStat();
        if (m.attributeSet.resistances == null) m.attributeSet.resistances = new DamageResistenceContainer();
        if (m.spells == null) m.spells = new System.Collections.Generic.List<string>();
    }
}

namespace PartyManagement
{
    public enum CharacterUnitStatus
    {
        Alive,
        Dead,
        Unavaliable,
    }

    public class CharacterUnit : MonoBehaviour
    {
        public CharacterUnitStatus status = CharacterUnitStatus.Alive;
        public bool isMyTurn = false;
        public NavMeshAgent agent;

        int IsMovingHash;
        [SerializeField] public Animator animator;

        [Header("Weapon")] public Weapon weapon = new Weapon();

        static readonly int MeleeSliceHash = Animator.StringToHash("Base Layer.Dualwield_Melee_Attack_Slice");
        static readonly int MeleeStabHash = Animator.StringToHash("Base Layer.2H_Melee_Attack_Stab");
        static readonly int ShootHash = Animator.StringToHash("Base Layer.1H_Ranged_Shoot");




        public NPCParty parentParty;

        public string rigMeshName;

        // save/ load
        public CharacterMetaData CaptureState()
        {
            var ret = new CharacterMetaData();
            ret.attributeSet = new AttributeSet(this.attributeSet);
            ret.isMainCharacter = this.isMainCharacter;
            ret.spells = new List<string>();

            foreach(var spell in this.spellBook.GetAllSpells())
            {
                ret.spells.Add(spell.name);
            }

            ret.portraitPrefabName = this.portraitSprite.name; // name is guaranteed to be saved
            ret.unitName = this.name;
            ret.rigMeshName = this.rigMeshName;

            return ret;
        }


        void Start()
        {
            // animation
            IsMovingHash = Animator.StringToHash("IsMoving");

            agent = GetComponent<NavMeshAgent>();
            agent.stoppingDistance = 1.5f;

            EnsureAgentIsOnNavMesh();

            capsuleCollider = GetComponentInChildren<CapsuleCollider>();

        }

        private CapsuleCollider capsuleCollider;

        public float GetRadius() => capsuleCollider.radius;

        public void WalkTo(Vector3 targetPos, float stopingDistance = 0.0f, Action onComplete = null)
        {
            StartCoroutine(MoveTo(targetPos, stopingDistance, onComplete));
        }

        public void StopMovement()
        {
            StopAllCoroutines();
            agent.ResetPath();
            StopAnimationWalking();
        }

        public void Melee(Vector3 targetPos, Action onImpact = null)
        {
            LookAtTarget(targetPos);
            meleeCoroutine = StartCoroutine(MeleeRoutine(onImpact));
        }

        public void MeleeAoE(Vector3 targetPos, Action onImpact = null)
        {
            LookAtTarget(targetPos);
            meleeCoroutine = StartCoroutine(MeleeAoERoutine(onImpact));
        }

        public void Ranged(Vector3 targetPos, Action onImpact = null)
        {
            LookAtTarget(targetPos);
            rangedCoroutine = StartCoroutine(RangedRoutine(onImpact));
        }

        IEnumerator RangedRoutine(Action onImpact)
        {
            PlayRangedAnimation();

            while (animator.IsInTransition(0))
                yield return null;

            DeductMeleeAPCost();

            rangedCoroutine = null;

            onImpact?.Invoke();
        }

        IEnumerator MeleeRoutine(Action onImpact)
        {
            yield return PlayMeleeAnimation();

            //while (animator.IsInTransition(0))
            //    yield return null;
            //
            //float deathTime = animator.runtimeAnimatorController
            //    .animationClips
            //    .First(c => c.name == "Death_A")
            //    .length;
            //
            //animator.Play("Death_A");
            //
            //animationCurrentRoutine = StartCoroutine(WaitAndInvoke(deathTime, onComplete));

            //DeductMeleeAPCost();

            meleeCoroutine = null;

            onImpact?.Invoke();
        }

        IEnumerator MeleeAoERoutine(Action onImpact)
        {
            PlayMeleeAoeAnimation();

            while (animator.IsInTransition(0))
                yield return null;

            //DeductMeleeAPCost();

            meleeCoroutine = null;

            onImpact?.Invoke();
        }

        private void StartAnimationWalking()
        {
            // start animation
            if (animator != null) animator.SetBool(IsMovingHash, true);
        }

        private IEnumerator PlayMeleeAnimation()
        {
            float time = 0.5f;
            if (weapon.type == WeaponType.Melee_Slice)
            {
                time = animator.runtimeAnimatorController
                .animationClips
                .First(c => c.name == "Dualwield_Melee_Attack_Slice")
                .length;

                animator.Play(MeleeSliceHash);

            }
            else if (weapon.type == WeaponType.Melee_Stab)
            {
                time = animator.runtimeAnimatorController
                .animationClips
                .First(c => c.name == "2H_Melee_Attack_Stab")
                .length;

                animator.Play(MeleeStabHash);
            }
            yield return WaitAndInvoke(time, null);
        }

        private void PlayMeleeAoeAnimation()
        {
            animator.Play("2H_Melee_Attack_Spin");
        }

        private void PlayRangedAnimation()
        {
            animator.Play(ShootHash);
        }

        private void StopAnimationWalking()
        {
            // stop animation
            if (animator != null) animator.SetBool(IsMovingHash, false);
        }

        public IEnumerator MoveTo(Vector3 targetPos, float stopingDistance = 0.0f, Action onComplete = null)
        {
            StartAnimationWalking();

            agent.isStopped = false;
            agent.stoppingDistance = stopingDistance;
            agent.SetDestination(targetPos);
            while (agent.pathPending || agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }
            agent.ResetPath();

            StopAnimationWalking();

            onComplete?.Invoke();
        }

        public void WalkToWithAp(Vector3 targetPos, float stopingDistance = 0.0f)
        {
            StartCoroutine(MoveWithApMeter(targetPos, 3f, stopingDistance));
        }
        
        public IEnumerator MoveWithApMeter(Vector3 destination, float navSpeed, float stoppingDistance = 0f,
            Action onComplete = null, Action onImpact = null)
        {
            StartAnimationWalking();

            int remainingAP = attributeSet.stats.ActionPoints;
            float costPerUnit = attributeSet.stats.Speed;

            // cache default stopping distance
            float defaultStoppingDistance = agent.stoppingDistance;
            agent.stoppingDistance = stoppingDistance; // push new stop dist

            // save current pos
            Vector3 current = transform.position;

            float accumulator = 0f; // accumulate traversed path

            // start motion
            agent.isStopped = false;
            agent.SetDestination(destination);

            yield return new WaitUntil(() => !agent.pathPending && agent.hasPath);

            var hasPath = agent.hasPath;
            var pathPending = agent.pathPending;
            var remDist = agent.remainingDistance;

            attributeSet.stats.ActionPoints -= 1;// first time ap taken as a cost for first move

            while (agent.hasPath && //&& !agent.pathPending
                agent.remainingDistance >= agent.stoppingDistance + 0.02)
            {
                // add traversed distance
                accumulator += Vector3.Distance(current, transform.position);
                current = transform.position;

                if (accumulator >= costPerUnit)
                {
                    remainingAP -= 1;
                    attributeSet.stats.ActionPoints -= 1;
                    accumulator = 0f;

                    if (remainingAP <= 0f)
                    {
                        agent.isStopped = true;
                        agent.ResetPath();
                        break;
                    }
                }

                yield return null;
            }

            // restore
            agent.stoppingDistance = defaultStoppingDistance;

            // cler visuals
            AimingVisualizer.ClearPathPreview();

            StopAnimationWalking();
            agent.isStopped = true;

            onComplete?.Invoke();
            onImpact?.Invoke();
        }

        Coroutine meleeCoroutine, rangedCoroutine;

       


        public void WalkAndMelee(Vector3 targetPos, float stopingDistance = 2.5f, Action onImpact = null)
        {
            StartCoroutine(MoveWithApMeter(targetPos, 3f, stopingDistance, ()=>
            {
                DeductMeleeAPCost();
                
               PlayMeleeAnimation();
            }, onImpact));
        }

        private void DeductMeleeAPCost()
        {
            attributeSet.stats.ActionPoints -= weapon.apCost;
        }

        public IEnumerator PressButtonAnimation()
        {
            if (animator != null)
            {
                animator.SetTrigger("PressButton");
            }
            yield return new WaitForSeconds(1.0f); // Duration of animation
        }
        public void EnsureAgentIsOnNavMesh()
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

        Coroutine animationCurrentRoutine = null;
        public void PlayDeathAnimation(Action onComplete = null)
        {
            if (animationCurrentRoutine != null) StopCoroutine(animationCurrentRoutine);

            float deathTime = animator.runtimeAnimatorController
                .animationClips
                .First(c => c.name == "Death_A")
                .length;

            animator.Play("Death_A");

            animationCurrentRoutine = StartCoroutine(WaitAndInvoke(deathTime, onComplete));
        }

        private static IEnumerator WaitAndInvoke(float waitingTime, Action onComplete)
        {
            yield return new WaitForSeconds(waitingTime);
            onComplete?.Invoke();
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
        /*public IEnumerator CastSpellWithMovement(
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
            combatManager.ApplySpell(gameManager, this, spell, targetPoint, () =>
            {
                // only after the animation spell is over
                AimingVisualizer.DrawImpactCircle(targetImpactVFXPoint, spell.radius, Color.red);
                DeductActionPoints(spell.apCost);
                done = true;
                onComplete?.Invoke();
            });

            yield return new WaitUntil(() => done);
        }*/

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

       //public void StopMovement()
       //{
       //    //movementController?.StopMovement(); // obsolete
       //
       //    agent.isStopped = true;
       //}

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
