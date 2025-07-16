using System;
using System.Collections;
using UnityEngine;

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