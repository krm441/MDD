using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using static UnityEngine.UI.CanvasScaler;
using System.Linq;
using UnityEngine.Analytics;
using System.Reflection;
using UnityEngine.AI;
using Pathfinding;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.Assertions;
using DelaunatorSharp;

/*
 
spell:
select spell, aim, cast
deduct ap, calculate damage
spell type: point, aoe, chain

melee:
point on enemy and click, calculate avaliable ap, if enough: walk and melee, play hitting
animation, play being hit animation, deduct ap, calculate damage

Arrow ranged:
hover over enemy, calculate avaliable ap, if enough: shoot Arrow, play shooting animation, play hitting aniamtion, deduct ap, calculate damage

OnSpell(caster, spell):
	if spell.isAoe:
		Visulalizer.ShowAreaImpact();
		Visualizer.HighlightTargets();
		targets = Visualizer.GetPotentialTargets();
	else // point spell
		Visualiser.HighlightTarget(char);

	if(click)
		yield caster.PlaySpellAnimation(spell, onImpact, onFinish);
		    onImpact()
			    if spell.isAoE:
				    DamageCalculator.CalculateDamage(targets, spell);
			    else
				    DamageCalculator.CalculateDamage(target, spell);

		    onFinish()
			    targets.PlayOnHitAnimation();
			    Visualizer.ShowHelperText();

OnWeapon(caster, weapon):
		Visualiser.HighlightTarget(char);

	if(click)
        if weapon.isRanged == false
		    yield WalkToTarget(can stop = true, onImpact, onFinish)
                onImpact()=>
                    caster.PlayMeleeAnimation();
                    DamageCalculator.CalculateDamage(target, weapon)                    
                onFinish()=>
                    target.PlayOnHitAnimation();
                    Visualizer.ShowHelperText();

        else
            caster.shoot(weapon, onImpact, onFinish)
                onImpact()=>
                    DamageCalculator.CalculateDamage(target, spell)
                onFinish()=>
                    target.PlayOnHitAnimation();
                    Visualizer.ShowHelperText();
 
 */

/// <summary>
/// Substate also needs a standalone FSM
/// Interface class for substate: casting, movement etc...
/// </summary>
public interface ISubstate
{
    void Enter();
    void Update();
    void Exit();

    //void HandleButtonEvent(EventSystemMDD.ButtonEvent customEvent);

    InteractionSubstate Type { get; }

    SpellCastingAnimationStates SpellcastingAnimationState { get; set; }
}

public abstract class SubStateBase : ISubstate
{
    protected GameManagerMDD gameManager;
    //protected PartyManager partyManager;
    protected PartyPlayer partyManager;

    public SubStateBase(GameManagerMDD manager)
    {
        gameManager = manager;
        partyManager = gameManager.partyManager;
        if (partyManager == null)
        {
            partyManager = GameObject.FindObjectOfType<PartyPlayer>();
            Assert.IsNotNull(partyManager, "SubStateBase::SubStateBase: Assign 'PartyPlayer' in scene");
        }
    }

    protected InteractionSubstate substate;

    public virtual InteractionSubstate Type => substate;

    public virtual void Enter() { EventSystemMDD.EventSystemMDD.ButtonClick += HandleButtonEvent; }
    public virtual void Update() { }
    public virtual void Exit() { EventSystemMDD.EventSystemMDD.ButtonClick -= HandleButtonEvent; }

    protected virtual void HandleButtonEvent(EventSystemMDD.ButtonEvent customEvent) { }

    public SpellCastingAnimationStates SpellcastingAnimationState { get; set; }
}

public class MovementSubstate : SubStateBase
{
    public MovementSubstate(GameManagerMDD manager) : base(manager) 
    {
        substate = InteractionSubstate.Default;
    }

    protected override void HandleButtonEvent(EventSystemMDD.ButtonEvent customEvent)
    {
        if (customEvent.isConsumed == true) return;

        switch (customEvent.eventType)
        {
            case EventSystemMDD.EventType.CharPortratClick:
                {
                    customEvent.Consume();
                    partyManager.SelectMember(customEvent.targetUnit);
                    //UnityEngine.Object.FindObjectOfType<IsometricCameraController>().SnapToCharacter(customEvent.targetUnit.transform);
                    UnityEngine.Object.FindObjectOfType<IsometricCameraController>().LerpToCharacter(customEvent.targetUnit.transform);
                    UnityEngine.Object.FindObjectOfType<SpellMap>().BuildIconBar(customEvent.targetUnit, gameManager);
                }
                break;

            case EventSystemMDD.EventType.SpellClick:
                {
                    customEvent.Consume();
                    var unit = customEvent.targetUnit;
                    unit.StopMovement();
                    unit.SelectSpell(customEvent.spell);

                    gameManager.GetCurrentState().SetCastingSubState();
                }
                break;

            default: 
                break;
        }               
    }

    public override void Enter()
    {
        base.Enter();

        Console.Log("Entered Movement Substate");
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        if(statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "SubStatus: Movement";
        }

        partyManager.GetIntoFormation();

        cm = GameObject.FindObjectOfType<CursorManager>();
    }

    PathVisualiser pathVisualiser;// = new PathVisualiser();
    NavMeshPath currentPath = null;

    static readonly RaycastHit[] raycastHits = new RaycastHit[32];
    int mask = LayerMask.GetMask("Interactables", "FriendlyNPCs", "PartyLayer", "Walkable", "Obstacles", "HostileNPCs");
    // helper for painter's sorting
    private static readonly IComparer<RaycastHit> HitDistanceComparer =
    Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    private CursorManager cm;

    public override void Update() 
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (partyManager.CurrentSelected == null || !partyManager.CurrentSelected.isPlayerControlled) return;

        var agent = partyManager.CurrentSelected.agent;

        partyManager.GetIntoFormation();

        if (Input.GetMouseButtonDown(1)) // Cancel with right-click
        {
            agent.isStopped = true;
            agent.ResetPath();
            //pathVisualiser.Reset();
            return;
        }

        //cm.ShowLabel("Test");

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int count = Physics.RaycastNonAlloc(ray, raycastHits, 100f, mask, QueryTriggerInteraction.Ignore);

        // sort by distance (aka Painter's apgorithm)
        System.Array.Sort<RaycastHit>(raycastHits, 0, count, HitDistanceComparer);

        //if(count > 0) //AimingVisualizer.ManageCursor(raycastHits[0].collider.gameObject.layer);
        bool isReachable = false;
        if (count > 0)
        {
            var h = raycastHits[0];
            var go = h.collider.gameObject;

            if (go.layer == LayerMask.NameToLayer("Walkable"))
            {
                var from = partyManager.CurrentSelected.transform.position;
                var area = partyManager.CurrentSelected.agent.areaMask;      
                isReachable = PathReachability.CanReach(from, h.point, area);
            }

            AimingVisualizer.ManageCursor(cm, go.layer, isReachable, partyManager.CurrentSelected.weapon.type == WeaponType.Ranged);
        }

