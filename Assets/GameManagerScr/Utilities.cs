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
