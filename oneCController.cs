using UnityEngine;
using UnityEngine.Animations;

public class oneCController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator bossAnimator;

    [Header("Constraint Settings")]
    public ParentConstraint parentConstraint;

    [Header("Optional Settings")]
    public bool playSwapSoundEffect = true;
    public AudioClip swapSoundClip;

    private void Start()
    {
        // Auto-assign components if not manually set
        if (bossAnimator == null)
            bossAnimator = GetComponent<Animator>();

        if (parentConstraint == null)
            parentConstraint = GetComponent<ParentConstraint>();
    }

    public void TriggerSwapWithConstraint()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Swap");
        }

        // Delay constraint activation by 1 second
        Invoke(nameof(ActivateConstraint), 1.0f);

        if (playSwapSoundEffect && swapSoundClip != null)
        {
            SFXManager.instance?.PlaySFXClip(swapSoundClip, transform, 1f);
        }
    }

    private void ActivateConstraint()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = true;
        }
    }


    public void DeactivateParentConstraint()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = false;
        }
    }

    public void ToggleParentConstraint()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = !parentConstraint.constraintActive;
        }
    }

    public bool IsConstraintActive()
    {
        return parentConstraint != null && parentConstraint.constraintActive;
    }
    
    public void PlayDeathAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Death");
        }
        else
        {
            Debug.LogWarning("Boss Animator is not assigned. Cannot play death animation.");
        }
    }
}