        for (int i = 0; i < count; i++)
        {
            var h = raycastHits[i];
            var go = h.collider.gameObject;

            if (Input.GetMouseButtonDown(0))
            {
                // if an obstacle appears before anything, it should block the click
                if (go.layer == LayerMask.NameToLayer("Obstacles"))
                {
                    break;
                }

                if (go.TryGetComponent<IInteractable>(out var interactable))
                {
                    gameManager.GetCurrentState().SetSubstate(new ObjectInteractionSubstate(gameManager, interactable));
                    break;
                }

                var cu = go.GetComponentInParent<CharacterUnit>();
                if (cu && (go.layer == LayerMask.NameToLayer("FriendlyNPCs") || go.layer == LayerMask.NameToLayer("PartyLayer")))
                {
                    if (cu == partyManager.CurrentSelected) break; // block talking to self

                    gameManager.GetCurrentState().SetSubstate(new DialogueSubState(partyManager.CurrentSelected, cu, gameManager));
                    break;
                }

                if (go.layer == LayerMask.NameToLayer("Walkable") && isReachable)
                {
                    partyManager.CurrentSelected.WalkTo(h.point);
                    AimingVisualizer.SpawnClickMarker(h.point - new Vector3(0.5f, 0, 0.5f));
                    break;
                }
            }

            /*
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Obstacles"))) return;


            currentPath = new NavMeshPath();

            if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("PartyLayer")))
            {
                // Switch to party chat layer
                if (NavMesh.CalculatePath(agent.transform.position, hit.point, NavMesh.AllAreas, currentPath))
                {
                    if (currentPath.status == NavMeshPathStatus.PathComplete)
                    {
                        // Set selected object to interact with
                        var targetChar = hit.collider.GetComponentInParent<CharacterUnit>(); // walks through tree and gets the CharacterUnit
                        if (targetChar != null)
                        {
                            Console.Log("INTERACTING:", targetChar.name);
                            //partyManager.CurrentSelected.MoveTo(targetChar.transform.position);
                            gameManager.GetCurrentState().SetSubstate(new DialogueSubState(partyManager.CurrentSelected, targetChar, gameManager));
                            return;
                        }
                    }
                }
            }

            if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("FriendlyNPCs")))
            {
                // Switch to party chat layer
                if (NavMesh.CalculatePath(agent.transform.position, hit.point, NavMesh.AllAreas, currentPath))
                {
                    if (currentPath.status == NavMeshPathStatus.PathComplete)
                    {
                        // Set selected object to interact with
                        var targetChar = hit.collider.GetComponentInParent<CharacterUnit>(); // walks through tree and gets the CharacterUnit
                        if (targetChar != null)
                        {
                            Console.Error("INTERACTING NPC:", targetChar.name, targetChar.transform.position);
                            gameManager.GetCurrentState().SetSubstate(new DialogueSubState(partyManager.CurrentSelected, targetChar, gameManager));
                            return;
                        }
                    }
                }
            }

            if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Interactables")))
            {
                if (NavMesh.CalculatePath(agent.transform.position, hit.point, NavMesh.AllAreas, currentPath))
                {
                    if (currentPath.status == NavMeshPathStatus.PathComplete)
                    {
                        // Set selected object to interact with
                        var CurrentInteractable = hit.collider.gameObject;

                        // Switch substate to interaction
                        if (CurrentInteractable.TryGetComponent<IInteractable>(out var target))
                        {
                            //target.Interact(partyManager.CurrentSelected);
                            gameManager.GetCurrentState().SetSubstate(new ObjectInteractionSubstate(gameManager, target));
                        }
                        return;
                    }
                }
            }
            if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Walkable")))
            {
                // Try to calculate path to clicked point
                if (NavMesh.CalculatePath(agent.transform.position, hit.point, NavMesh.AllAreas, currentPath))
                {
                    if (currentPath.status == NavMeshPathStatus.PathComplete)
                    {
                        //agent.SetPath(currentPath);
                        partyManager.CurrentSelected.WalkTo(hit.point);
                        AimingVisualizer.SpawnClickMarker(hit.point - new Vector3(0.5f, 0, 0.5f));
                    }
                }
            }
            */
        }
        pathVisualiser?.PreviewPath(currentPath);
       
    }

    private List<Vector3> GetFormationTargets(Vector3 leaderTarget, Vector3 leaderForward)
    {
        List<Vector3> formationTargets = new List<Vector3>();
        Vector3 right = Vector3.Cross(Vector3.up, leaderForward).normalized;

        float spacing = 2f; // distance between characters

        // Rhombus formation:

        //     Leader
        // F1    F3    F2
        //       F4


        // F1: back-left
        formationTargets.Add(leaderTarget - leaderForward * spacing + right * -spacing);
        // F2: back-right
        formationTargets.Add(leaderTarget - leaderForward * spacing + right * spacing);
        // F3: directly behind
        formationTargets.Add(leaderTarget - leaderForward * spacing * 1.5f);
        // F4: double back
        formationTargets.Add(leaderTarget - leaderForward * spacing * 2f);

        return formationTargets;
    }

    /*private IEnumerator FollowPartyTogether(Vector3 leaderTarget, List<Pathfinding.Node> path_leader)
    {
        // set leader to target
        if(path_leader != null) partyManager.CurrentSelected.MoveAlongPath(path_leader);

        var party = partyManager.GetParty();
        var leader = partyManager.CurrentSelected;
        Vector3 leaderForward = (leaderTarget - leader.transform.position).normalized;

        // Followers should look at a forward point, 13 units ahead of the leader's destination
        Vector3 sharedLookTarget = leaderTarget + leaderForward * 13f;

        // Get follower positions behind leader
        List<Vector3> formationTargets = GetFormationTargets(leaderTarget, leaderForward);

        // This variable is used to know whom to apply lookAt to target direction
        List<PartyManagement.CharacterUnit> activeFollowers = new List<PartyManagement.CharacterUnit>();


        // 
        int followerIndex = 0;

        foreach (var follower in party)
        {
            if (follower == leader ) continue;

            if (followerIndex >= formationTargets.Count)
                break;

            Vector3 followerGoal = formationTargets[followerIndex];
            followerIndex++;

            var path = gameManager.gridSystem.FindPathTo(followerGoal, follower.transform.position, follower.unitID, -1);
            if (path != null)
            {
                follower.MoveAlongPath(path);
                activeFollowers.Add(follower);
            }
        }

        // Wait until all followers have stopped moving
        yield return new WaitUntil(() => AllFollowersStopped(activeFollowers));

        // Apply look at direction of leader
        foreach (var follower in party)
        {
            follower.LookAtTarget(sharedLookTarget);
        }
    }*/

    /// <summary>
    /// For early return: checks if party is in formation
    /// </summary>
    /// <param name="leaderTarget"></param>
    /// <param name="leaderForward"></param>
    /// <returns></returns>
    /*private bool IsPartyInFormation(float maxDistance = 4f)
    {
        var party = partyManager.GetParty();

        for (int i = 0; i < party.Count; i++)
        {
            for (int j = i + 1; j < party.Count; j++)
            {
                float distance = Vector3.Distance(party[i].transform.position, party[j].transform.position);
                if (distance > maxDistance)
                    return false;
            }
        }

        return true;
    }*/

    /*public void GetIntoFormation()
    {
        if (IsPartyInFormation()) // early return
        {
            return;
        }

        List<CharacterUnit> party = partyManager.GetParty();
        var leader = partyManager.CurrentSelected;

        Vector3 leaderForward = leader.transform.forward;
        Vector3 leaderTarget = leader.transform.position;
        Vector3 sharedLookTarget = leaderTarget + leaderForward * 13f;



        List<Vector3> formationTargets = GetFormationTargets(leaderTarget, leaderForward);

        int followerIndex = 0;
        foreach (var follower in party)
        {
            if (follower == leader) continue;
            if (followerIndex >= formationTargets.Count) break;

            Vector3 targetPos = formationTargets[followerIndex++];
            follower.agent.isStopped = false;
            follower.agent.SetDestination(targetPos);
            //NavMeshHit navHit;
            //if (NavMesh.SamplePosition(targetPos, out navHit, 2f, NavMesh.AllAreas))
            //{
            //    var destination = 
            //    Console.Log(destination);
            //}
            //var path = gameManager.gridSystem.FindPathTo(targetPos, follower.transform.position, follower.unitID, -1);
            //if (path != null)
            //{
            //    follower.MoveAlongPath(path);
            //}
        }

        //gameManager.StartCoroutine(LookAtRoutine(party, leader, sharedLookTarget));
    }*/

    private IEnumerator LookAtRoutine(List<CharacterUnit> party, CharacterUnit leader, Vector3 sharedLookTarget)
    {
        yield return new WaitUntil(() =>
            party.Where(u => u != leader).All(u => u.movementController != null && !u.movementController.IsMoving)
        );

        foreach (var follower in party)
        {
            if (follower != leader)
                follower.LookAtTarget(sharedLookTarget);
        }
    }


    private bool AllFollowersStopped(List<PartyManagement.CharacterUnit> followers)
    {
        foreach (var unit in followers)
        {
            if (unit.movementController != null && unit.movementController.IsMoving)
                return false;
        }
        return true;
    }

    public override void Exit()
    {   
        base.Exit();

        partyManager.CurrentSelected.agent.isStopped = true;
        partyManager.CurrentSelected.agent.ResetPath();


        Console.Log($"{partyManager.CurrentSelected.unitName} ends their turn.");
        partyManager.CurrentSelected.StopMovement();
        //AimingVisualizer.ClearState();
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }    
}

