using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class MovementController : MonoBehaviour
{
    private Coroutine movementCoroutine;

    public bool IsMoving => movementCoroutine != null;

    public void MoveAlongPath(List<Node> path, float speed = 3f)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        movementCoroutine = StartCoroutine(FollowPath(path, speed));
    }
        public void MoveAlongPath(List<Vector3> path, float speed = 3f)
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }

        movementCoroutine = StartCoroutine(FollowPath(path, speed));
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
    }

    private IEnumerator FollowPath(List<Node> path, float speed)
    {
        foreach (Node node in path)
        {
            Vector3 targetPos = node.worldPos;

            LookAtTarget(targetPos);

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
        }

        movementCoroutine = null;
    }

    public void LookAtTarget(Vector3 targetPos)
    {
        targetPos.y = transform.position.y;

        if ((targetPos - transform.position).sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.LookRotation(targetPos - transform.position);
        }
    }

    private IEnumerator FollowPath(List<Vector3> path, float speed)
    {
        foreach (var node in path)
        {
            Vector3 targetPos = node;
            targetPos.y = transform.position.y;

            if ((targetPos - transform.position).sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(targetPos - transform.position);
            }

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                yield return null;
            }
        }

        movementCoroutine = null;
    }
}
