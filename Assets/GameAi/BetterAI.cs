using System.Collections;
using System.Collections.Generic;
using CleverCrow.Fluid.BTs.Trees;
using CleverCrow.Fluid.BTs.Tasks;
using UnityEngine;



public class BetterAI : MonoBehaviour
{
    [SerializeField]
    private BehaviorTree _tree;

    private void Awake()
    {
        _tree = new BehaviorTreeBuilder(gameObject)
            .Sequence()
                .Condition("Custom Condition", () => {
                    return true;
                })
                .Do("Custom Action", () => {
                    Console.Log("Yes");
                    return TaskStatus.Success;
                })
            .End()
            .Build();
    }

    private void Update()
    {
        // Update our tree every frame
        _tree.Tick();
    }
}
