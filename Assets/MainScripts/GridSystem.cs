using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems; // this should be moved out
using UnityEngine;
using PartyManagement;

namespace Pathfinding
{
    [ExecuteInEditMode]
    public class GridSystem : MonoBehaviour
    {

        void Awake()
        {
            //if(partyManager == null)
            //{
            //    Debug.LogError("Party manager is at null - ")
            //}
            //partyManager = PartyManagement.PartyManager.Instance;
            //Debug.Log("awake: GridSystem");
        }

        public void GeneratePathfinder(int width, int height)
        {
            aStar = new AStar();
            GenerateGrid(width, height);
            aStar.SetGrid(grid);

            thetaStar = new ThetaStar();
            thetaStar.SetGrid(grid);
        }

        public int width, height;
        public float nodeSize;

        public void GeneratePathThetta(int width_, int height_, bool walkable)
        {
            width = width_;
            height = height_;
            nodeSize = 1;

            GenerateGrid(width, height, walkable);

            Subdivide();

            thetaStar = new ThetaStar();
            thetaStar.SetGrid(grid);
        }

        public void MarkWalkable(Vector2Int gridPos, bool isWalkable_)
        {
            if (gridPos.x >= 0 && gridPos.y >= 0 && gridPos.x < grid.GetLength(0) && gridPos.y < grid.GetLength(1))
            {
                grid[gridPos.x, gridPos.y].isWalkable = isWalkable_;
            }
        }

        public void MarkOccupied(Vector2Int gridPos, int idOccupant, bool isOccupied_)
        {
            if (gridPos.x >= 0 && gridPos.y >= 0 && gridPos.x < grid.GetLength(0) && gridPos.y < grid.GetLength(1))
            {
                grid[gridPos.x, gridPos.y].isOccupied = isOccupied_;

                if (isOccupied_)
                    grid[gridPos.x, gridPos.y].ocupantID = idOccupant;
                else
                    grid[gridPos.x, gridPos.y].ocupantID = -1;
            }
        }

        public GameObject clickMarkerPrefab;
        public Vector3 LastClickPosition;

        // Update is called once per frame
        void Update()
        {
            //DetectClick();
        }

        public PartyManagement.PartyManager partyManager;// = PartyManagement.PartyManager.Instance;

        public AStar aStar;
        public ThetaStar thetaStar;

        public LayerMask clickableLayer;

        Node[,] grid;
        public float tileSize = 1f;

        void GenerateGrid(int width, int height, bool walkable = false, int paddingRadius = 1)
        {
            grid = new Node[width, height];
            LayerMask floorMask = LayerMask.GetMask("PathGrid");
            LayerMask obstacleMask = LayerMask.GetMask("Obstacles");
            float rayHeight = 5f;

            // First pass: initialize grid and track obstacle positions
            List<Vector2Int> obstaclePositions = new List<Vector2Int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = new Vector3(x * tileSize, 0, y * tileSize);
                    bool hasFloor = Physics.Raycast(worldPos + Vector3.up * rayHeight, Vector3.down, rayHeight * 2f, floorMask);
                    bool hasObstacle = Physics.Raycast(worldPos + Vector3.up * rayHeight, Vector3.down, rayHeight * 2f, obstacleMask);

                    bool isWalkable = walkable && hasFloor && !hasObstacle;

                    grid[x, y] = new Node(new Vector2Int(x, y), worldPos, isWalkable);

                    if (hasObstacle)
                        obstaclePositions.Add(new Vector2Int(x, y));
                }
            }

