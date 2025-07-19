using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems; // this should be moved out
using UnityEngine;

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

        public void GeneratePathThetta(int width, int height, bool walkable)
        {
            GenerateGrid(width, height, walkable);

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
        float tileSize = 1f;

        void GenerateGrid(int width, int height, bool walkable = false)
        {
            grid = new Node[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = new Vector3(x * tileSize, 0, y * tileSize);
                    //grid[x, y] = new Node(new Vector2Int(x, y), worldPos, false);
                    grid[x, y] = new Node(new Vector2Int(x, y), worldPos, walkable);
                }
            }
        }

        private GameObject currentClickMarker; // marker prefab reference
               
        public List<Node> FindPathTo(Vector3 to, Vector3 from)
        {
            Node startNode = GetNodeFromWorldPosition(from);
            Node endNode = GetNodeFromWorldPosition(to);


            if (startNode != null && endNode != null && endNode.isWalkable)
            {
                return thetaStar.FindPath(startNode, endNode);
            }

            return null;
        }

        // better method for path construction on click
        // from point 'from' to the point where ray hit the clickable layer
        // TODO: fix, and research the event syste,
        public List<Node> FindPathToClick(Transform from)
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
                        return thetaStar.FindPath(startNode, endNode);
                    }

                    
                }
            //}

            return null;
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
            //Debug.Log("x y: " + x + " " + y + ' ' + tileSize + " " + grid == null);
            if (x >= 0 && y >= 0 && x < grid.GetLength(0) && y < grid.GetLength(1))
            {
                return grid[x, y];
            }

            return null;
        }

        // DEBUG - visual debugging 
        void OnDrawGizmos()
        {
            if (grid == null) return;

            foreach (var node in grid)
            {
                Gizmos.color = node.isWalkable ? Color.green : Color.red;
                Gizmos.DrawWireCube(node.worldPos + Vector3.up * 0.1f, Vector3.one * tileSize * 0.9f);
            }
        }

    }
}
