using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BridgesController : MonoBehaviour
{
    [SerializeField] private GameObject bridge;
    [SerializeField] private float duration = 3.0f;
    [SerializeField] private NavMeshSurface navMesh;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            RaiseTheBridge();
    }

    public void RaiseTheBridge()
    {
        StartCoroutine(RaiseTheBridge(bridge.transform, duration));
    }

    private IEnumerator RaiseTheBridge(Transform t, float time)
    {
        Quaternion startRot = t.localRotation;

        Vector3 e = t.localEulerAngles;
        float startZ = e.z;
        float targetZ = 0f;

        float endZ = startZ + Mathf.DeltaAngle(startZ, targetZ);
        Quaternion targetRot = Quaternion.Euler(e.x, e.y, endZ);

        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t01 = Mathf.Clamp01(elapsed / time);
            t.localRotation = Quaternion.Slerp(startRot, targetRot, t01);
            yield return null;
        }

        t.localRotation = targetRot;

        // builkd the nav mesh again
        navMesh.BuildNavMesh();
    }
}
