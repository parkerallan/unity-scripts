using UnityEngine;
using UnityEngine.Audio;

public class SoundMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer; // Reference to the AudioMixer

    public void SetMasterVolume(float volume)
    {
        //audioMixer.SetFloat("masterVolume", volume);
        audioMixer.SetFloat("masterVolume", Mathf.Log10(volume) * 20);
    }
    public void SetMusicVolume(float volume)
    {
        //audioMixer.SetFloat("musicVolume", volume);
        audioMixer.SetFloat("musicVolume", Mathf.Log10(volume) * 20);
    }
    public void SetSFXVolume(float volume)
    {
        //audioMixer.SetFloat("sfxVolume", volume);
        audioMixer.SetFloat("sfxVolume", Mathf.Log10(volume) * 20);
    }
}
