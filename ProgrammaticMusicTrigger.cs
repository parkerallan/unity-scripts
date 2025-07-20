using UnityEngine;
using System.Collections;

public class ProgrammaticMusicTrigger : MonoBehaviour
{
    [Header("Audio Components")]
    public AudioSource musicAudioSource; // Drag your music AudioSource here
    
    [Header("Settings")]
    public float fadeInDuration = 1f; // Time to fade in new music
    public float fadeOutDuration = 1f; // Time to fade out current music
    
    private AudioClip currentClip;
    private bool isTransitioning = false;
    
    private void Start()
    {
        // Auto-setup AudioSource if not assigned
        if (musicAudioSource == null)
        {
            // First try to find by component on this GameObject
            musicAudioSource = GetComponent<AudioSource>();
            
            // If not found, try to find an AudioSource named "music" anywhere in the scene
            if (musicAudioSource == null)
            {
                GameObject musicObject = GameObject.Find("Music");
                if (musicObject != null)
                {
                    musicAudioSource = musicObject.GetComponent<AudioSource>();
                }
            }
            
            // If still not found, try to find any AudioSource in children
            if (musicAudioSource == null)
            {
                musicAudioSource = GetComponentInChildren<AudioSource>();
            }
        }
    }
    
    /// <summary>
    /// Start playing a song with specified settings
    /// </summary>
    /// <param name="audioPath">Path to the audio file (e.g., "Assets/Audio/Music/KickingInDoors")</param>
    /// <param name="looping">1 for looping, 0 for no looping</param>
    public void StartSong(string audioPath, int looping)
    {
        StartSong(audioPath, looping == 1, 1f);
    }
    
    /// <summary>
    /// Start playing a song with specified settings and volume
    /// </summary>
    /// <param name="audioPath">Path to the audio file</param>
    /// <param name="looping">True for looping, false for no looping</param>
    /// <param name="volume">Volume level (0-1)</param>
    public void StartSong(string audioPath, bool looping, float volume = 1f)
    {
        if (musicAudioSource == null)
        {
            Debug.LogError("ProgrammaticMusicTrigger: No AudioSource assigned!");
            return;
        }
        
        // Load the audio clip from Resources folder
        AudioClip newClip = LoadAudioClip(audioPath);
        if (newClip == null)
        {
            Debug.LogError($"ProgrammaticMusicTrigger: Could not load audio clip from path: {audioPath}");
            return;
        }
        
        // Start transition to new music
        StartCoroutine(TransitionToNewMusic(newClip, looping, volume));
    }
    
    /// <summary>
    /// Stop the currently playing music
    /// </summary>
    public void StopMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            StartCoroutine(FadeOutMusic());
        }
    }
    
    /// <summary>
    /// Pause the currently playing music
    /// </summary>
    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
    }
    
    /// <summary>
    /// Resume paused music
    /// </summary>
    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying)
        {
            musicAudioSource.UnPause();
        }
    }
    
    /// <summary>
    /// Set music volume
    /// </summary>
    /// <param name="volume">Volume level (0-1)</param>
    public void SetVolume(float volume)
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = Mathf.Clamp01(volume);
        }
    }
    
    private AudioClip LoadAudioClip(string audioPath)
    {
        // Remove file extension and "Assets/" prefix for Resources.Load
        string resourcesPath = audioPath;
        
        // Remove "Assets/" prefix if present
        if (resourcesPath.StartsWith("Assets/"))
        {
            resourcesPath = resourcesPath.Substring(7);
        }
        
        // Remove file extension
        if (resourcesPath.Contains("."))
        {
            resourcesPath = resourcesPath.Substring(0, resourcesPath.LastIndexOf('.'));
        }
        
        // Try to load from Resources folder
        AudioClip clip = Resources.Load<AudioClip>(resourcesPath);
        
        if (clip == null)
        {
            // If not found in Resources, try loading directly (for testing)
            Debug.LogWarning($"ProgrammaticMusicTrigger: Could not load {resourcesPath} from Resources. Make sure the audio file is in a Resources folder.");
        }
        
        return clip;
    }
    
    private IEnumerator TransitionToNewMusic(AudioClip newClip, bool looping, float targetVolume)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        float originalVolume = musicAudioSource.volume;
        
        // Fade out current music if playing
        if (musicAudioSource.isPlaying)
        {
            yield return StartCoroutine(FadeOutMusic(false));
        }
        
        // Set up new music
        musicAudioSource.clip = newClip;
        musicAudioSource.loop = looping;
        musicAudioSource.volume = 0f;
        musicAudioSource.Play();
        
        currentClip = newClip;
        
        // Fade in new music
        float fadeTimer = 0f;
        while (fadeTimer < fadeInDuration)
        {
            fadeTimer += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(0f, targetVolume, fadeTimer / fadeInDuration);
            yield return null;
        }
        
        musicAudioSource.volume = targetVolume;
        isTransitioning = false;
        
        Debug.Log($"ProgrammaticMusicTrigger: Started playing {newClip.name} (Looping: {looping})");
    }
    
    private IEnumerator FadeOutMusic(bool stopAfterFade = true)
    {
        if (musicAudioSource == null || !musicAudioSource.isPlaying) yield break;
        
        float startVolume = musicAudioSource.volume;
        float fadeTimer = 0f;
        
        while (fadeTimer < fadeOutDuration && musicAudioSource.isPlaying)
        {
            fadeTimer += Time.deltaTime;
            musicAudioSource.volume = Mathf.Lerp(startVolume, 0f, fadeTimer / fadeOutDuration);
            yield return null;
        }
        
        if (stopAfterFade && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
            musicAudioSource.volume = startVolume;
        }
    }
    
    // Utility methods for checking music state
    public bool IsPlaying()
    {
        return musicAudioSource != null && musicAudioSource.isPlaying;
    }
    
    public bool IsLooping()
    {
        return musicAudioSource != null && musicAudioSource.loop;
    }
    
    public string GetCurrentSongName()
    {
        return currentClip != null ? currentClip.name : "None";
    }
}