public class DialogueSubState : SubStateBase
{
    private CharacterUnit initiator;
    private CharacterUnit target;

    private bool walkToTalk;

    private DialogueUIController dialogueController;

    public DialogueSubState(CharacterUnit initiator, CharacterUnit target, GameManagerMDD manager, bool walkToTalk = true) : base(manager) 
    {
        this.initiator = initiator;
        this.target = target;
        this.walkToTalk = walkToTalk;
    }

    public override void Enter() 
    {
        base.Enter();
        gameManager.CreateCoroutine("walkAndTalk", WalkAndTalk());
    }

    public override void Update()
    {
        if(Input.GetMouseButtonDown(1))
        {
            gameManager.RemoveCoroutine("walkAndTalk");
            gameManager.GetCurrentState().SetMovementSubState();
        }
    }

    public override void Exit()
    {
        base.Exit();
        gameManager.UIManager.HideDialogueUI();
    }
    private void OnDialogueFinished()
    {
        gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
    }

    private IEnumerator WalkAndTalk()
    {
        // 1) Move agent to the NPC
        if(walkToTalk)
        yield return initiator.MoveTo(target.transform.position, 3f);

        // 2) Dialogue
        UIManager.SetState(UIStates.Dialogue);
        
        dialogueController = gameManager.UIManager.LoadDialogueUI(initiator, target);
        dialogueController.OnDialogueFinished = OnDialogueFinished;
    }
}

public class ObjectInteractionSubstate : SubStateBase
{
    public ObjectInteractionSubstate(GameManagerMDD manager, IInteractable interactable) : base(manager)
    {
        substate = InteractionSubstate.Interaction;
        this.interactable = interactable;
    }

    private IInteractable interactable;

    public override void Enter() 
    {
        base.Enter();
        interactable.Interact(partyManager.CurrentSelected, ()=>
        {
            gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
        });

        //Vector3 interactionTarget = GetAdjacentPosition(targetObject.transform.position);
        //Vector3 interactionTarget = targetObject.transform.Find("Pivot").transform.position;
        //
        //var leader = partyManager.CurrentSelected;
        //
        //var path = gameManager.gridSystem.FindPathTo(interactionTarget, partyManager.CurrentSelected.transform.position, leader.unitID, -1);
        //
        //if (path != null)
        //{
        //    leader.LookAtTarget(targetObject.transform.position);
        //    gameManager.CreateCoroutine("interact_move", InteractWithObject(path, targetObject));
        //}
    }
    public override void Update()
    {
        

        if(Input.GetMouseButtonDown(1))
        {
            interactable.CancelInteraction(()=>
            {
                partyManager.CurrentSelected.StopMovement();
                gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
            });
            
        }

        //var agent = partyManager.CurrentSelected.agent;
        //
        //if (agent.remainingDistance <= agent.stoppingDistance)
        //{
        //    var button = targetObject.GetComponent
        //    TryActivateButton(currentTargetCollider);
        //}


        //var interactCoroutine = gameManager.GetCoroutine("interact_move");
        //
        //if (Input.GetMouseButtonDown(1)) // Cancel with right-click
        //{
        //    if (interactCoroutine?.IsRunning == true)
        //    {
        //        interactCoroutine.Stop();
        //
        //        partyManager.CurrentSelected.StopMovement();
        //
        //        // Return to movement or idle substate
        //        gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
        //
        //        AimingVisualizer.Hide();
        //    }
        //    return;
        //}
    }
    public override void Exit() { base.Exit(); }

    private Vector3 GetAdjacentPosition(Vector3 center)
    {
        // sample positions around center to find a valid walkable spot
        Vector3[] offsets = new Vector3[]
        {
        Vector3.forward,
        Vector3.back,
        Vector3.left,
        Vector3.right
        };

        foreach (var offset in offsets)
        {
            Vector3 candidate = center + offset;
            var node = gameManager.gridSystem.GetNodeFromWorldPosition(candidate);
            if (node != null && node.isWalkable)
                return node.worldPos;
        }

        // fallback: just return the center
        return center;
    }


    private IEnumerator InteractWithObject(List<Pathfinding.Node> path, GameObject interactable)
    {
        var leader = partyManager.CurrentSelected;
        leader.MoveAlongPath(path);

        yield return new WaitUntil(() => !leader.movementController.IsMoving);

        // face the object
        leader.LookAtTarget(interactable.transform.position);

       
        // Return to movement or idle substate
        gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
    }

}

public class TurnBasedMovement : SubStateBase
{
    public TurnBasedMovement(GameManagerMDD manager) : base(manager)
    {
        substate = InteractionSubstate.TurnBased;
    }

    protected override void HandleButtonEvent(EventSystemMDD.ButtonEvent customEvent)
    {
        if (customEvent.isConsumed == true) return;

        switch (customEvent.eventType)
        {
            case EventSystemMDD.EventType.SpellClick:
                {
                    if (partyManager.CurrentSelected == null || !partyManager.CurrentSelected.isPlayerControlled) return;
                    customEvent.Consume();
                    var unit = partyManager.CurrentSelected;
                    unit.StopMovement();
                    unit.SelectSpell(customEvent.spell);

                    gameManager.GetCurrentState().SetCastingSubState();
                }
                break;

            default:
                break;
        }
    }

    public override void Enter()
    {
        base.Enter();
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        if (statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "SubStatus: TurnBasedM";
        }

        // stop the agent
        //var unit = partyManager.CurrentSelected;
        //unit.GetComponent<NavMeshAgent>().enabled = true;
        //unit.GetComponent<NavMeshObstacle>().enabled = false;
        //unit.agent.isStopped = true;
        //partyManager.CurrentSelected.Uncarve();
        isCarvedResolved = false;

        currentPath = new NavMeshPath();
    }

    NavMeshPath currentPath = null;
    List<Vector3> traversablePath = null;
    private bool isHolding = false;

