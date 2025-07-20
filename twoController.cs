using UnityEngine;
using UnityEngine.Animations;

public class twoController : MonoBehaviour
{
  [Header("Animation Settings")]
    public Animator twoAnimator;
  public void playDeathAnimation()
  {
        if (twoAnimator != null)
        {
            twoAnimator.SetTrigger("Death");
        }
        else
        {
            Debug.LogError("twoAnimator is not assigned in twoController.");
        }
    }
}