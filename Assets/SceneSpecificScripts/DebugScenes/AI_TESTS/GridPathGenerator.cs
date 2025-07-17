using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GridPathGenerator : MonoBehaviour
{
    [Header("Pathfinder")]
    [Tooltip("Reference to the pathfinding system.")]
    public Pathfinding.GridSystem pathFinder;

    // Start is called before the first frame update
    void Start()
    {
        pathFinder.GeneratePathThetta(100, 100, true);
        Debug.Log("generated");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
