using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PathVisualiser : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private static NavMeshPath currentPath;

    public void PreviewPath(NavMeshPath pathIn)
    {
        currentPath = pathIn;
       //if (currentPath != null && currentPath.corners.Length > 1)
       //{
       //    Gizmos.color = Color.red;
       //    for (int i = 0; i < currentPath.corners.Length - 1; i++)
       //    {
       //        Gizmos.DrawLine(currentPath.corners[i], currentPath.corners[i + 1]);
       //        Gizmos.DrawSphere(currentPath.corners[i], 0.1f);
       //    }
       //
       //    Gizmos.DrawSphere(currentPath.corners[currentPath.corners.Length - 1], 0.15f); // Last corner
       //}
    }

    public void Reset()
    {
        currentPath = null;
    }

    void OnDrawGizmos()
    {
        if (currentPath != null && currentPath.corners.Length > 1)
        {
            Gizmos.color = Color.red;
            for (int i = 0; i < currentPath.corners.Length - 1; i++)
            {
                Gizmos.DrawLine(currentPath.corners[i], currentPath.corners[i + 1]);
                Gizmos.DrawSphere(currentPath.corners[i], 0.1f);
            }

            Gizmos.DrawSphere(currentPath.corners[currentPath.corners.Length - 1], 0.15f); // Last corner
        }
    }
}
