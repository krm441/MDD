using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class NavMeshManager
{
    public static int pathLayer = LayerMask.NameToLayer("Walkable");
    public static void AddFloorToNavMeshLayer(GameObject go)
    {
        go.layer = pathLayer;
    }

    public static void BuildNavMesh()
    {
        var navMesh = UnityEngine.GameObject.FindObjectOfType<NavMeshSurface>();

        navMesh.BuildNavMesh();
    }

}
