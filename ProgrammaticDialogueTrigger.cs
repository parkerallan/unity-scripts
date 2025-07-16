using UnityEngine;
using System.Collections.Generic;

public class ProgrammaticDialogueTrigger : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform;
    
    private void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }
    

    /// Start a simple dialogue with just speaker and sentence
    
    public void StartSimpleDialogue(string speaker, string sentence)
    {
        Dialogue dialogue = CreateSimpleDialogue(speaker, sentence);
        TriggerDialogue(dialogue);
    }
    

    /// Start a dialogue with multiple lines
    
    public void StartMultiLineDialogue(string[] speakers, string[] sentences)
    {
        if (speakers.Length != sentences.Length)
        {
            Debug.LogError("ProgrammaticDialogueTrigger: Speakers and sentences arrays must be the same length!");
            return;
        }
        
        Dialogue dialogue = CreateMultiLineDialogue(speakers, sentences);
        TriggerDialogue(dialogue);
    }
    

    /// Start a dialogue with choices
    
    public void StartDialogueWithChoices(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions = null)
    {
        Dialogue dialogue = CreateDialogueWithChoices(speaker, sentence, choiceTexts, choiceActions);
        TriggerDialogue(dialogue);
    }
    

    /// Start a dialogue with a delay (useful for chaining dialogues)
    
    public void StartDelayedDialogue(string speaker, string sentence, float delay = 0.5f)
    {
        StartCoroutine(DelayedDialogueCoroutine(speaker, sentence, delay));
    }
    

    /// Start a choice dialogue with a delay
    
    public void StartDelayedDialogueWithChoices(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions = null, float delay = 0.5f)
    {
        StartCoroutine(DelayedChoiceDialogueCoroutine(speaker, sentence, choiceTexts, choiceActions, delay));
    }
    
    private System.Collections.IEnumerator DelayedDialogueCoroutine(string speaker, string sentence, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartSimpleDialogue(speaker, sentence);
    }
    
    private System.Collections.IEnumerator DelayedChoiceDialogueCoroutine(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions, float delay)
    {
        yield return new WaitForSeconds(delay);
        StartDialogueWithChoices(speaker, sentence, choiceTexts, choiceActions);
    }
    

    /// Create a simple dialogue with one line
    
    private Dialogue CreateSimpleDialogue(string speaker, string sentence)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.lines = new List<Dialogue.DialogueLine>();
        
        Dialogue.DialogueLine line = new Dialogue.DialogueLine();
        line.speaker = speaker;
        line.sentence = sentence;
        line.choices = new List<Dialogue.DialogueChoice>();
        
        dialogue.lines.Add(line);
        return dialogue;
    }
    

    /// Create a multi-line dialogue
    
    private Dialogue CreateMultiLineDialogue(string[] speakers, string[] sentences)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.lines = new List<Dialogue.DialogueLine>();
        
        for (int i = 0; i < speakers.Length; i++)
        {
            Dialogue.DialogueLine line = new Dialogue.DialogueLine();
            line.speaker = speakers[i];
            line.sentence = sentences[i];
            line.choices = new List<Dialogue.DialogueChoice>();
            
            dialogue.lines.Add(line);
        }
        
        return dialogue;
    }
    

    /// Create a dialogue with choices
    
    private Dialogue CreateDialogueWithChoices(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.lines = new List<Dialogue.DialogueLine>();
        
        Dialogue.DialogueLine line = new Dialogue.DialogueLine();
        line.speaker = speaker;
        line.sentence = sentence;
        line.choices = new List<Dialogue.DialogueChoice>();
        
        // Add choices
        for (int i = 0; i < choiceTexts.Length; i++)
        {
            Dialogue.DialogueChoice choice = new Dialogue.DialogueChoice();
            choice.choiceText = choiceTexts[i];
            choice.nextLineIndex = -1; // End dialogue after choice
            
            // Create UnityEvent for the choice action
            choice.onChoiceSelected = new UnityEngine.Events.UnityEvent();
            
            // Add the action if provided
            if (choiceActions != null && i < choiceActions.Length && choiceActions[i] != null)
            {
                // Capture the current index in a local variable to avoid closure issues
                int currentIndex = i;
                choice.onChoiceSelected.AddListener(() => choiceActions[currentIndex].Invoke());
            }
            
            line.choices.Add(choice);
        }
        
        dialogue.lines.Add(line);
        return dialogue;
    }
    

    /// Trigger the dialogue
    
    private void TriggerDialogue(Dialogue dialogue)
    {
        DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        
        if (dialogueManager == null)
        {
            Debug.LogError("ProgrammaticDialogueTrigger: DialogueManager not found in the scene!");
            return;
        }
        
        if (dialogueManager.IsDialogueActive())
        {
            Debug.LogWarning("ProgrammaticDialogueTrigger: Dialogue is already active!");
            return;
        }
        
        // Get the player's Rigidbody
        Rigidbody playerRigidbody = null;
        if (playerTransform != null)
        {
            playerRigidbody = playerTransform.GetComponent<Rigidbody>();
        }
        
        dialogueManager.DialogCanvas.SetActive(true);
        dialogueManager.StartDialogue(dialogue, playerRigidbody);
    }
}
