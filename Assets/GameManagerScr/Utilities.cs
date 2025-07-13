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