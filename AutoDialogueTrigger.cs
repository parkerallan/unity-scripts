using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue; // The dialogue to trigger
    private bool hasDialogueStarted = false; // Prevent re-triggering dialogue
    private Animator playerAnimator; // Store reference to player animator
    private MonoBehaviour playerController; // Store reference to player controller script

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasDialogueStarted)
        {
            hasDialogueStarted = true;
           //Debug.Log("Player entered the dialogue zone. Starting dialogue...");

            // Find the DialogueManager in the scene
            DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();

            if (dialogueManager == null)
            {
               //Debug.LogError("DialogueManager not found in the scene!");
                return; // Exit to prevent further errors
            }

            // Get the player's Rigidbody
            Rigidbody playerRigidbody = other.GetComponent<Rigidbody>();
            
            // Get the player's Animator
            playerAnimator = other.GetComponent<Animator>();
            
            // Get the player controller script to disable movement
            playerController = other.GetComponent<PlayerController>();
            if (playerController == null)
            {
                playerController = other.GetComponent<UnderwaterPlayerController>();
            }

            if (!dialogueManager.IsDialogueActive())
            {
                TriggerDialogue(dialogueManager, playerRigidbody);
            }
        }
    }

    private void TriggerDialogue(DialogueManager dialogueManager, Rigidbody playerRigidbody)
    {
        dialogueManager.DialogCanvas.SetActive(true); // Activate the dialogue canvas
        
        // Disable player animations
        if (playerAnimator != null)
        {
            playerAnimator.enabled = false;
        }
        
        // Disable player movement controller
        if (playerController != null)
        {
            playerController.enabled = false;
           //Debug.Log("Player controller disabled for dialogue");
        }
        
        // Stop player rigidbody movement
        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
        
        dialogueManager.StartDialogue(dialogue, playerRigidbody); // Start the dialogue with the player's Rigidbody

        // Subscribe to the dialogue end event
        dialogueManager.OnDialogueEnd += HandleDialogueEnd;
    }

    private void HandleDialogueEnd()
    {
       //Debug.Log("Dialogue ended. Re-enabling player animations and movement, then destroying trigger.");

        // Re-enable player animations
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
        }
        
        // Re-enable player movement controller
        if (playerController != null)
        {
            playerController.enabled = true;
           //Debug.Log("Player controller re-enabled after dialogue");
        }

        // Unsubscribe from the OnDialogueEnd event
        DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueEnd -= HandleDialogueEnd;
        }

        Destroy(gameObject); // Destroy the trigger after unsubscribing
    }
}
