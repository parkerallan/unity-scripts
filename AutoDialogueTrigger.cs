using UnityEngine;

public class AutoDialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue; // The dialogue to trigger
    private bool hasDialogueStarted = false; // Prevent re-triggering dialogue

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasDialogueStarted)
        {
            hasDialogueStarted = true;
            Debug.Log("Player entered the dialogue zone. Starting dialogue...");

            // Find the DialogueManager in the scene
            DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();

            if (dialogueManager == null)
            {
                Debug.LogError("DialogueManager not found in the scene!");
                return; // Exit to prevent further errors
            }

            // Get the player's Rigidbody
            Rigidbody playerRigidbody = other.GetComponent<Rigidbody>();

            if (!dialogueManager.IsDialogueActive())
            {
                TriggerDialogue(dialogueManager, playerRigidbody);
            }
        }
    }

    private void TriggerDialogue(DialogueManager dialogueManager, Rigidbody playerRigidbody)
    {
        dialogueManager.DialogCanvas.SetActive(true); // Activate the dialogue canvas
        dialogueManager.StartDialogue(dialogue, playerRigidbody); // Start the dialogue with the player's Rigidbody

        // Subscribe to the dialogue end event
        dialogueManager.OnDialogueEnd += HandleDialogueEnd;
    }

    private void HandleDialogueEnd()
    {
        Debug.Log("Dialogue ended. Destroying trigger.");

        // Unsubscribe from the OnDialogueEnd event
        DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        if (dialogueManager != null)
        {
            dialogueManager.OnDialogueEnd -= HandleDialogueEnd;
        }

        Destroy(gameObject); // Destroy the trigger after unsubscribing
    }
}
