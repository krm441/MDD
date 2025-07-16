using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Node
    {
        public Vector2Int gridPos;
        public Vector3 worldPos;
        public bool isWalkable = true;

        public float gCost, hCost;
        public Node parentNode;

        public float fCost => gCost + hCost;

        public Node() { }

        public Node(Vector2Int gridPos, Vector3 worldPos, bool isWalkable)
        {
            this.gridPos = gridPos;
            this.worldPos = worldPos;
            this.isWalkable = isWalkable;
        }
    }

}