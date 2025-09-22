using System.Collections;
using System.Collections.Generic;
using AiMdd;
using PartyManagement;
using UnityEngine;
using UnityEngine.Assertions;
using CleverCrow.Fluid.BTs.Trees;
using CleverCrow.Fluid.BTs.Tasks;
using static UnityEngine.UI.CanvasScaler;

public class SkeletalParty : PartyNPC
{
    public List<Transform> spawnPositions = new List<Transform>();

    [SerializeField] GameObject skeletalArcherPrefab;
    [SerializeField] private BehaviorTree behavioralTreeArchers;

    private GameObject go;
    private bool spawned = false;

    protected override void OnStart()
    {
        Init();
        foreach (var unit in partyMembers)
        {
            InitAI(unit);
            PushCharToReg(unit);
        }
    }

    protected override void OnUpdate()
    {
        foreach (var unit in partyMembers)
            TickAI(unit);
    }

    public void Spawn()
    {
        gameObject.SetActive(true);
        foreach(CharacterUnit unit in partyMembers)
        {
            var animator = unit.GetComponentInChildren<Animator>();

            animator.Play("Spawn_Ground", 0, 0f);
            animator.SetFloat("AppearSpeed", 1f);
        }
        spawned = true;
    }

    private void Init()
    {
        string enemyType = "SkeletalArcher";
        if (!EnemyDatabase.NPCs.TryGetValue(enemyType, out var npcDef))
        {
            Console.Error($"Enemy type '{enemyType}' not found in EnemyDatabase.");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("NPC/CharPrefab");
        GameObject visual = Resources.Load<GameObject>("NPC/" + npcDef.visualPath);

        Assert.IsNotNull(prefab);
        Assert.IsNotNull(visual);

        go = Instantiate(skeletalArcherPrefab, spawnPositions[0]);
        go.transform.SetParent(transform, true);

        var unit = FindObjectOfType<CharacterUnit>();

        unit.unitName = $"{npcDef.displayName}_{go.GetInstanceID()}";
        unit.isPlayerControlled = false;
        unit.attributeSet.stats = new StatBlock(npcDef.statBlock);
        unit.attributeSet.armorStat = new ArmorStat(npcDef.armorStat);
        unit.attributeSet.resistances = new DamageResistenceContainer(npcDef.damageResistenceContainer);

        partyMembers.Add(unit);

        gameObject.SetActive(false);// hidden till surprise appear
    }

    #region AI

    public void TickAI(CharacterUnit unit)
    {
        behavioralTreeArchers.Tick();
    }

    private void InitAI(CharacterUnit unit)
    {
        var splice1 = new BehaviorTreeBuilder(gameObject)
            .Sequence("Seq1")
                .Condition("Custom Condition", () => false)
                .Do("Custom Action", () => {
                    Debug.Log("Yes");
                    return TaskStatus.Success;
                })
            .End();

        behavioralTreeArchers = new BehaviorTreeBuilder(gameObject)
        .Selector("Top")
            .Splice(splice1.Build())
            .Sequence("Seq2")
                .Condition("Custom Condition2", () => true)
                .Do("Custom Action2", () => {
                    Debug.Log("Yes2");
                    return TaskStatus.Continue;
                })
                .Do("Custom Action3", () => {
                    Debug.Log("Yes3");
                    return TaskStatus.Success;
                })
            .End()
        .End()
        .Build();

      
    }

    #endregion
}
