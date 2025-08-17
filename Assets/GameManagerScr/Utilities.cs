using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public static class TimerUtility
{
    public static void WaitAndDo(MonoBehaviour context, float delay, Action callback)
    {
        context.StartCoroutine(WaitRoutine(delay, callback));
    }

    private static IEnumerator WaitRoutine(float delay, Action callback)
    {
        yield return new WaitForSeconds(delay);
        callback?.Invoke();
    }
}

public static class MathMDD
{
    public static float CalculatePathDistance(NavMeshPath path)
    {
        float ret = 0.0f;

        if (path == null || path.corners == null || path.corners.Length < 2)
            return 0.0f;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            ret += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }

        return ret;
    }

    public static Vector3 ProjectToNavMesh(Vector3 position, float maxDistance = 1f, int areaMask = NavMesh.AllAreas)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, maxDistance, areaMask))
            return hit.position;

        // Fallback
        return position;
    }

}

public static class MouseTracker
{
    private static Vector3 lastPosition;
    private static bool mouseMoved;

    public static bool MouseMovedThisFrame => mouseMoved;

    public static void Update()
    {
        Vector3 currentPos = Input.mousePosition;
        mouseMoved = currentPos != lastPosition;
        lastPosition = currentPos;
    }
}

public class CoroutineHandle
{
    public bool IsRunning { get; private set; }
    public bool IsCompleted { get; private set; }

    private MonoBehaviour owner;
    private Coroutine routine;

    public CoroutineHandle(MonoBehaviour owner, IEnumerator coroutine)
    {
        this.owner = owner;
        routine = owner.StartCoroutine(Run(coroutine));
    }

    private IEnumerator Run(IEnumerator coroutine)
    {
        IsRunning = true;
        yield return coroutine;
        IsRunning = false;
        IsCompleted = true;
    }

    public void Stop()
    {
        if (IsRunning)
        {
            owner.StopCoroutine(routine);
            IsRunning = false;
            IsCompleted = false;
        }
    }
}