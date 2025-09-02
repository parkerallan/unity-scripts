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
            
        // Auto-find player head (WaterLevel on CharModel1)
        FindPlayerHead();
    }
    
    private void FindPlayerHead()
    {
        if (playerHead == null)
        {
            // Look for CharModel1 first
            GameObject charModel = GameObject.Find("CharModel1");
            if (charModel != null)
            {
                // Look for WaterLevel child object
                Transform waterLevel = charModel.transform.Find("WaterLevel");
                if (waterLevel != null)
                {
                    playerHead = waterLevel;
                    Debug.Log("UnderwaterOverlay: Auto-found WaterLevel on CharModel1");
                }
                else
                {
                    Debug.LogWarning("UnderwaterOverlay: CharModel1 found but no WaterLevel child object");
                }
            }
            else
            {
                // Fallback: look for any GameObject named WaterLevel
                GameObject waterLevelObj = GameObject.Find("WaterLevel");
                if (waterLevelObj != null)
                {
                    playerHead = waterLevelObj.transform;
                    Debug.Log("UnderwaterOverlay: Auto-found WaterLevel object");
                }
                else
                {
                    Debug.LogWarning("UnderwaterOverlay: Could not find CharModel1 or WaterLevel");
                }
            }
        }
    }

    private void Start()
    {
        // Double-check player head in case it wasn't available during Awake
        if (playerHead == null)
        {
            Debug.Log("UnderwaterOverlay: Re-checking for player head in Start()");
            FindPlayerHead();
        }
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
