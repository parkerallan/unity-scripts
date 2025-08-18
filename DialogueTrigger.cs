using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;
    public GameObject markerEffect;
    [Header("Trigger Options")]
    public bool disableAfterDialogue = false; // Checkbox to disable trigger after dialogue ends
    private bool isPlayerInRange = false;
    private bool hasDialogueEnded = false;
    private bool isTriggerDisabled = false; // Track if trigger is permanently disabled
    private Rigidbody playerRigidbody;

    // Public method to check if player is in dialogue range
    public bool IsPlayerInDialogueRange()
    {
        return isPlayerInRange && !isTriggerDisabled;
    }

    // --- Smooth rotation fields ---
    public float rotationSpeed = 5f; // Degrees per second
    private Quaternion? playerTargetRotation = null;
    private Quaternion? npcTargetRotation = null;
    private GameObject playerObj = null;
    // -----------------------------

    void Update()
    {
        // Skip all functionality if trigger is disabled
        if (isTriggerDisabled) return;
        
        // Dialogue trigger logic
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && !hasDialogueEnded)
        {
            DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
            if (!dialogueManager.IsDialogueActive())
            {
                TriggerDialogue(dialogueManager);
            }
        }

        // --- Smooth rotation logic ---
        if (playerObj != null && playerTargetRotation.HasValue)
        {
            playerObj.transform.rotation = Quaternion.RotateTowards(
                playerObj.transform.rotation,
                playerTargetRotation.Value,
                rotationSpeed * Time.deltaTime * 60f
            );
            if (Quaternion.Angle(playerObj.transform.rotation, playerTargetRotation.Value) < 1f)
                playerTargetRotation = null;
        }
        if (npcTargetRotation.HasValue)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                npcTargetRotation.Value,
                rotationSpeed * Time.deltaTime * 60f
            );
            if (Quaternion.Angle(transform.rotation, npcTargetRotation.Value) < 1f)
                npcTargetRotation = null;
        }
        // -----------------------------
    }

    private void OnTriggerEnter(Collider other)
    {
        // Skip if trigger is disabled
        if (isTriggerDisabled) return;
        
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerRigidbody = other.GetComponent<Rigidbody>();
            if (markerEffect != null)
                markerEffect.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Skip if trigger is disabled
        if (isTriggerDisabled) return;
        
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (markerEffect != null)
                markerEffect.SetActive(false);
        }
    }

    private void TriggerDialogue(DialogueManager dialogueManager)
    {
        // Make both characters face each other smoothly
        FaceEachOther();

        dialogueManager.DialogCanvas.SetActive(true);
        Animator npcAnimator = GetComponent<Animator>();
        dialogueManager.StartDialogue(dialogue, playerRigidbody, npcAnimator);
        dialogueManager.OnDialogueEnd += HandleDialogueEnd;
    }

    private void FaceEachOther()
    {
        playerObj = playerRigidbody != null ? playerRigidbody.gameObject : null;
        if (playerObj == null) return;

        Vector3 npcPosition = transform.position;
        Vector3 playerPosition = playerObj.transform.position;

        Vector3 toNPC = (npcPosition - playerPosition);
        Vector3 toPlayer = (playerPosition - npcPosition);

        toNPC.y = 0;
        toPlayer.y = 0;

        if (toNPC.sqrMagnitude > 0.001f)
            playerTargetRotation = Quaternion.LookRotation(toNPC);

        if (toPlayer.sqrMagnitude > 0.001f)
            npcTargetRotation = Quaternion.LookRotation(toPlayer);
    }

    private void HandleDialogueEnd()
    {
        hasDialogueEnded = true;
        
        if (disableAfterDialogue)
        {
            // Permanently disable the trigger functionality
            isTriggerDisabled = true;
            isPlayerInRange = false;
            // Hide marker effect if it exists
            if (markerEffect != null)
                markerEffect.SetActive(false);
        }
        else
        {
            // Original behavior - reset after delay
            if (this != null && gameObject != null && enabled)
            {
                Invoke(nameof(ResetDialogue), 1f);
            }
        }
    }

    private void ResetDialogue()
    {
        hasDialogueEnded = false;
    }
}
