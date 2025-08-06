using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] public Animator animator;
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void PlayAnimation(string triggerName)
    {
        if (animator == null)
        {
            Console.Warn("No animator found.");
            return;
        }

        animator.SetTrigger(triggerName);
    }
}
