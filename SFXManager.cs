using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;
    [SerializeField] private AudioSource SFXObject;
    private void Awake()
    {
        if (instance == null) {
            instance = this;
        }
    }

    public void PlaySFXClip(AudioClip audioClip, Transform spawnTransform, float volume)
    {
        // Play the sound effect for the clip
        AudioSource audioSource = Instantiate(SFXObject, spawnTransform.position, Quaternion.identity);
        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.Play();
        float clipLength = audioClip.length;
        Destroy(audioSource.gameObject, clipLength);

    }
}
