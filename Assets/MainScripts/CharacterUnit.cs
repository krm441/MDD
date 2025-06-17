using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PartyManagement
{
    public class CharacterUnit : MonoBehaviour
    {
        private Coroutine movementCoroutine;

        // For animation FSM
        public bool IsMoving => movementCoroutine != null;


        void Start()
        {
            FindObjectOfType<PartyManager>().AddMember(this);
        }

        public void MoveAlongPath(List<Pathfinding.Node> path)
        {
            // Cancel current movement, if any
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                Debug.Log("new movement");
            }

            // Start new movement
            movementCoroutine = StartCoroutine(FollowPath(path, 3f));
        }

        private IEnumerator FollowPath(List<Pathfinding.Node> path, float speed)
        {
            foreach (Pathfinding.Node node in path)
            {
                Vector3 targetPos = node.worldPos;
                targetPos.y = transform.position.y; // Prevent rotation tilt

                // Determine direction to look at (skip if already at target)
                Vector3 direction = targetPos - transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }

                while (Vector3.Distance(transform.position, targetPos) > 0.05f)
                {
                    // move
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
                    yield return null;
                }
            }

            movementCoroutine = null; // Reset after movement ends
        }
    }
}
