using UnityEngine;
using UnityEngine.Animations;

public class twoBController : MonoBehaviour
{
  [Header("Animation Settings")]
    public Animator twoBAnimator;
  public void playDeathAnimation()
  {
        if (twoBAnimator != null)
        {
            twoBAnimator.SetTrigger("Death");
        }
        else
        {
            Debug.LogError("twoBAnimator is not assigned in twoBController.");
        }
    }
}