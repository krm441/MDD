using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavigationManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        //NavMeshManager.BuildNavMesh();
    }

}

public static class PathReachability
{
    static NavMeshPath path;

    public static bool CanReach(Vector3 fromPos, Vector3 toPos, int areaMask)
    {
        if (path == null) path = new NavMeshPath();

        // Sample pos
        if (!NavMesh.SamplePosition(toPos, out var hit, 0.6f, areaMask))
            return false;

        // Calculate a path
        NavMesh.CalculatePath(fromPos, hit.position, areaMask, path);
        return path.status == NavMeshPathStatus.PathComplete;
    }
}