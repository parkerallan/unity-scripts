using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    public AudioClip musicClip; // The music track to play
    public bool loop = true;    // Should the music loop?
    [Range(0f, 1f)]
    public float volume = 1f;   // Music volume
    public bool disableAfterTrigger = false; // Should this trigger only work once?

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && disableAfterTrigger)
            return;

        if (other.CompareTag("Player") && musicClip != null)
        {
            GameObject musicObj = GameObject.Find("Music");
            if (musicObj != null)
            {
                AudioSource audioSource = musicObj.GetComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.clip = musicClip;
                    audioSource.loop = loop;
                    audioSource.volume = volume;
                    audioSource.Play();
                }
                else
                {
                    Debug.LogWarning("No AudioSource found on the 'Music' object.");
                }
            }
            else
            {
                Debug.LogWarning("No GameObject named 'Music' found in the scene.");
            }

            if (disableAfterTrigger)
                hasTriggered = true;
        }
    }
}