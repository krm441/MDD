using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ProjectileBallistic : MonoBehaviour
{
    public float flightTime = 1.0f;
    private Vector3 start, end;
    private float elapsed = 0f;
    private Action onImpact;

    public void Launch(Vector3 start, Vector3 end, Action onHit = null)
    {
        this.start = start;
        this.end = end;
        this.onImpact = onHit;
        transform.position = start;
        StartCoroutine(Fly());
    }

    private IEnumerator Fly()
    {
        Vector3 control = (start + end) / 2 + Vector3.up * 3f; // arc height

        while (elapsed < flightTime)
        {
            float t = elapsed / flightTime;
            Vector3 a = Vector3.Lerp(start, control, t);
            Vector3 b = Vector3.Lerp(control, end, t);
            transform.position = Vector3.Lerp(a, b, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = end;

        onImpact?.Invoke();
        Destroy(gameObject);
    }
}
