using UnityEngine;
using System;
using System.Collections;

public class ProjectileLinear : MonoBehaviour
{
    public float speed = 10f;
    private Vector3 start, end, direction;
    private float distance;
    private Action onImpact;

    private bool launched = false;

    public void Launch(Vector3 start, Vector3 end, Action onHit = null)
    {
        this.start = start;
        this.end = end;
        this.onImpact = onHit;

        transform.position = start;
        direction = (end - start).normalized;
        distance = Vector3.Distance(start, end);
        transform.rotation = Quaternion.LookRotation(direction);
        transform.Rotate(90f, 0f, 0f);
        launched = true;
    }

    private void Update()
    {
        if (!launched) return;

        float step = speed * Time.deltaTime;
        float remaining = Vector3.Distance(transform.position, end);

        if (step >= remaining)
        {
            transform.position = end;
            launched = false;
            onImpact?.Invoke();
            Destroy(gameObject);
        }
        else
        {
            transform.position += direction * step;
        }
    }
}
