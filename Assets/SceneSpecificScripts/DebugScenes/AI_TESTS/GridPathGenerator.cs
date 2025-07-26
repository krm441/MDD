using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GridPathGenerator : MonoBehaviour
{
    [Header("Pathfinder")]
    [Tooltip("Reference to the pathfinding system.")]
    public Pathfinding.GridSystem pathFinder;

    [SerializeField] private Vector2Int dimentions = new Vector2Int(62, 20);

    // Start is called before the first frame update
    void Start()
    {
        //pathFinder.GeneratePathThetta(100, 100, true);
        
        //Console.Log("generated", dimentions);
    }

    public void Initialise()
    {
        pathFinder.GeneratePathThetta(dimentions.x, dimentions.y, true);
    }
}
