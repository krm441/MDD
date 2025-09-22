using System;
using System.Collections;
using System.Collections.Generic;
using PartyManagement;
using UnityEngine;

// =================== AI Manager for all AI utilities =================== //
namespace AiMdd
{
    public class AiManager : MonoBehaviour
    {
        private Dictionary<CharacterUnit, BTNode> aiTrees = new Dictionary<CharacterUnit, BTNode>();
        private Dictionary<CharacterUnit, BTblackboard> blackboards = new Dictionary<CharacterUnit, BTblackboard>();

        [SerializeField] private GameManagerMDD gameManager;
        [SerializeField] private Transform stockpile;

        private void RegisterAI(CharacterUnit unit, BTNode tree, BTblackboard context)
        {
            aiTrees[unit] = tree;
            blackboards[unit] = context;
        }

        public void TickAI(CharacterUnit unit)
        {
            if (aiTrees.TryGetValue(unit, out var tree) && blackboards.TryGetValue(unit, out var bb))
            {
                var result = tree.Tick(bb);

                if (result == BTState.Success || result == BTState.Failure)
                {
                    //Console.Log("result", result);

                    // Turn over
                    //gameManager.GetCurrentState().NextTurn();
                }
            }
        }

        public void SetupAI_BT(CharacterUnit unit)
        {
            var tree =
                new Selector
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
                //new EndTurn()
                );

            Debug.Assert(gameManager != null, "gameManager is null");
            Debug.Assert(gameManager.gridSystem != null, "gridSystem is null");
            Debug.Assert(gameManager.partyManager != null, "partyManager is null");
            //Debug.Assert(gameManager.partyManager.GetPlayerControlledUnits() != null, "PotentialTargets is null");


            var context = new BTblackboard
            {
                Caster = unit,
                gameManager = gameManager,
                Grid = gameManager.gridSystem,
                //PotentialTargets = gameManager.partyManager.GetPlayerControlledUnits(),

                StockpilePosition = stockpile//.position
            };

            RegisterAI(unit, tree, context);
        }
    }
}