    static readonly RaycastHit[] raycastHits = new RaycastHit[32];
    int mask = LayerMask.GetMask("Interactables", "FriendlyNPCs", "PartyLayer", "Walkable", "Obstacles", "HostileNPCs");
    // helper for painter's sorting
    private static readonly IComparer<RaycastHit> HitDistanceComparer =
    Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));
    private CursorManager cm;

    // position return logical controll flow
    private bool isCarvedResolved = false;

    private bool EarlyReturn()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return true;
        if (partyManager.CurrentSelected == null || !partyManager.CurrentSelected.isPlayerControlled) return true;

        if (Input.GetMouseButtonDown(1)) // Cancel with right-click
        {
            var unit = partyManager.CurrentSelected;
            unit.StopMovement();
        }

        if(inAction) return true;

        return false;
    }

    private bool inAction = false;
    private bool isReachable = false;

    private void MeleeAndArrows(Vector3 hitPoint, CharacterUnit target)
    {
        if (NavMesh.CalculatePath(partyManager.CurrentSelected.agent.transform.position, hitPoint, NavMesh.AllAreas, currentPath))
        {
            if (currentPath.status == NavMeshPathStatus.PathComplete)
            {
                var unit = partyManager.CurrentSelected;
                var weapon = unit.weapon;
                Assert.IsNotNull(target);
                var from = partyManager.CurrentSelected.transform.position;
                var area = partyManager.CurrentSelected.agent.areaMask;
                var pos = target.GetFeetPos();
                isReachable = PathReachability.CanReach(from, new Vector3(pos.x, 0, pos.z), area);

                var availableAP = partyManager.CurrentSelected.attributeSet.stats.ActionPoints;
                var speed = partyManager.CurrentSelected.attributeSet.stats.Speed;

                double predictedAPcost = Math.Ceiling(MathMDD.CalculatePathDistance(currentPath) / speed);
                predictedAPcost += unit.weapon.apCost;

                if (weapon.type == WeaponType.Melee_Slice ||
                    weapon.type == WeaponType.Melee_Stab)
                {
                    var acceptableDistance = 3f;
                    if (Vector3.Distance(new Vector3(unit.GetFeetPos().x, 0, unit.GetFeetPos().z), new Vector3(pos.x, 0, pos.z)) < acceptableDistance)
                    {
                        if (Input.GetMouseButtonDown(0))
                        {
                            unit.Melee(target.GetChestPos(), () =>

                            {
                                var spell = new Spell { baseDamage = new DamageResistenceContainer { Slashing = unit.weapon.power } };
                                var damageCalculator = gameManager.CombatManager.damageCalculator;
                                damageCalculator.CalculateDamage(new DamageContext
                                {
                                    Caster = unit,
                                    Spell = spell,
                                    Target = target,
                                    CombatManager = gameManager.CombatManager
                                });
                                partyManager.CurrentSelected.attributeSet.stats.ActionPoints -= weapon.apCost;
                            });
                        }
                        AimingVisualizer.HidePathPreview();
                        return;
                    }

                    AimingVisualizer.DrawPathPreview(currentPath, (predictedAPcost <= availableAP) ? Color.green : Color.red);
                    bool canMelee = isReachable;
                    if (predictedAPcost > availableAP) canMelee = false;

                    if (Input.GetMouseButtonDown(0) && canMelee)
                    {
                        inAction = true;
                        unit.WalkAndMelee(hitPoint, 3f, () =>
                        {
                            var spell = new Spell { baseDamage = new DamageResistenceContainer { Slashing = unit.weapon.power } };
                            var damageCalculator = gameManager.CombatManager.damageCalculator;
                            damageCalculator.CalculateDamage(new DamageContext
                            {
                                Caster = unit,
                                Spell = spell,
                                Target = target,
                                CombatManager = gameManager.CombatManager
                            });
                            inAction = false;
                        });
                    }
                }
                else if (weapon.type == WeaponType.Ranged)
                {
                    AimingVisualizer.HidePathPreview();
                    var maxRange = weapon.range;

                    var dist = Vector3.Distance(target.GetChestPos(), unit.GetChestPos());
                    bool inRange = dist <= maxRange;

                    if (inRange && Input.GetMouseButtonDown(0))
                    {
                        unit.Ranged(target.GetChestPos(), () =>
                        {
                            unit.SelectSpell(unit.spellBook.GetSpell("arrow"));
                            gameManager.CreateCoroutine("casting_spell", CastingSubstate.CastSelectedSpell
                                (unit, gameManager, new Vector3(pos.x, 0, pos.z), () =>
                                {
                                    //gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
                                })
                                );
                        });
                    }
                }
            }
        }
    }

    private void Walkable(Vector3 hitPoint)
    {
        if (NavMesh.CalculatePath(partyManager.CurrentSelected.agent.transform.position, hitPoint, NavMesh.AllAreas, currentPath))
        {
            if (currentPath.status == NavMeshPathStatus.PathComplete)
            {
                var unit = partyManager.CurrentSelected;
                var from = partyManager.CurrentSelected.transform.position;
                var area = partyManager.CurrentSelected.agent.areaMask;
                isReachable = PathReachability.CanReach(from, hitPoint, area);

                var availableAP = partyManager.CurrentSelected.attributeSet.stats.ActionPoints;
                var speed = partyManager.CurrentSelected.attributeSet.stats.Speed;

                double predictedAPcost = Math.Ceiling(MathMDD.CalculatePathDistance(currentPath) / speed);

                gameManager.cursorManager.SetLable($"AP cost: {predictedAPcost}");

                AimingVisualizer.DrawPathPreview(currentPath, (predictedAPcost <= availableAP) ? Color.green : Color.red);
                if (predictedAPcost > availableAP) isReachable = false;

                if (Input.GetMouseButtonDown(0) && isReachable)
                {
                    unit.WalkToWithAp(hitPoint);
                }
            }
        }
    }

    public override void Update()
    {
        if(EarlyReturn()) return;

        // restore position - carving unity bug
        if (!isCarvedResolved)
        {
            partyManager.CurrentSelected.ReturnMemorizedPosition();
            isCarvedResolved = true;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        int count = Physics.RaycastNonAlloc(ray, raycastHits, 100f, mask, QueryTriggerInteraction.Ignore);

        // sort by distance (aka Painter's apgorithm)
        System.Array.Sort<RaycastHit>(raycastHits, 0, count, HitDistanceComparer);

        

        
        if (count > 0)
        {
            var h = raycastHits[0];
            var go = h.collider.gameObject;

            if (go.layer == LayerMask.NameToLayer("Walkable"))
            {
                Walkable(h.point);
            }

            else if (go.layer == LayerMask.NameToLayer("HostileNPCs"))
            {
                var target = go.GetComponentInParent<CharacterUnit>();
                Assert.IsNotNull(target);
                MeleeAndArrows(h.point, target);
            }

            if (cm == null)
            {
                cm = GameObject.FindObjectOfType<CursorManager>();
                Assert.IsNotNull(cm);
            }

            AimingVisualizer.ManageCursor(cm, raycastHits[0].collider.gameObject.layer, isReachable, partyManager.CurrentSelected.weapon.type == WeaponType.Ranged);
        }

        if (raycastHits.Length <= 0 || raycastHits[0].collider == null) return;

        var goo = raycastHits[0].collider.gameObject;

        if (cm == null)
        {
            cm = GameObject.FindObjectOfType<CursorManager>();
            Assert.IsNotNull(cm);
        }

        AimingVisualizer.ManageCursor(cm, goo.layer, isReachable, partyManager.CurrentSelected.weapon.type == WeaponType.Ranged);
    }

    public static IEnumerator FollowPath(CharacterUnit unit, List<Vector3> path, float navSpeed)
    {
        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerUnit = 1f / unit.attributeSet.stats.Speed;

        unit.agent.speed = navSpeed;
        unit.agent.isStopped = false;
        var defaultAcceleration = unit.agent.acceleration;
        var defaultStoppingDistance = unit.agent.stoppingDistance;
        unit.agent.acceleration = 9999f;
        unit.agent.autoBraking = false;
        unit.agent.stoppingDistance = 0f;

        Vector3 prev = unit.agent.transform.position;

        for (int i = 0; i < path.Count; i++)
        {
            var point = path[i];
            float segDist = Vector3.Distance(prev, point);
            float apCost = segDist * costPerUnit;


            // If not enough AP for full segment, move partial and stop
            if (remainingAP < apCost)
            {
                float allowedDist = remainingAP / costPerUnit;
                Vector3 partialTarget = Vector3.MoveTowards(prev, point, allowedDist);
                unit.agent.SetDestination(partialTarget);
                while (Vector3.Distance(unit.agent.transform.position, partialTarget) > unit.agent.stoppingDistance + 0.1f)
                    yield return null;

                remainingAP = 0f;
                break;
            }

            unit.agent.SetDestination(point);
            unit.LookAtTarget(point);
            while (Vector3.Distance(unit.agent.transform.position, point) > unit.agent.stoppingDistance + 0.1f)
                yield return null;

            remainingAP -= apCost;
            unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);
            prev = point;
        }

        unit.agent.autoBraking = true;
        unit.agent.isStopped = true;
        unit.agent.acceleration = defaultAcceleration;
        unit.agent.stoppingDistance = defaultStoppingDistance;
        unit.agent.ResetPath();
        unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);
        AimingVisualizer.Hide();
    }

    public static IEnumerator MoveWithApMeter1(CharacterUnit unit, Vector3 destination, float navSpeed, float stoppingDistance = 0f)
    {
        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerUnit = unit.attributeSet.stats.Speed;
        var agent = unit.agent;

        // cache default stopping distance
        float defaultStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = stoppingDistance; // push new stop dist

        // save current pos
        Vector3 current = unit.transform.position;

        float accumulator = 0f; // accumulate traversed path

        // start motion
        agent.SetDestination(destination);

        yield return new WaitUntil(() => !agent.pathPending && agent.hasPath);

        while (agent.hasPath && !agent.pathPending)
        {
            // add traversed distance
            accumulator += Vector3.Distance(current, unit.transform.position);
            current = unit.transform.position;

            if(accumulator > costPerUnit)
            {
                remainingAP -= 1f;
                unit.attributeSet.stats.ActionPoints -= 1;
                accumulator = 0f;
            }

            yield return null;
        }

        if (remainingAP <= 0f)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // restore
        unit.agent.stoppingDistance = defaultStoppingDistance;
    }

    public static IEnumerator MoveWithApMeter2(CharacterUnit unit, Vector3 destination, float navSpeed, float stoppingDistance = 0f)
    {
        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerUnit = unit.attributeSet.stats.Speed;
        var agent = unit.agent;

        float defaultStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = stoppingDistance;

        Vector3 current = unit.transform.position;
        float accumulator = 0f;

        agent.SetDestination(destination);
        yield return new WaitUntil(() => !agent.pathPending && agent.hasPath);

        while (agent.hasPath && !agent.pathPending)
        {
            // accumulate distance actually traveled this frame
            var pos = unit.transform.position;
            accumulator += Vector3.Distance(current, pos);
            current = pos;

            // deduct AP every time we cross a threshold
            while (accumulator >= costPerUnit && remainingAP > 0f)
            {
                accumulator -= costPerUnit;
                remainingAP -= 1f;
                unit.attributeSet.stats.ActionPoints -= 1;

                if (remainingAP <= 0f)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                    break; // break inner while
                }
            }

            yield return null;
        }

        // FINAL FLUSH: catch the last step that happened on the arrival frame
        while (accumulator >= costPerUnit && remainingAP > 0f)
        {
            accumulator -= costPerUnit;
            remainingAP -= 1f;
            unit.attributeSet.stats.ActionPoints -= 1;
        }

        if (remainingAP <= 0f)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        unit.agent.stoppingDistance = defaultStoppingDistance;
    }

    public static IEnumerator MoveWithApMeter3(CharacterUnit unit, Vector3 destination, float navSpeed, float stoppingDistance = 0f)
    {
        var agent = unit.agent;
        int ap = unit.attributeSet.stats.ActionPoints;   // single source of truth
        float costPerUnit = unit.attributeSet.stats.Speed;

        float defaultStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = stoppingDistance;

        Vector3 current = unit.transform.position;
        float accumulator = 0f;

        agent.SetDestination(destination);
        yield return new WaitUntil(() => !agent.pathPending && agent.hasPath);

        const float EPS = 1e-4f;

        while (agent.hasPath && !agent.pathPending)
        {
            // distance moved this frame
            accumulator += Vector3.Distance(current, unit.transform.position);
            current = unit.transform.position;

            // spend AP for every full cost unit crossed (inclusive, with epsilon)
            while (ap > 0 && accumulator + EPS >= costPerUnit)
            {
                accumulator -= costPerUnit;
                ap--;
                unit.attributeSet.stats.ActionPoints = ap; // keep stats in sync
            }

            // stop immediately when AP runs out
            if (ap <= 0)
            {
                agent.isStopped = true;
                agent.ResetPath();
                break;
            }

            yield return null;
        }

        // restore
        agent.stoppingDistance = defaultStoppingDistance;
    }

    public static IEnumerator MoveWithApMeter(CharacterUnit unit, NavMeshPath path, float navSpeed, Action onComplete, float stoppingDistance = 0f)
    {
        yield return MoveWithApMeter(unit, path.corners[path.corners.Length - 1], navSpeed, stoppingDistance);

        onComplete?.Invoke();

        /*
        return;

        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerUnit = unit.attributeSet.stats.Speed;
        var agent = unit.agent;

        // cache default stopping distance
        float defaultStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = stoppingDistance; // push new stop dist

        // save current pos
        Vector3 current = unit.transform.position;

        float accumulator = 0f; // accumulate traversed path

        // start motion
        agent.isStopped = false;
        agent.SetPath(path);

        yield return new WaitUntil(() => !agent.pathPending && agent.hasPath);

        while (agent.hasPath && !agent.pathPending &&
            agent.remainingDistance >= agent.stoppingDistance + 0.02)
        {
            // add traversed distance
            accumulator += Vector3.Distance(current, unit.transform.position);
            current = unit.transform.position;

            if (accumulator >= costPerUnit)
            {
                remainingAP -= 1f;
                unit.attributeSet.stats.ActionPoints -= 1;
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

        // Deduct final AP if any partial distance left
        if (accumulator > 0f && remainingAP > 0f)
        {
            remainingAP -= 1f;
            unit.attributeSet.stats.ActionPoints -= 1;
        }

        // restore
        unit.agent.stoppingDistance = defaultStoppingDistance;

        // cler visuals
        AimingVisualizer.ClearPathPreview();*/
    }
    public static IEnumerator MoveWithApMeter(CharacterUnit unit, Vector3 destination, float navSpeed, float stoppingDistance = 0f)
    {
        int remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerUnit = unit.attributeSet.stats.Speed;
        var agent = unit.agent;

        // cache default stopping distance
        float defaultStoppingDistance = agent.stoppingDistance;
        agent.stoppingDistance = stoppingDistance; // push new stop dist

        // save current pos
        Vector3 current = unit.transform.position;

        float accumulator = 0f; // accumulate traversed path

        // start motion
        agent.isStopped = false;
        agent.SetDestination(destination);

        yield return new WaitUntil(() => !agent.pathPending && agent.hasPath);

        var hasPath = agent.hasPath;
        var pathPending = agent.pathPending;
        var remDist = agent.remainingDistance;

        unit.attributeSet.stats.ActionPoints -= 1;// first time ap taken as a cost for first move

        while (agent.hasPath && //&& !agent.pathPending
            agent.remainingDistance >= agent.stoppingDistance + 0.02)
        {
            // add traversed distance
            accumulator += Vector3.Distance(current, unit.transform.position);
            current = unit.transform.position;

            if (accumulator >= costPerUnit)
            {
                remainingAP -= 1;
                unit.attributeSet.stats.ActionPoints -= 1;
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

        // Deduct final AP if any partial distance left
        //if (accumulator > 0f && remainingAP > 0)
        //{
        //    remainingAP -= 1;
        //    unit.attributeSet.stats.ActionPoints = Math.Max(0, unit.attributeSet.stats.ActionPoints - 1);
        //}

        // restore
        unit.agent.stoppingDistance = defaultStoppingDistance;

        // cler visuals
        AimingVisualizer.ClearPathPreview();
    }

    public static IEnumerator FollowPath1(CharacterUnit unit, List<Vector3> path, float speed)
    {
        var pathNodes = path;
        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerStep = 1f / unit.attributeSet.stats.Speed;

        // this setup assures the smooth movement
        unit.agent.speed = speed;
        unit.agent.isStopped = false;
        var defaultAcceleration = unit.agent.acceleration;
        unit.agent.acceleration = 9999f;
        unit.agent.autoBraking = false;
        var defaultStoppingDistance = unit.agent.stoppingDistance;
        unit.agent.stoppingDistance = 0f;

        for (int i = 0; i < pathNodes.Count; i++)
        {
            var point = pathNodes[i];

            if(i == pathNodes.Count - 1)
            {
                // return defaults to agent at the end
                unit.agent.autoBraking = true;
            }

            unit.agent.SetDestination(point);

            unit.LookAtTarget(point); // Facing the target
            remainingAP -= costPerStep;
            unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);

            while (Vector3.Distance(unit.agent.transform.position, point) > unit.agent.stoppingDistance + 0.1f)
            {
                yield return null;
            }

            if (remainingAP < 0)
            {
                break;
            }
        }

        // return defaults to agent        
        unit.agent.acceleration = defaultAcceleration;
        unit.agent.stoppingDistance = defaultStoppingDistance;

        // Finalize state
        unit.agent.isStopped = true;
        unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);
        AimingVisualizer.Hide();
    }


    private IEnumerator FollowPathCoroutine(Pathfinding.Path path, float speed)
    {
        var unit = partyManager.CurrentSelected;
        var pathNodes = path.pathNodes;
        float remainingAP = unit.attributeSet.stats.ActionPoints;
        float costPerStep = 1f / unit.attributeSet.stats.Speed;

        foreach (var node in pathNodes)
        {
            Vector3 targetPos = node.worldPos;

            unit.LookAtTarget(targetPos); // Facing the target
            remainingAP -= costPerStep;
            unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);

            while (Vector3.Distance(unit.transform.position, targetPos) > 0.05f)
            {
                if (Input.GetMouseButtonDown(1)) // Cancel move
                {                    
                    Console.Log("Movement cancelled.", unit.attributeSet.stats.ActionPoints);
                    yield break;
                }

                unit.transform.position = Vector3.MoveTowards(unit.transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }

           
            if (remainingAP < 0)
            {
                Console.Log("Ran out of AP.");
                break;
            }
        }

        // Finalize state
        unit.attributeSet.stats.ActionPoints = Mathf.FloorToInt(remainingAP);
        Console.Log($"{unit.unitName} finished movement. Remaining AP: {unit.attributeSet.stats.ActionPoints}");
        AimingVisualizer.Hide();
    }
     
    public override void Exit() 
    {
        base.Exit();
        //var unit = partyManager.CurrentSelected;
        //unit.GetComponent<NavMeshAgent>().enabled = false;
        //unit.GetComponent<NavMeshObstacle>().enabled = true;
        remaining?.Clear(); 
        AimingVisualizer.Hide();
        //AimingVisualizer.ClearState();
        gameManager.StopAllCoroutinesMDD();
        Console.Log("Exiting turn based mode");
    }

    private List<Vector3> remaining;    
    
}

