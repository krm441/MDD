using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    public class AStar
    {
        private Node[,] grid;
        private int gridWidth;
        private int gridHeight;

        public bool allowDiagonals = true;

        public void SetGrid(Node[,] grid)
        {
            this.grid = grid;
            gridWidth = grid.GetLength(0);
            gridHeight = grid.GetLength(1);
        }

       

        public List<Node> FindPath(Node start, Node target)
        {
            //Typical A* algorythm from here and on

            List<Node> foundPath = new List<Node>();

            //We need two lists, one for the nodes we need to check and one for the nodes we've already checked
            List<Node> openSet = new List<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();

            //We start adding to the open set
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                Node currentNode = openSet[0];

                for (int i = 0; i < openSet.Count; i++)
                {
                    //We check the costs for the current node
                    //You can have more opt. here but that's not important now
                    if (openSet[i].fCost < currentNode.fCost ||
                        (openSet[i].fCost == currentNode.fCost &&
                        openSet[i].hCost < currentNode.hCost))
                    {
                        //and then we assign a new current node
                        if (!currentNode.Equals(openSet[i]))
                        {
                            currentNode = openSet[i];
                        }
                    }
                }

                //we remove the current node from the open set and add to the closed set
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);

                //if the current node is the target node
                if (currentNode.Equals(target))
                {
                    //that means we reached our destination, so we are ready to retrace our path
                    foundPath = RetracePath(start, currentNode);
                    break;
                }

                //if we haven't reached our target, then we need to start looking the neighbours
                foreach (Node neighbour in GetNeighbours(currentNode, allowDiagonals))
                {
                    if (!closedSet.Contains(neighbour))
                    {
                        //we create a new movement cost for our neighbours
                        float newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);

                        //and if it's lower than the neighbour's cost
                        if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                        {
                            //we calculate the new costs
                            neighbour.gCost = newMovementCostToNeighbour;
                            neighbour.hCost = GetDistance(neighbour, target);
                            //Assign the parent node
                            neighbour.parentNode = currentNode;
                            //And add the neighbour node to the open set
                            if (!openSet.Contains(neighbour))
                            {
                                openSet.Add(neighbour);
                            }
                        }
                    }
                }
            }

            //we return the path at the end
            return foundPath;
        }

        private List<Node> RetracePath(Node startNode, Node endNode)
        {
            //Retrace the path, is basically going from the endNode to the startNode
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                //by taking the parentNodes we assigned
                currentNode = currentNode.parentNode;
            }

            //then we simply reverse the list
            path.Reverse();

            return path;
        }

        private List<Node> GetNeighbours(Node node, bool allowDiagonals = false)
        {
            List<Node> retList = new List<Node>();

            Vector2Int[] directions = allowDiagonals
                ? new Vector2Int[] {
                    new Vector2Int(-1, 0), new Vector2Int(1, 0), // left, right
                    new Vector2Int(0, -1), new Vector2Int(0, 1), // down, up
                    new Vector2Int(-1, -1), new Vector2Int(-1, 1),
                    new Vector2Int(1, -1), new Vector2Int(1, 1)
                }
                : new Vector2Int[] {
                    new Vector2Int(-1, 0), new Vector2Int(1, 0),
                    new Vector2Int(0, -1), new Vector2Int(0, 1)
                };

            foreach (var dir in directions)
            {
                int checkX = node.gridPos.x + dir.x;
                int checkY = node.gridPos.y + dir.y;

                if (checkX >= 0 && checkY >= 0 && checkX < gridWidth && checkY < gridHeight)
                {
                    Node neighbor = grid[checkX, checkY];
                    if (neighbor.isWalkable)
                    {
                        retList.Add(neighbor);
                    }
                }
            }

            return retList;
        }

        private int GetDistance(Node a, Node b)
        {
            int dstX = Mathf.Abs(a.gridPos.x - b.gridPos.x);
            int dstY = Mathf.Abs(a.gridPos.y - b.gridPos.y); // using Y because it's 2D (grid's "Z" is stored as Y in Vector2Int)

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            else
                return 14 * dstX + 10 * (dstY - dstX);
        }

        public static bool IsStaircaseLikePath(IList<Node> path, int tolerance = 2)
        {
            if (path == null || path.Count < 3) return false;
            if (tolerance < 0) tolerance = 0;

            // Helpers
            bool IsCardinal(Vector2Int v) => (v.x == 0) ^ (v.y == 0);
            bool IsPerpendicular(Vector2Int a, Vector2Int b) => (a.x * b.x + a.y * b.y) == 0;

            // Compute normalized step between two nodes (-1/0/1 per axis)
            Vector2Int Step(Node a, Node b)
            {
                var d = b.gridPos - a.gridPos;
                int sx = d.x == 0 ? 0 : (d.x > 0 ? 1 : -1);
                int sy = d.y == 0 ? 0 : (d.y > 0 ? 1 : -1);
                return new Vector2Int(sx, sy);
            }

            bool hasPrev = false;
            Vector2Int prev = default;

            // Current alternating pair we are tracking (e.g., Right and Up with fixed signs)
            bool havePair = false;
            Vector2Int dirA = default; // first direction of the pair
            Vector2Int dirB = default; // second direction of the pair
            int alternations = 0;      // count of consecutive alternations in the current run

            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int dir = Step(path[i - 1], path[i]);
                if (dir == Vector2Int.zero) continue;           // duplicate node; ignore

                if (!IsCardinal(dir))                           // diagonals break staircase runs
                {
                    hasPrev = true; prev = dir;
                    havePair = false; alternations = 0;
                    continue;
                }

                if (!hasPrev)
                {
                    hasPrev = true; prev = dir;
                    havePair = false; alternations = 0;
                    continue;
                }

                if (dir == prev)
                {
                    // Straight segment -> break the consecutive zig-zag run
                    havePair = false; alternations = 0;
                }
                else if (IsPerpendicular(dir, prev))
                {
                    if (!havePair)
                    {
                        // First perpendicular turn defines the pair
                        dirA = prev;
                        dirB = dir;
                        havePair = true;
                        alternations = 1;
                    }
                    else
                    {
                        // Must alternate strictly between the same two directions
                        bool validFlip =
                            (dir == dirA && prev == dirB) ||
                            (dir == dirB && prev == dirA);

                        if (validFlip)
                        {
                            alternations++;
                        }
                        else
                        {
                            // Different turn -> start new run
                            dirA = prev;
                            dirB = dir;
                            alternations = 1;
                        }
                    }

                    if (alternations > tolerance) return true;
                }
                else
                {
                    // Backtrack or other non-perpendicular change -> reset
                    havePair = false; alternations = 0;
                }

                prev = dir; hasPrev = true;
            }

            return false;
        }
    }
}
