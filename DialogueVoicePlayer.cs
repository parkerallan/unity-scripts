using UnityEngine;
using System.Collections.Generic;

public class DialogueVoicePlayer : MonoBehaviour
{
    private Dictionary<char, AudioClip> femaleClips = new Dictionary<char, AudioClip>();
    
    void Start()
    {
        LoadClipsFromResources("female-voice");
    }

    private void LoadClipsFromResources(string folderName)
    {
        // Load all audio clips from the specified folder in the Resources directory
        AudioClip[] clips = Resources.LoadAll<AudioClip>(folderName);

        if (clips.Length == 0)
        {
            Debug.LogError($"No audio clips found in Resources/{folderName}. Ensure the folder exists and contains audio clips.");
            return;
        }

        foreach (var clip in clips)
        {
            if (clip != null && clip.name.Length > 0)
            {
                char key = char.ToUpper(clip.name[0]); // Use the first letter of the clip name as the key
                if (!femaleClips.ContainsKey(key))
                {
                    femaleClips[key] = clip;
                    Debug.Log($"Loaded clip: {clip.name} with key: {key}");
                }
                else
                {
                    Debug.LogWarning($"Duplicate key detected for clip: {clip.name}. Skipping this clip.");
                }
            }
            else
            {
                Debug.LogWarning("Encountered a null or invalid clip while loading.");
            }
        }
    }

    public void PlayDialogueSound(char c)
    {
        char key = char.ToUpper(c);
        if (femaleClips.ContainsKey(key))
        {
            SFXManager.instance.PlaySFXClip(femaleClips[key], transform, 1f);
            Debug.Log($"Playing sound for character: {key}");
        }
        else
        {
            Debug.LogWarning($"No sound found for character: {key}");
        }
    }

    public void PlayRandomDialogueSound()
    {
        if (femaleClips.Count == 0)
        {
            Debug.LogWarning("No audio clips available to play.");
            return;
        }

        // Get a random key from the dictionary
        List<char> keys = new List<char>(femaleClips.Keys);
        char randomKey = keys[Random.Range(0, keys.Count)];

        // Play the corresponding clip
        AudioClip clip = femaleClips[randomKey];
        if (clip != null)
        {
            Debug.Log($"Playing random sound for character: {randomKey}");
            SFXManager.instance.PlaySFXClip(clip, transform, 1f);
        }
    }
}