public class CastingSubstate : SubStateBase
{
    public CastingSubstate(GameManagerMDD manager) : base(manager)
    {
        substate = InteractionSubstate.Casting;
    }

    protected override void HandleButtonEvent(EventSystemMDD.ButtonEvent customEvent)
    {
        if (customEvent.isConsumed == true) return;

        if (customEvent.eventType == EventSystemMDD.EventType.CharPortratClick)
        {
            customEvent.Consume();

            if (gameManager.GetCoroutine("casting_spell")?.IsRunning == true)
                return;

            var caster = partyManager.CurrentSelected;
            var targetUnit = customEvent.targetUnit;
            var spell = caster.GetSelectedSpell();

            //var path = SpellVisualizer.VisualizeSpell(
            //    spell,
            //    -1,
            //    caster.attributeSet.stats.Speed,
            //    caster,
            //    targetUnit.transform.position,
            //    out bool inRange
            //);

            //if (!inRange)
            //{
            //    Console.Log("Target out of range.");
            //    return;
            //}
            //
            //Vector3 targetPos = targetUnit.GetFeetPos();
            //
            //gameManager.CreateCoroutine("casting_spell",
            //    CastSpell(caster, path, targetPos, gameManager));
        }
    }


    public override void Enter()
    {
        base.Enter();
        Console.Log("Entered Casting Substate");
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        if (statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "SubStatus: Casting";
        }


        // action points
        partyManager.CurrentSelected.attributeSet.stats.ActionPoints = 9999;
    }

