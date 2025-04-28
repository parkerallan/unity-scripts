using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public Dialogue dialogue;
    public GameObject markerEffect; // Reference to the particle effect
    private bool isPlayerInRange = false;
    private bool hasDialogueEnded = false; // Track if the dialogue has ended

    private Rigidbody playerRigidbody; // Reference to the player's Rigidbody

    void Update()
    {
        // Check if the player is in range and presses the "E" key
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && !hasDialogueEnded)
        {
            DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();

            // Only trigger dialogue if it is not already active
            if (!dialogueManager.IsDialogueActive())
            {
                TriggerDialogue(dialogueManager);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            Debug.Log("Player is in range. Press 'E' to start dialogue.");

            // Get the player's Rigidbody
            playerRigidbody = other.GetComponent<Rigidbody>();

            // Activate the particle effect
            if (markerEffect != null)
            {
                markerEffect.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting the trigger is the player
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            Debug.Log("Player left the range.");

            // Deactivate the particle effect
            if (markerEffect != null)
            {
                markerEffect.SetActive(false);
            }
        }
    }

    private void TriggerDialogue(DialogueManager dialogueManager)
    {
        dialogueManager.DialogCanvas.SetActive(true); // Activate the canvas
        dialogueManager.StartDialogue(dialogue, playerRigidbody); // Pass the player's Rigidbody

        // Subscribe to the dialogue end event
        dialogueManager.OnDialogueEnd += HandleDialogueEnd;
    }

    private void HandleDialogueEnd()
    {
        hasDialogueEnded = true; // Mark the dialogue as ended

        // Optionally reset the dialogue after a delay or condition
        Invoke(nameof(ResetDialogue), 1f); // Reset after 1 second (adjust as needed)
    }

    private void ResetDialogue()
    {
        hasDialogueEnded = false; // Allow the dialogue to be triggered again
    }
}
