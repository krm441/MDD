using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum BTState { Success, Failure, Running }

public abstract class BTNode
{
    public abstract BTState Tick();
}
public class Sequence : BTNode
{
    private readonly List<BTNode> children;

    public Sequence(params BTNode[] nodes) => children = new List<BTNode>(nodes);

    public override BTState Tick()
    {
        foreach (var child in children)
        {
            var result = child.Tick();
            if (result != BTState.Success)
                return result; // Failure or Running
        }
        return BTState.Success;
    }
}