    public override void Update()
    {
        if (Input.GetMouseButtonDown(1)) { AimingVisualizer.Hide(); Debug.Log("Cast cancelled"); gameManager.GetCurrentState().SetMovementSubState(); return; }

        if (EventSystem.current.IsPointerOverGameObject()) return;

        var caster = partyManager.CurrentSelected;
        var castingSpellCoroutine = gameManager.GetCoroutine("casting_spell");

        // Prevent path preview while moving
        if (castingSpellCoroutine?.IsRunning == true)
        {
            AimingVisualizer.Hide();
            return;
        }

        bool inRange = false;
        //NavMeshPath path = null;
        List<GameObject> hitTargets = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            inRange = SpellVisualizer.VisualizeSpell(
                partyManager.CurrentSelected.GetSelectedSpell(),
                9999,
                partyManager.CurrentSelected.attributeSet.stats.Speed,
                partyManager.CurrentSelected.GetFeetPos(),
                hit.point,
                out hitTargets
                );
        }

        if (Input.GetMouseButtonDown(0) && inRange)
        {
            var target = hit.point;
            gameManager.CreateCoroutine("casting_spell", CastSelectedSpell
                (caster, gameManager, target, () =>                
                    {
                        gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
                    })
                );
        }
        /*
        Pathfinding.Path path = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            // for optimisation, to alter the state of line renderer only if user is aiming
            bool mouseMoved = MouseTracker.MouseMovedThisFrame;
            bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

            if (mouseMoved || mouseClicked)
            {
                path = SpellVisualizer.VisualizeSpell(
                    partyManager.CurrentSelected.GetSelectedSpell(),
                    -1,
                    partyManager.CurrentSelected.attributeSet.stats.Speed,
                    partyManager.CurrentSelected,
                    //partyManager.CurrentSelected.GetSpellSpawnLocation(),
                    hit.point,
                    out inRange
                    );
            }
        }

        if (Input.GetMouseButtonDown(0) && inRange)
        {
            var target = hit.point;
            gameManager.CreateCoroutine("casting_spell", CastSpell(caster, path, target, gameManager));
        }*/
    }

    public static IEnumerator CastSelectedSpell1(CharacterUnit caster, GameManagerMDD gameManager, NavMeshPath path, Vector3 targetPoint, Action onImpact)
    {
        // variables:
        var spell = caster.GetSelectedSpell();

        // 1- Walk first
        if (path != null)
            yield return gameManager.StartCoroutine(TurnBasedMovement.FollowPath(caster, new List<Vector3>(path.corners), 3f));

        bool done = false; // simple bool will block the immediate return

        // 2- Cast at targetPoint when walk is over
        // onComplete callback is passed to ApplySpell in Combat manager
        gameManager.CombatManager.ApplySpell(gameManager, caster, spell, targetPoint, () =>
        {
            // only after the animation spell is over
            AimingVisualizer.DrawImpactCircle(targetPoint, spell.radius, Color.red);
            caster.DeductActionPoints(spell.apCost);
            done = true;
            //onComplete?.Invoke();
            AimingVisualizer.Hide();
        });

        yield return new WaitUntil(() => done);

        onImpact?.Invoke();
    }

    public static IEnumerator CastSelectedSpell(CharacterUnit caster, GameManagerMDD gameManager, Vector3 targetPoint, Action onImpact)
    {
        // 1) variables:
        var spell = caster.GetSelectedSpell();
        var spellRadius = spell.radius * 2;
        bool done = false; // simple bool will block the immediate return

        // 2) Cast
        gameManager.CombatManager.ApplySpell(gameManager, caster, spell, targetPoint, () =>
        {
            // only after the animation spell is over
            if(spell.physicsType != SpellPhysicsType.Static)
                AimingVisualizer.DrawImpactCircle(targetPoint, spell.radius, Color.red);
            else
                AimingVisualizer.DrawImpactCircle(caster.GetFeetPos(), spell.radius, Color.green);
            caster.DeductActionPoints(spell.apCost);
            done = true;
            AimingVisualizer.Hide();
        });

        yield return new WaitUntil(() => done);

        onImpact?.Invoke();
    }

    public static IEnumerator CastSelectedSpell(CharacterUnit caster, GameManagerMDD gameManager, NavMeshPath path, Vector3 targetPoint, Action onImpact)
    {
        // variables:
        var spell = caster.GetSelectedSpell();
        var spellRadius = spell.radius * 2;

        // 1- Walk first
        if (path != null)
            yield return gameManager.StartCoroutine(TurnBasedMovement.MoveWithApMeter(caster, path, 3f, null, spellRadius));

        bool done = false; // simple bool will block the immediate return

        // 2- Cast at targetPoint when walk is over
        // onComplete callback is passed to ApplySpell in Combat manager
        gameManager.CombatManager.ApplySpell(gameManager, caster, spell, targetPoint, () =>
        {
            // only after the animation spell is over
            AimingVisualizer.DrawImpactCircle(targetPoint, spell.radius, Color.red);
            caster.DeductActionPoints(spell.apCost);
            done = true;
            //onComplete?.Invoke();
            AimingVisualizer.Hide();
        });

        yield return new WaitUntil(() => done);

        onImpact?.Invoke();
    }

    public static IEnumerator CastSelectedSpell(CharacterUnit caster, GameManagerMDD gameManager, NavMeshPath path,
        List<GameObject> targets,
        Vector3 targetPoint, Action onImpact)
    {
        // variables:
        var spell = caster.GetSelectedSpell();
        var spellRadius = spell.radius * 2;

        if (targets != null && targets.Count == 1) // only targeting one game object
        {
            if (path != null)
                yield return gameManager.StartCoroutine(TurnBasedMovement.MoveWithApMeter(caster, targets[0].transform.position, 3f, spellRadius));
        }
        else
        { 
            // 1- Walk first
            if (path != null)
                yield return gameManager.StartCoroutine(TurnBasedMovement.MoveWithApMeter(caster, targetPoint, 3f, spellRadius));
        }

        bool done = false; // simple bool will block the immediate return

        // 2- Cast at targetPoint when walk is over
        // onComplete callback is passed to ApplySpell in Combat manager
        gameManager.CombatManager.ApplySpell(gameManager, caster, spell, targetPoint, () =>
        {
            // only after the animation spell is over
            AimingVisualizer.DrawImpactCircle(targetPoint, spell.radius, Color.red);
            caster.DeductActionPoints(spell.apCost);
            done = true;
            //onComplete?.Invoke();
            AimingVisualizer.Hide();
        });

        yield return new WaitUntil(() => done);

        onImpact?.Invoke();
    }

    /*public IEnumerator CastSpell(CharacterUnit caster, Pathfinding.Path path, Vector3 target, GameManagerMDD gameManager)
    {
        Console.Log("Casitng spell called");
        
        yield return caster.CastSpellWithMovement(
                caster,
                gameManager.CombatManager,
                path,
                target,
                target,
                () =>
                {
                    // only runs after move + cast are done (when spell animation is over)
                    AimingVisualizer.Hide();
                    //GameManagerMDD.GetCurrentState().GetSubstate().AnimationFinished = true;
                    gameManager.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Finished;


                    // Special case : restore char's ap to maximin
                    //caster.SetActionPoints(caster.stats.StartActionPoints);
                }
            );

        gameManager.GetCurrentState().SetSubstate(new MovementSubstate(gameManager));
    }*/

    public override void Exit()
    {
        base.Exit();
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }   
}

