using UnityEngine;

public class SFXTrigger : MonoBehaviour
{
    public AudioClip sfxClip; // The sound effect to play
    public float volume = 1f; // Volume of the sound effect
    public bool disableAfterTrigger = false; // Should this trigger only work once?

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && disableAfterTrigger)
            return;

        if (other.CompareTag("Player") && sfxClip != null && SFXManager.instance != null)
        {
            // Play the sound effect at the player's position
            SFXManager.instance.PlaySFXClip(sfxClip, other.transform, volume);

            if (disableAfterTrigger)
                hasTriggered = true;
        }
    }
}