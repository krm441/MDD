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
        public bool isOccupied = false;
        public int ocupantID = -1;

        public float gCost, hCost;
        public Node parentNode;

        public float fCost => gCost + hCost;

        public Node() { }

        public Node(Vector2Int gridPos, Vector3 worldPos, bool isWalkable)
        {
            this.gridPos = gridPos;
            this.worldPos = worldPos;
            this.isWalkable = isWalkable;
            this.isOccupied = false;
        }

        public void Occupy(int id)
        {
            this.isOccupied = true;
            this.ocupantID = id;
        }

        public void UnOccupy()
        {
            this.isOccupied = false;
            this.ocupantID = -1;
        }
    }

}