public class TurnBasedCasting : SubStateBase
{
    private Vector3 lastHitPos;

    public TurnBasedCasting(GameManagerMDD manager) : base(manager)
    {
        substate = InteractionSubstate.CombatCasting;
    }

    protected override void HandleButtonEvent(EventSystemMDD.ButtonEvent customEvent)
    {
        if (customEvent.isConsumed == true) return;

        switch (customEvent.eventType)
        {
            case EventSystemMDD.EventType.CharPortratClick:
            case EventSystemMDD.EventType.EnemyPortratClick:
                {
                    if (gameManager.GetCoroutine("casting_spell")?.IsRunning == true)
                        return;

                    var caster = partyManager.CurrentSelected;
                    var targetUnit = customEvent.targetUnit;
                    var spell = caster.GetSelectedSpell();

                    //var path = SpellVisualizer.VisualizeSpell(
                    //    spell,
                    //    -1,
                    //    caster.attributeSet.stats.Speed,
                    //    caster,
                    //    targetUnit.transform.position,
                    //    out bool inRange
                    //);
                    //
                    //if (!inRange)
                    //{
                    //    Console.Log("Target out of range.");
                    //    return;
                    //}
                    //
                    //Vector3 targetPos = targetUnit.GetFeetPos();
                    //
                    //gameManager.CreateCoroutine("casting_spell",
                    //    CastSpell(caster, path, targetPos, gameManager));
                }
                break;

            case EventSystemMDD.EventType.SpellClick:
                {
                    customEvent.Consume();
                    var unit = customEvent.targetUnit;
                    unit.StopMovement();
                    unit.SelectSpell(customEvent.spell);
                }
                break;

            default:
                break;
        }
    }

