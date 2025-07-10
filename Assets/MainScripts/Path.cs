using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class Path
    {
        public List<Pathfinding.Node> pathNodes;
        float distance;
        public float CalculateDistance(Vector3 start)
        {
            // calculate distance
            if (pathNodes == null)
                return 0f;

            distance = 0f;
            distance += Vector3.Distance(start, pathNodes[0].worldPos);

            for (int i = 0; i < pathNodes.Count - 1; i++)
            {
                Vector3 a = pathNodes[i].worldPos;
                Vector3 b = pathNodes[i + 1].worldPos;
                distance += Vector3.Distance(a, b);
            }

            return distance;
        }
    }
}