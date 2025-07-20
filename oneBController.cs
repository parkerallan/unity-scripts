using UnityEngine;
using UnityEngine.Animations;

public class oneBController : MonoBehaviour
{
  [Header("Animation Settings")]
    public Animator oneBAnimator;
  public void playDeathAnimation()
  {
        if (oneBAnimator != null)
        {
            oneBAnimator.SetTrigger("Death");
        }
        else
        {
            Debug.LogError("oneBAnimator is not assigned in oneBController.");
        }
    }
}