    public override void Enter()
    {
        base.Enter();

        Console.Log("Entered CombatCasting Substate");
        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        if (statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "SubStatus: TurnCasting";
        }

        //unit.agent.isStopped = true;
        //unit.GetComponent<NavMeshAgent>().enabled = true;
        //unit.GetComponent<NavMeshObstacle>().enabled = false;

        var unit = partyManager.CurrentSelected;
        unit.MemorizePosition();
    }

    public override void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        // early return
        if (partyManager.CurrentSelected.GetSelectedSpell().apCost > partyManager.CurrentSelected.attributeSet.stats.ActionPoints)
        {
            gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }

        var caster = partyManager.CurrentSelected;
        var castingSpellCoroutine = gameManager.GetCoroutine("casting_spell");

        // Prevent path preview while moving
        if (castingSpellCoroutine?.IsRunning == true)
        {
            AimingVisualizer.Hide();
            return;
        }

        bool inRange = false;
        //NavMeshPath path = null;
        List<GameObject> hitTargets = null;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            inRange = SpellVisualizer.VisualizeSpell(
                partyManager.CurrentSelected.GetSelectedSpell(),
                partyManager.CurrentSelected.attributeSet.stats.ActionPoints,
                partyManager.CurrentSelected.attributeSet.stats.Speed,
                partyManager.CurrentSelected.GetFeetPos(),
                hit.point,
                out hitTargets
                );

            //path = SpellVisualizer.VisualizeSpell(
            //    partyManager.CurrentSelected.GetSelectedSpell(),
            //    partyManager.CurrentSelected.attributeSet.stats.ActionPoints,
            //    partyManager.CurrentSelected.attributeSet.stats.Speed,
            //    partyManager.CurrentSelected.GetFeetPos(),
            //    hit.point,
            //    out inRange
            //    );
        }

        if (Input.GetMouseButtonDown(0) && inRange)
        {
            var target = hit.point;
            gameManager.CreateCoroutine("casting_spell", CastingSubstate.CastSelectedSpell
                (caster, gameManager, target, () =>
                    {
                        gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
                    })
                );
        }

        if (Input.GetMouseButtonDown(1))
        {
            AimingVisualizer.Hide();
            gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }

        /*
        var caster = partyManager.CurrentSelected;
        var castingSpellCoroutine = gameManager.GetCoroutine("casting_spell");

        // Prevent path preview while moving
        if (castingSpellCoroutine?.IsRunning == true)
        {
            AimingVisualizer.Hide();
            return;
        }

        //if (!MouseTracker.MouseMovedThisFrame && !Input.GetMouseButtonDown(0))
        //{
        //    return;
        //}

        bool inRange = false;
        Pathfinding.Path path = null;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out var hit))
        {
            if (lastHitPos == hit.point && !Input.GetMouseButtonDown(0)) // for optimisation
                return;

            lastHitPos = hit.point;

            // for optimisation, to alter the state of line renderer only if user is aiming
            bool mouseMoved = true;// MouseTracker.MouseMovedThisFrame;
            bool mouseClicked = Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);

            if (mouseMoved || mouseClicked)
            {
                path = SpellVisualizer.VisualizeSpell(
                    partyManager.CurrentSelected.GetSelectedSpell(),
                    -1,
                    partyManager.CurrentSelected.attributeSet.stats.Speed,
                    partyManager.CurrentSelected,//        .transform.position,
                    //partyManager.CurrentSelected.GetSpellSpawnLocation(),
                    hit.point,
                    out inRange
                    );
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            AimingVisualizer.Hide();
            gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
            return;
        }

        if (Input.GetMouseButtonDown(0) && inRange)
        {
            var target = hit.point;
            gameManager.CreateCoroutine("casting_spell", CastSpell(caster, path, target, gameManager));
        }*/
    }

    /*public IEnumerator CastSpell(CharacterUnit caster, Pathfinding.Path path, Vector3 target, GameManagerMDD gameManager)
    {
        Console.Log("Casitng spell called");

        yield return caster.CastSpellWithMovement(
                caster,
                gameManager.CombatManager,
                path,
                target,
                target,
                () =>
                {
                    // only runs after move + cast are done (when spell animation is over)
                    AimingVisualizer.Hide();
                    //GameManagerMDD.GetCurrentState().GetSubstate().AnimationFinished = true;
                    gameManager.GetCurrentState().GetSubstate().SpellcastingAnimationState = SpellCastingAnimationStates.Finished;


                    // Special case : restore char's ap to maximin
                    //caster.SetActionPoints(caster.stats.StartActionPoints);
                }
            );

        gameManager.GetCurrentState().SetSubstate(new TurnBasedMovement(gameManager));
    }*/

    public override void Exit()
    {
        base.Exit();
        
        partyManager.CurrentSelected.MemorizePosition();
        AimingVisualizer.Hide();
        Console.Log("Exited Casting Substate");
    }
}

public class AITurnSubstate : SubStateBase
{
    private Action endTurn;

    private IParty partyAI;

    public AITurnSubstate(GameManagerMDD manager, Action onCompleteCallback) : base(manager)
    {
        substate = InteractionSubstate.AI_Turn;
        endTurn = onCompleteCallback;
    }

    public override void Enter()
    {
        base.Enter();

        // ui debug
        GameObject statusTextObject = GameObject.Find("Substatus");
        if (statusTextObject != null)
        {
            Text statusText = statusTextObject.GetComponent<Text>();
            statusText.text = "SubStatus: AITurn";
        }

        var aiUnit = partyManager.CurrentSelected;
        Console.ScrLog("AI turn", aiUnit.GetInstanceID());

        //aiUnit.GetComponent<NavMeshAgent>().enabled = true;
        //aiUnit.GetComponent<NavMeshObstacle>().enabled = false;
        //aiUnit.agent.isStopped = true;

        aiUnit.attributeSet.stats.ActionPoints = 10;

        partyAI = gameManager.combatManagement.GetRegistry().GetContext(aiUnit).parent;
        Assert.IsNotNull(partyAI);
        partyAI.CurrentSelected = aiUnit;

        // for BT context
        aiUnit.isMyTurn = true;
    }

    public override void Update() 
    {
        // Run AI BT logic
        partyAI.HandleScriptedAction();

        //var aiUnit = partyManager.CurrentSelected;
        //gameManager.aiManager.TickAI(aiUnit);

        //if(!called)
        //    TimerUtility.WaitAndDo(gameManager, 2f, endTurn);
        //called = true;
    }

    private bool called = false;

    public override void Exit() 
    {
        base.Exit();

        var aiUnit = partyManager.CurrentSelected;
        aiUnit.isMyTurn = false;
        //aiUnit.agent.isStopped = true;
        //aiUnit.GetComponent<NavMeshAgent>().enabled = false;
        //aiUnit.GetComponent<NavMeshObstacle>().enabled = true;

        // deselect
        partyAI.CurrentSelected = null;
    }
}