using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class ThetaStar
    {
        private Node[,] grid;
        private int width, height;

        public void SetGrid(Node[,] grid)
        {
            this.grid = grid;
            width = grid.GetLength(0);
            height = grid.GetLength(1);
        }

        public List<Node> FindPath(Node start, Node target)
        {
            var openSet = new List<Node> { start };
            var closedSet = new HashSet<Node>();

            // optimisition techniques - initial Node class was designed for AStar. in this implementation we need to keep gCost and hCost outside from the Node class
            var gCost = new Dictionary<Node, float>();
            var hCost = new Dictionary<Node, float>();
            var parent = new Dictionary<Node, Node>();

            gCost[start] = 0;
            hCost[start] = GetDistance(start, target);
            parent[start] = start;

            while (openSet.Count > 0)
            {
                // Get node with lowest fCost
                Node current = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    float fCurrent = gCost[current] + hCost[current];
                    float fOther = gCost[openSet[i]] + hCost[openSet[i]];

                    if (fOther < fCurrent || (fOther == fCurrent && hCost[openSet[i]] < hCost[current]))
                    {
                        current = openSet[i];
                    }
                }

                if (current == target)
                    return RetracePath(start, target, parent);

                openSet.Remove(current);
                closedSet.Add(current);

                foreach (var neighbor in GetNeighbours(current))
                {
                    if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                        continue;

                    // Ensure neighbor is initialized
                    if (!gCost.ContainsKey(neighbor)) gCost[neighbor] = float.MaxValue;
                    if (!hCost.ContainsKey(neighbor)) hCost[neighbor] = GetDistance(neighbor, target);

                    Node currentParent = parent[current];

                    if (currentParent != null && HasLineOfSight(currentParent, neighbor))
                    {
                        float tentativeG = gCost[currentParent] + GetDistance(currentParent, neighbor);
                        if (tentativeG < gCost[neighbor])
                        {
                            gCost[neighbor] = tentativeG;
                            parent[neighbor] = currentParent;
                            if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                        }
                    }
                    else
                    {
                        float tentativeG = gCost[current] + GetDistance(current, neighbor);
                        if (tentativeG < gCost[neighbor])
                        {
                            gCost[neighbor] = tentativeG;
                            parent[neighbor] = current;
                            if (!openSet.Contains(neighbor)) openSet.Add(neighbor);
                        }
                    }
                }
            }

            return null; // No path found
        }

        private List<Node> RetracePath(Node start, Node end, Dictionary<Node, Node> parent)
        {
            List<Node> path = new List<Node>();
            Node current = end;

            while (current != start)
            {
                path.Add(current);
                current = parent[current];
            }

            //path.Add(start); // include the starting node : NOTE: no need, makes a zig-zag path
            path.Reverse();
            return path;
        }

        private List<Node> GetNeighbours(Node node)
        {
            List<Node> result = new List<Node>();

            Vector2Int[] dirs = {
                new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1),
                new Vector2Int(-1, -1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(1, 1)
            };

            foreach (var dir in dirs)
            {
                int nx = node.gridPos.x + dir.x;
                int ny = node.gridPos.y + dir.y;

                if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                    result.Add(grid[nx, ny]);
            }

            return result;
        }

        private bool HasLineOfSight(Node from, Node to)
        {
            Vector2Int a = from.gridPos;
            Vector2Int b = to.gridPos;

            int dx = Mathf.Abs(b.x - a.x);
            int dy = Mathf.Abs(b.y - a.y);
            int sx = a.x < b.x ? 1 : -1;
            int sy = a.y < b.y ? 1 : -1;

            int err = dx - dy;

            while (a != b)
            {
                if (!grid[a.x, a.y].isWalkable)
                    return false;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; a.x += sx; }
                if (e2 < dx)  { err += dx; a.y += sy; }
            }

            return true;
        }

        private float GetDistance(Node a, Node b)
        {
            float dx = Mathf.Abs(a.gridPos.x - b.gridPos.x);
            float dy = Mathf.Abs(a.gridPos.y - b.gridPos.y);
            return Mathf.Sqrt(dx * dx + dy * dy); // Euclidean for Theta*
        }
    }
}
