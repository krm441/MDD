using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Pathfinding
{
    [ExecuteInEditMode]
    public class GridSystem : MonoBehaviour
    {
        public void GeneratePathfinder(int width, int height)
        {
            aStar = new AStar();
            GenerateGrid(width, height);
            aStar.SetGrid(grid);
        }

        public void MarkWalkable(Vector2Int gridPos, bool isWalkable_)
        {
            if (gridPos.x >= 0 && gridPos.y >= 0 && gridPos.x < grid.GetLength(0) && gridPos.y < grid.GetLength(1))
            {
                grid[gridPos.x, gridPos.y].isWalkable = isWalkable_;
            }
        }

        public GameObject clickMarkerPrefab;

        // Update is called once per frame
        void Update()
        {
            DetectClick();
        }

        public PartyManagement.PartyManager partyManager;

        public AStar aStar;

        public LayerMask clickableLayer;

        Node[,] grid;
        float tileSize = 1f;

        void GenerateGrid(int width, int height)//, float tileSize)
        {
            grid = new Node[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 worldPos = new Vector3(x * tileSize, 0, y * tileSize);
                    grid[x, y] = new Node(new Vector2Int(x, y), worldPos, false);
                }
            }
        }

        private GameObject currentClickMarker; // marker prefab reference

        void DetectClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Debug.Log("RaycastSent");

                // Only raycast against objects in the "clickableLayer" layer mask
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayer))
                {        
                    Vector3 worldPos = hit.point;
                    Node clickedNode = GetNodeFromWorldPosition(worldPos);

                    Debug.Log("RaycastHit "  + worldPos);

                    if (clickedNode != null && clickedNode.isWalkable)
                    {
                        Debug.Log("Clicked node: " + clickedNode.gridPos);

                        Node startNode = GetNodeFromWorldPosition(partyManager.CurrentSelected.transform.position);
                        Node endNode = GetNodeFromWorldPosition(hit.point);

                        List<Node> path = aStar.FindPath(startNode, endNode);
                        partyManager.CurrentSelected.MoveAlongPath(path);

                        // Visual marker
                        if (clickMarkerPrefab != null)
                        {
                             // Destroy the previous marker if it still exists
                            if (currentClickMarker != null)
                            {
                                Destroy(currentClickMarker);
                            }

                            Quaternion rotation = Quaternion.Euler(90f, 0f, 0f); // Flat on ground
                            Vector3 pos = hit.point + new Vector3(0f, 0.1f, 0f); // Prevent Z-fighting

                            // Spawn new marker and keep reference
                            currentClickMarker = Instantiate(clickMarkerPrefab, pos, rotation);

                            // Auto-destroy after 1.5 seconds and clear the reference
                            Destroy(currentClickMarker, 1.5f); // Lifetime = 1.5 seconds
                        }

                    }
                }
            }
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
            Debug.Log("x y: " + x + " " + y + ' ' + tileSize + " " + grid == null);
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