            // Second pass: expand unwalkable region around obstacles
            foreach (var pos in obstaclePositions)
            {
                for (int dx = -paddingRadius; dx <= paddingRadius; dx++)
                {
                    for (int dy = -paddingRadius; dy <= paddingRadius; dy++)
                    {
                        int nx = pos.x + dx;
                        int ny = pos.y + dy;

                        if (nx >= 0 && ny >= 0 && nx < width && ny < height)
                        {
                            grid[nx, ny].isWalkable = false;
                        }
                    }
                }
            }
        }


        void GenerateGridgg(int width, int height, bool walkable = false)
        {
            // Grid arr initialised
            grid = new Node[width, height];

            // Layer mask
            LayerMask floorMask = LayerMask.GetMask("PathGrid");
            LayerMask obstacleMask = LayerMask.GetMask("Obstacles");
            float rayHeight = 5f; // height above ground to cast from


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = new Vector3(x * tileSize, 0, y * tileSize);

                    // Cast downward to detect floor
                    bool hasFloor = Physics.Raycast(worldPos + Vector3.up * rayHeight, Vector3.down, rayHeight * 2f, floorMask);
                    bool hasObstacle = Physics.Raycast(worldPos + Vector3.up * rayHeight, Vector3.down, rayHeight * 2f, obstacleMask);

                    //grid[x, y] = new Node(new Vector2Int(x, y), worldPos, false);
                    grid[x, y] = new Node(new Vector2Int(x, y), worldPos, walkable && hasFloor && !hasObstacle); // if floor will be unwalkable
                }
            }
        }

        public void Subdivide()
        {
            int newWidth = width * 2;
            int newHeight = height * 2;
            Node[,] newGrid = new Node[newWidth, newHeight];

            float newNodeSize = nodeSize / 2f;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Node oldNode = grid[x, y];
                    Vector3 center = oldNode.worldPos;

                    for (int dx = 0; dx < 2; dx++)
                    {
                        for (int dy = 0; dy < 2; dy++)
                        {
                            int newX = x * 2 + dx;
                            int newY = y * 2 + dy;

                            float offsetX = (dx * 2 - 1) * newNodeSize / 2f;
                            float offsetZ = (dy * 2 - 1) * newNodeSize / 2f;
                            Vector3 newPos = center + new Vector3(offsetX, 0, offsetZ);


                            bool walkable = oldNode.isWalkable;

                            newGrid[newX, newY] = new Node(new Vector2Int(newX, newY), newPos, walkable);
                        }
                    }
                }
            }

            grid = newGrid;
            width = newWidth;
            height = newHeight;
            nodeSize = newNodeSize;
            tileSize = newNodeSize;
        }


        public void FreeFootPrintPoints(List<Vector3> footPrintPoints)
        {
            foreach (var worldPt in footPrintPoints)
            {
                Node n = GetNodeFromWorldPosition(worldPt);
                MarkWalkable(n.gridPos, true);
            }
        }

        public void MarkFootPrintPoints(List<Vector3> footPrintPoints)
        {
            foreach (var worldPt in footPrintPoints)
            {
                Node n = GetNodeFromWorldPosition(worldPt);
                MarkWalkable(n.gridPos, false);
            }
        }

        private GameObject currentClickMarker; // marker prefab reference

        //public List<Node> FindPathTo(CharacterUnit to, Vector3 from)
        //{
        //    //FreeFootPrintPoints(to.ComputeFootprintPoints());
        //    var path = FindPathTo(to.GetFeetPos(), from, to.unitID);
        //    //MarkFootPrintPoints(to.ComputeFootprintPoints());
        //    return path;
        //}

        public List<Node> FindPathTo(Vector3 to, Vector3 from, int idInitiator, int idTarget)
        {
            Node startNode = GetNodeFromWorldPosition(from);
            Node endNode = GetNodeFromWorldPosition(to);

            if(endNode.isWalkable == false)
                return null;

            return thetaStar.FindPath(startNode, endNode, idInitiator, idTarget);

            /*
            if (startNode != null && endNode != null && endNode.isWalkable)
            {
                //var ret = thetaStar.FindPath(startNode, endNode);
                // If target tile is NOT occupied, proceed as normal
                if (!endNode.isOccupied)
                {
                    return thetaStar.FindPath(startNode, endNode);
                }
                else
                {
                    var path = thetaStar.FindPath(startNode, endNode);

                    if (path == null || path.Count == 0)
                        return null;

                    if (path.Count.Equals(1))
                    {
                        // Interpolate a point 1 unit away from the end node along the line from start
                        Vector3 dir = (endNode.worldPos - startNode.worldPos).normalized;
                        Vector3 stopPos = endNode.worldPos - dir * 1.0f;

                        path[0] = GetNodeFromWorldPosition(stopPos);
                        return path;
                    }
                    else
                    {
                        // Interpolate between the last two nodes to be 1 unit before the end node
                        Vector3 last = path[path.Count - 1].worldPos;
                        Vector3 prev = path[path.Count - 2].worldPos;
                        Vector3 dir = (last - prev).normalized;
                        Vector3 stopPos = last - dir * 1.0f;

                        var node = GetNodeFromWorldPosition(stopPos);
                        if (!node.isOccupied)
                        {
                            path[path.Count - 1] = node;
                            return path;
                        }
                        else
                        {
                            // Scan neighbors of the endNode
                            Node best = null;
                            float bestScore = float.MaxValue;

                            foreach (var neighbor in thetaStar.GetNeighbours(endNode))
                            {
                                if (neighbor.isWalkable && !neighbor.isOccupied)
                                {
                                    float dist = Vector3.Distance(neighbor.worldPos, stopPos);
                                    if (dist < bestScore)
                                    {
                                        best = neighbor;
                                        bestScore = dist;
                                    }
                                }
                            }

                            if (best != null)
                            {
                                path[path.Count - 1] = best;
                                return path;
                            }
                        }
                    }
                }
            }*/

            return null;
        }
        public Vector3 GridPosToWorld(Vector2Int gridPos)
        {
            return grid[gridPos.x, gridPos.y].worldPos;
        }

        // better method for path construction on click
        // from point 'from' to the point where ray hit the clickable layer
        // TODO: fix, and research the event syste,
        public List<Node> FindPathToClick(Transform from, int id)
        {
            
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayer))
                {
                    Node startNode = GetNodeFromWorldPosition(from.position);
                    Node endNode = GetNodeFromWorldPosition(hit.point);

                    // click position
                    LastClickPosition = hit.point;
                    
                    if (startNode != null && endNode != null && endNode.isWalkable)
                    {
                        return thetaStar.FindPath(startNode, endNode, id);
                    }

                    
                }
            //}

            return null;
        }

        public Node GetNodeFromWorldPosition(Vector2Int worldPos)
        {
            if (grid == null)
            {
                Debug.Log("Grid is null — pathfinder may not be initialized.");
                return null;
            }

            int x = Mathf.FloorToInt(worldPos.x / tileSize);
            int y = Mathf.FloorToInt(worldPos.y / tileSize);

            // Clamp to valid range
            x = Mathf.Clamp(x, 0, grid.GetLength(0) - 1);
            y = Mathf.Clamp(y, 0, grid.GetLength(1) - 1);

            return grid[x, y];
        }

        public Node GetNodeFromWorldPosition(Vector3 worldPos)
        {
            if (grid == null)
            {
                Debug.Log("Grid is null — pathfinder may not be initialized.");
                return null;
            }

            int x = Mathf.FloorToInt(worldPos.x / tileSize);
            int y = Mathf.FloorToInt(worldPos.z / tileSize);

            // Clamp to valid range
            x = Mathf.Clamp(x, 0, grid.GetLength(0) - 1);
            y = Mathf.Clamp(y, 0, grid.GetLength(1) - 1);

            return grid[x, y];
        }


        // DEBUG - visual debugging 
        void OnDrawGizmos()
        {
            if (grid == null) return;

            foreach (var node in grid)
            {
                Gizmos.color = node.isWalkable ? Color.green : Color.red;

                if (node.isOccupied) Gizmos.color = Color.blue;

                Gizmos.DrawWireCube(node.worldPos + Vector3.up * 0.1f, Vector3.one * tileSize * 0.9f);
            }
        }

    }
}
