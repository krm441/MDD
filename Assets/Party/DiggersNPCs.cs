using AiMdd;
using PartyManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using static UnityEngine.UI.CanvasScaler;

public class DiggersNPCs : PartyNPC
{
    public List<Transform> spawnPositions = new List<Transform>();


    [SerializeField] Transform stockpile;
    private GameManagerMDD gameManager;

    private int counter = 0;
    private Dictionary<CharacterUnit, BTNode> aiTrees = new Dictionary<CharacterUnit, BTNode>();
    private Dictionary<CharacterUnit, BTblackboard> blackboards = new Dictionary<CharacterUnit, BTblackboard>();

    protected override void OnStart()
    {
        gameManager = FindObjectOfType<GameManagerMDD>();
        Assert.IsNotNull(gameManager);

        Init(this.transform.position, 5f, 1);
        foreach (var unit in partyMembers)
        {
            InitAI(unit);
            PushCharToReg(unit);
        }
    }

    public override void HandleScriptedAction()
    {
        Assert.IsNotNull(CurrentSelected);
        TickAI(CurrentSelected);
    }

    protected override void OnUpdate() 
    {
        foreach (var unit in partyMembers)
            TickAI(unit);
    }

    public void TickAI(CharacterUnit unit)
    {
        if (aiTrees.TryGetValue(unit, out var tree) && blackboards.TryGetValue(unit, out var bb))
        {
            tree.Tick(bb);
        }
    }

    private void Init(Vector3 center, float areaRadius, int prefCount)
    {
        string enemyType = "NpcSimple";
        if (!EnemyDatabase.NPCs.TryGetValue(enemyType, out var npcDef))
        {
            Console.Error($"Enemy type '{enemyType}' not found in EnemyDatabase.");
            return;
        }

        //GameObject prefab = Resources.Load<GameObject>(npcDef.prefabPath);
        GameObject prefab = Resources.Load<GameObject>("NPC/CharPrefab");
        GameObject visual = Resources.Load<GameObject>("NPC/" + npcDef.visualPath);

        if (prefab == null || visual == null)
        {
            Console.Error("Could not load prefab or visual for enemy type", enemyType);
            return;
        }

        // Root holder for all enemy parties
        GameObject root = GameObject.Find("EnemyParties") ?? new GameObject("EnemyParties");

        // Create a holder for this specific pack
        GameObject partyHolder = new GameObject($"ResourceDiggers {counter++}");
        partyHolder.transform.parent = root.transform;
        //var enemyParty = partyHolder.AddComponent<EnemyParty>();



        // Determine how many enemies based on area size
        float spacing = 2.5f;
        int maxEnemies = Mathf.FloorToInt(Mathf.PI * areaRadius * areaRadius / (spacing * spacing));
        int count = Mathf.Clamp(maxEnemies, 1, prefCount);

        List<Vector3> spawnPoints = NPCSpawnHelper.GenerateCirclePositions(center, areaRadius, count);
        List<CharacterUnit> activeEnemies = new List<CharacterUnit>();

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = spawnPoints[i];

            GameObject obj = GameObject.Instantiate(prefab, pos, Quaternion.identity, partyHolder.transform);

            // Set the layer to HostileNPCs
            obj.layer = LayerMask.NameToLayer("HostileNPCs");

            // Set all children recursively - since the model will be made from many meshes
            foreach (Transform child in obj.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("HostileNPCs");
            }

            CharacterUnit unit = obj.GetComponent<CharacterUnit>();
            if (unit == null)
            {
                Debug.LogError("CharPrefab missing CharacterUnit script.");
                continue;
            }

            // Assign properties from JSON
            unit.unitName = $"{npcDef.displayName}_{i}";
            unit.isPlayerControlled = false;
            unit.attributeSet.stats = new StatBlock(npcDef.statBlock);
            unit.attributeSet.armorStat = new ArmorStat(npcDef.armorStat);
            unit.attributeSet.resistances = new DamageResistenceContainer(npcDef.damageResistenceContainer);

            // register BTs
            //aiManager.SetupAI_BT(unit);

            // spell
            unit.spellBook.AddSpell(spellMap.idSpellPairs[3]); // basic piercing arrow

            // portrait
            Sprite portrait = Resources.Load<Sprite>(npcDef.portraitPath);
            if (portrait == null)
                Debug.LogWarning($"Portrait not found for {npcDef.portraitPath}");
            else
                unit.portraitSprite = portrait;

            GameObject vis = GameObject.Instantiate(visual, obj.transform);
            vis.transform.localRotation = Quaternion.identity;

            activeEnemies.Add(unit);
            Console.Log($"Spawned enemy {unit.unitName}");
        }

        partyMembers = activeEnemies;
    }

    private void RegisterAI(CharacterUnit unit, BTNode tree, BTblackboard context)
    {
        aiTrees[unit] = tree;
        blackboards[unit] = context;
    }

    private void InitAI(CharacterUnit unit)
    {
        var tree = new Selector
            (
                new Sequence // idle state
                (
                    new CheckCombatStateFalse(),
                    new CheckEnemyInRange(),
                    new FindResourceInRadius(25f),
                    new MoveToResource(),
                    new HarvestResource(),
                    new MoveToStockpile()
                )
                ,
                new Sequence // combat state
                (
                    new CheckIsMyTurn(),
                    new CheckCombatStateTrue(),
                    new PickTargetRadius(),
                    new Selector // OR logic
                    (
                        new Sequence( // AND logic
                            new CalculateSpellPath(),
                            new CastSpell()
                        ),
                        new PursueTarget()
                    ),
                    new EndTurn()
                )
            );

        var context = new BTblackboard
        {
            Caster = unit,
            gameManager = gameManager,
            PotentialTargets = FindObjectOfType<PartyPlayer>().partyMembers,

            StockpilePosition = stockpile
        };

        RegisterAI(unit, tree, context);
    }
}