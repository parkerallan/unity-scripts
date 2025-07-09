using UnityEngine;
using UnityEngine.Audio; // Required for AudioMixer control

public class UnderwaterOverlay : MonoBehaviour
{
    public GameObject overlayImage; // Assign your UI Image GameObject
    public Transform playerHead;    // Assign the player's camera or head transform
    public AudioMixer audioMixer;   // Assign your AudioMixer asset in the Inspector
    
    [Header("Offset Settings")]
    public float headOffset = 0.2f; // Vertical offset for head position check (positive = higher threshold)

    private BoxCollider boxCollider;
    private bool isUnderwater = false;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (!boxCollider.isTrigger)
            boxCollider.isTrigger = true;
    }

    private void Update()
    {
        if (playerHead == null || overlayImage == null || boxCollider == null || audioMixer == null)
            return;

        // Check if the player's head is within the collider's bounds, considering the offset
        bool isHeadInside = boxCollider.bounds.Contains(playerHead.position + Vector3.up * headOffset);

        if (isHeadInside != isUnderwater)
        {
            isUnderwater = isHeadInside;
            overlayImage.SetActive(isUnderwater);

            if (isUnderwater)
            {
                // Apply underwater audio effects
                audioMixer.SetFloat("LowpassCutoff", 800f); // muffle high frequencies
                audioMixer.SetFloat("ReverbLevel", 0f);     // enable reverb
            }
            else
            {
                // Restore normal audio
                audioMixer.SetFloat("LowpassCutoff", 22000f);
                audioMixer.SetFloat("ReverbLevel", -80f);   // essentially off
            }
        }
    }
}
