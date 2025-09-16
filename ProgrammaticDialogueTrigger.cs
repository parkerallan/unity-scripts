using UnityEngine;
using System.Collections.Generic;

public class ProgrammaticDialogueTrigger : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform playerTransform;
    
    private void Start()
    {
        // Always try to find player, even if one is assigned (in case of scene changes)
        FindAndAssignPlayer();
    }
    
    private void FindAndAssignPlayer()
    {
        // Try to find the player if not assigned or if the current reference is invalid
        if (playerTransform == null || playerTransform.gameObject.scene.name == null) // scene.name is null for DontDestroyOnLoad objects
        {
            GameObject foundPlayer = GameObject.FindWithTag("Player");
            
            if (foundPlayer != null)
            {
                // If our current playerTransform is null or invalid, assign the found player
                if (playerTransform == null)
                {
                    playerTransform = foundPlayer.transform;
                   //Debug.Log($"ProgrammaticDialogueTrigger: Auto-assigned player: {foundPlayer.name}");
                }
                // If we have a playerTransform but it's a scene-based duplicate, prefer the DontDestroyOnLoad one
                else if (playerTransform.gameObject.scene.name != null && foundPlayer.scene.name == null)
                {
                    playerTransform = foundPlayer.transform;
                   //Debug.Log($"ProgrammaticDialogueTrigger: Switched to DontDestroyOnLoad player: {foundPlayer.name}");
                }
            }
            
            if (playerTransform == null)
            {
               //Debug.LogWarning("ProgrammaticDialogueTrigger: No player GameObject found with 'Player' tag.");
            }
        }
    }
    

    /// <summary>
    /// Start a simple dialogue with just speaker and sentence
    /// </summary>
    /// <param name="speaker">Name of the speaker</param>
    /// <param name="sentence">What the speaker says</param>
    /// <param name="characterImagePath">Optional path to character sprite (e.g., "Images/Characters/Guard")</param>
    /// <param name="faceRight">Optional orientation: true = face right, false = face left</param>
    public void StartSimpleDialogue(string speaker, string sentence, string characterImagePath = null, bool faceRight = true)
    {
        Dialogue dialogue = CreateSimpleDialogue(speaker, sentence, characterImagePath, faceRight);
        TriggerDialogue(dialogue);
    }
    

    /// <summary>
    /// Start a dialogue with multiple lines
    /// </summary>
    /// <param name="speakers">Array of speaker names</param>
    /// <param name="sentences">Array of sentences</param>
    /// <param name="characterImagePaths">Optional array of character sprite paths</param>
    /// <param name="faceRightFlags">Optional array of orientation flags (true = face right, false = face left)</param>
    public void StartMultiLineDialogue(string[] speakers, string[] sentences, string[] characterImagePaths = null, bool[] faceRightFlags = null)
    {
        if (speakers.Length != sentences.Length)
        {
           //Debug.LogError("ProgrammaticDialogueTrigger: Speakers and sentences arrays must be the same length!");
            return;
        }
        
        if (characterImagePaths != null && characterImagePaths.Length != speakers.Length)
        {
           //Debug.LogError("ProgrammaticDialogueTrigger: Character images array must be the same length as speakers array!");
            return;
        }
        
        if (faceRightFlags != null && faceRightFlags.Length != speakers.Length)
        {
           //Debug.LogError("ProgrammaticDialogueTrigger: Face right flags array must be the same length as speakers array!");
            return;
        }
        
        Dialogue dialogue = CreateMultiLineDialogue(speakers, sentences, characterImagePaths, faceRightFlags);
        TriggerDialogue(dialogue);
    }
    

    /// <summary>
    /// Start a dialogue with choices
    /// </summary>
    /// <param name="speaker">Name of the speaker</param>
    /// <param name="sentence">What the speaker says</param>
    /// <param name="choiceTexts">Array of choice texts</param>
    /// <param name="choiceActions">Optional array of actions to execute when choices are selected</param>
    /// <param name="characterImagePath">Optional path to character sprite</param>
    /// <param name="faceRight">Optional orientation: true = face right, false = face left</param>
    public void StartDialogueWithChoices(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions = null, string characterImagePath = null, bool faceRight = true)
    {
        Dialogue dialogue = CreateDialogueWithChoices(speaker, sentence, choiceTexts, choiceActions, characterImagePath, faceRight);
        TriggerDialogue(dialogue);
    }
    

    /// <summary>
    /// Start a dialogue with a delay (useful for chaining dialogues)
    /// </summary>
    /// <param name="speaker">Name of the speaker</param>
    /// <param name="sentence">What the speaker says</param>
    /// <param name="delay">Delay in seconds before starting dialogue</param>
    /// <param name="characterImagePath">Optional path to character sprite</param>
    /// <param name="faceRight">Optional orientation: true = face right, false = face left</param>
    public void StartDelayedDialogue(string speaker, string sentence, float delay = 0.5f, string characterImagePath = null, bool faceRight = true)
    {
        StartCoroutine(DelayedDialogueCoroutine(speaker, sentence, delay, characterImagePath, faceRight));
    }
    

    /// <summary>
    /// Start a choice dialogue with a delay
    /// </summary>
    /// <param name="speaker">Name of the speaker</param>
    /// <param name="sentence">What the speaker says</param>
    /// <param name="choiceTexts">Array of choice texts</param>
    /// <param name="choiceActions">Optional array of actions to execute when choices are selected</param>
    /// <param name="delay">Delay in seconds before starting dialogue</param>
    /// <param name="characterImagePath">Optional path to character sprite</param>
    /// <param name="faceRight">Optional orientation: true = face right, false = face left</param>
    public void StartDelayedDialogueWithChoices(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions = null, float delay = 0.5f, string characterImagePath = null, bool faceRight = true)
    {
        StartCoroutine(DelayedChoiceDialogueCoroutine(speaker, sentence, choiceTexts, choiceActions, delay, characterImagePath, faceRight));
    }
    
    private System.Collections.IEnumerator DelayedDialogueCoroutine(string speaker, string sentence, float delay, string characterImagePath = null, bool faceRight = true)
    {
        yield return new WaitForSeconds(delay);
        StartSimpleDialogue(speaker, sentence, characterImagePath, faceRight);
    }
    
    private System.Collections.IEnumerator DelayedChoiceDialogueCoroutine(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions, float delay, string characterImagePath = null, bool faceRight = true)
    {
        yield return new WaitForSeconds(delay);
        StartDialogueWithChoices(speaker, sentence, choiceTexts, choiceActions, characterImagePath, faceRight);
    }
    

    /// <summary>
    /// Create a simple dialogue with one line
    /// </summary>
    /// <param name="speaker">Name of the speaker</param>
    /// <param name="sentence">What the speaker says</param>
    /// <param name="characterImagePath">Optional path to character sprite</param>
    /// <param name="faceRight">Optional orientation: true = face right, false = face left</param>
    private Dialogue CreateSimpleDialogue(string speaker, string sentence, string characterImagePath = null, bool faceRight = true)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.lines = new List<Dialogue.DialogueLine>();
        
        Dialogue.DialogueLine line = new Dialogue.DialogueLine();
        line.speaker = speaker;
        line.sentence = sentence;
        line.choices = new List<Dialogue.DialogueChoice>();
        line.faceRight = faceRight;
        
        // Load character image if path is provided
        if (!string.IsNullOrEmpty(characterImagePath))
        {
            line.characterImage = LoadCharacterSprite(characterImagePath);
        }
        
        dialogue.lines.Add(line);
        return dialogue;
    }
    

    /// <summary>
    /// Create a multi-line dialogue
    /// </summary>
    /// <param name="speakers">Array of speaker names</param>
    /// <param name="sentences">Array of sentences</param>
    /// <param name="characterImagePaths">Optional array of character sprite paths</param>
    /// <param name="faceRightFlags">Optional array of orientation flags</param>
    private Dialogue CreateMultiLineDialogue(string[] speakers, string[] sentences, string[] characterImagePaths = null, bool[] faceRightFlags = null)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.lines = new List<Dialogue.DialogueLine>();
        
        for (int i = 0; i < speakers.Length; i++)
        {
            Dialogue.DialogueLine line = new Dialogue.DialogueLine();
            line.speaker = speakers[i];
            line.sentence = sentences[i];
            line.choices = new List<Dialogue.DialogueChoice>();
            
            // Set face direction (default to right if not specified)
            line.faceRight = faceRightFlags != null && i < faceRightFlags.Length ? faceRightFlags[i] : true;
            
            // Load character image if path is provided
            if (characterImagePaths != null && i < characterImagePaths.Length && !string.IsNullOrEmpty(characterImagePaths[i]))
            {
                line.characterImage = LoadCharacterSprite(characterImagePaths[i]);
            }
            
            dialogue.lines.Add(line);
        }
        
        return dialogue;
    }
    

    /// <summary>
    /// Create a dialogue with choices
    /// </summary>
    /// <param name="speaker">Name of the speaker</param>
    /// <param name="sentence">What the speaker says</param>
    /// <param name="choiceTexts">Array of choice texts</param>
    /// <param name="choiceActions">Optional array of actions to execute when choices are selected</param>
    /// <param name="characterImagePath">Optional path to character sprite</param>
    /// <param name="faceRight">Optional orientation: true = face right, false = face left</param>
    private Dialogue CreateDialogueWithChoices(string speaker, string sentence, string[] choiceTexts, System.Action[] choiceActions, string characterImagePath = null, bool faceRight = true)
    {
        Dialogue dialogue = new Dialogue();
        dialogue.lines = new List<Dialogue.DialogueLine>();
        
        Dialogue.DialogueLine line = new Dialogue.DialogueLine();
        line.speaker = speaker;
        line.sentence = sentence;
        line.choices = new List<Dialogue.DialogueChoice>();
        line.faceRight = faceRight;
        
        // Load character image if path is provided
        if (!string.IsNullOrEmpty(characterImagePath))
        {
            line.characterImage = LoadCharacterSprite(characterImagePath);
        }
        
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
    

    /// <summary>
    /// Load character sprite from Resources folder
    /// </summary>
    /// <param name="spritePath">Path to the sprite (e.g., "Images/Characters/Guard")</param>
    /// <returns>Loaded sprite or null if not found</returns>
    private Sprite LoadCharacterSprite(string spritePath)
    {
        try
        {
            // Clean up the path for Resources.Load
            string resourcePath = spritePath;
            
            // Remove "Assets/" if present
            if (resourcePath.StartsWith("Assets/"))
            {
                resourcePath = resourcePath.Substring("Assets/".Length);
            }
            
            // Remove "Resources/" if present (to avoid double Resources path)
            if (resourcePath.StartsWith("Resources/"))
            {
                resourcePath = resourcePath.Substring("Resources/".Length);
            }
            
            // Remove file extension
            int lastDot = resourcePath.LastIndexOf('.');
            if (lastDot > 0)
            {
                resourcePath = resourcePath.Substring(0, lastDot);
            }
            
           //Debug.Log($"ProgrammaticDialogueTrigger: Attempting to load character sprite from Resources path: '{resourcePath}'");
            
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            
            if (sprite == null)
            {
               //Debug.LogWarning($"ProgrammaticDialogueTrigger: Could not load character sprite {resourcePath} from Resources. Make sure the image file is in a Resources folder.");
            }
            else
            {
               //Debug.Log($"ProgrammaticDialogueTrigger: Successfully loaded character sprite {sprite.name}");
            }
            
            return sprite;
        }
        catch (System.Exception e)
        {
           //Debug.LogError($"ProgrammaticDialogueTrigger: Error loading character sprite: {e.Message}");
            return null;
        }
    }

    /// Trigger the dialogue
    
    private void TriggerDialogue(Dialogue dialogue)
    {
        DialogueManager dialogueManager = Object.FindAnyObjectByType<DialogueManager>();
        
        if (dialogueManager == null)
        {
           //Debug.LogError("ProgrammaticDialogueTrigger: DialogueManager not found in the scene!");
            return;
        }
        
        if (dialogueManager.IsDialogueActive())
        {
           //Debug.LogWarning("ProgrammaticDialogueTrigger: Dialogue is already active!");
            return;
        }
        
        // Always try to find the player before getting Rigidbody
        FindAndAssignPlayer();
        
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
