using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public List<Dialogue.DialogueLine> lines; // List of dialogue lines
    public GameObject DialogCanvas;
    public bool isDialogueActive = false; // Track if dialogue is active
    public Image characterImageUI; // UI element to display the character image
    public RectTransform characterImageContainer; // Container for flipping orientation
    public Image arrowImage; // UI element to display the arrow
    private Coroutine arrowFlashCoroutine;
    public event System.Action OnDialogueEnd;
    public AudioClip nextSentenceSound;
    public AudioClip choiceSound; // Sound effect for choices
    public GameObject choicesContainer; // Container for choice buttons
    public GameObject choiceButtonPrefab; // Prefab for a choice button
    public GameObject choiceMarkerPrefab; // Prefab for the choice marker
    private int selectedChoiceIndex = 0; // Tracks the currently selected choice
    private List<Button> choiceButtons = new List<Button>(); // Stores the instantiated choice buttons
    private int currentLineIndex = 0; // Tracks the current line in the dialogue
    public RectTransform choiceMarker; // Reference to the marker (e.g., arrow)
    private Rigidbody playerRigidbody; // Reference to the player's Rigidbody
    private bool isDisplayingChoices = false; // Tracks whether choices are currently being displayed
    public Animator playerAnimator;
    private Animator currentNpcAnimator; // Reference to the current NPC's animator
    private bool justStartedDialogue = false;

    void Start()
    {
        lines = new List<Dialogue.DialogueLine>();
        DialogCanvas.SetActive(false); // Ensure the canvas is hidden at the start
    }

    void Update()
    {
        //Debug.Log($"isDialogueActive: {isDialogueActive}, choiceButtons.Count: {choiceButtons.Count}");

        if (isDialogueActive)
        {
            if (choiceButtons.Count > 0)
            {
                HandleChoiceNavigation();
                return;
            }

            if (justStartedDialogue)
            {
                justStartedDialogue = false;
                return; // Skip input this frame
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (nextSentenceSound != null)
                {
                    SFXManager.instance.PlaySFXClip(nextSentenceSound, transform, 1f);
                }
                DisplayNextSentence();
            }
        }
    }

    public void StartDialogue(Dialogue dialogue, Rigidbody playerRigidbody, Animator npcAnimator = null)
    {
        lines = new List<Dialogue.DialogueLine>(dialogue.lines);
        currentLineIndex = 0;
        selectedChoiceIndex = 0;
        isDialogueActive = true;
        this.playerRigidbody = playerRigidbody;
        this.currentNpcAnimator = npcAnimator; // Store the current NPC's animator

        // Lock the player's position
        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation; // Freeze all position axes
        }

        // Disable player movement
        PlayerController playerController = playerRigidbody.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        // Start the dialogue
        justStartedDialogue = true;
        DisplayNextSentence();
    }

    public void DisplayNextSentence()
    {
        if (currentLineIndex >= lines.Count)
        {
            EndDialogue();
            return;
        }

        Dialogue.DialogueLine line = lines[currentLineIndex];
        currentLineIndex++; // Move to the next line

        // Set the speaker's name
        nameText.text = line.speaker;

        // Set the character image
        if (line.characterImage != null)
        {
            characterImageUI.sprite = line.characterImage;
            characterImageUI.SetNativeSize(); // Preserve the original size of the image
            characterImageUI.gameObject.SetActive(true);
        }
        else
        {
            characterImageUI.gameObject.SetActive(false); // Hide if no image is provided
        }

        // Flip the character image based on orientation
        Vector3 scale = characterImageContainer.localScale;
        scale.x = line.faceRight ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x); // Flip if facing right
        characterImageContainer.localScale = scale;

        // Move the character image to the other side of the screen if facing right
        Vector2 anchoredPosition = characterImageContainer.anchoredPosition;
        anchoredPosition.x = line.faceRight ? -440 : 440; // Adjust the X position (e.g., -440 for right, 440 for left)
        characterImageContainer.anchoredPosition = anchoredPosition;

        // Display the sentence
        StopAllCoroutines();
        StartCoroutine(TypeSentence(line.sentence));

        // Trigger animation if specified
        if (!string.IsNullOrEmpty(line.animationTrigger))
        {
            Animator targetAnimator = null;
            if (line.speaker == "Player" && playerAnimator != null)
                targetAnimator = playerAnimator;
            else if (currentNpcAnimator != null)
                targetAnimator = currentNpcAnimator;

            if (targetAnimator != null)
            {
                if (line.animationParameterType == "bool")
                    targetAnimator.SetBool(line.animationTrigger, true);
                else
                    targetAnimator.SetTrigger(line.animationTrigger);
            }
        }

        // Check for choices
        if (line.choices != null && line.choices.Count > 0)
        {
            SetArrowVisibility(false); // Hide the arrow when choices are displayed
            DisplayChoices(line.choices);
        }
        else
        {
            SetArrowVisibility(true); // Show the arrow if no choices are present
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        dialogueText.text = "";
        arrowImage.gameObject.SetActive(false); // Hide the arrow while typing

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter;
            yield return null; // Wait for the next frame to create a typing effect
        }

        // Only enable the arrow if there are no choices being displayed
        if (!isDisplayingChoices)
        {
            SetArrowVisibility(true);
        }
    }
 
    private IEnumerator FlashArrow()
    {
        while (true)
        {
            arrowImage.gameObject.SetActive(!arrowImage.gameObject.activeSelf); // Toggle visibility
            yield return new WaitForSeconds(0.5f); // Adjust the delay for flashing speed
        }
    }

    private void SetArrowVisibility(bool isVisible)
    {
        Debug.Log($"SetArrowVisibility called. isVisible: {isVisible}");

        if (arrowFlashCoroutine != null)
        {
            StopCoroutine(arrowFlashCoroutine);
            arrowFlashCoroutine = null;
        }

        arrowImage.gameObject.SetActive(isVisible);

        if (isVisible)
        {
            arrowFlashCoroutine = StartCoroutine(FlashArrow());
        }
    }

    public void EndDialogue()
    {
        DialogCanvas.SetActive(false); // Deactivate the canvas
        isDialogueActive = false; // Set dialogue as inactive

        // Stop flashing the arrow
        SetArrowVisibility(false);

        // Clear dynamically created choice buttons
        foreach (Transform child in choicesContainer.transform)
        {
            if (child.gameObject != choiceMarker.gameObject && child.gameObject != choiceButtonPrefab)
            {
                Destroy(child.gameObject);
            }
        }
        choiceButtons.Clear(); // Clear the list to remove references to destroyed objects

        // Hide the marker when dialogue ends
        if (choiceMarker != null)
        {
            choiceMarker.gameObject.SetActive(false); // Disable the marker
        }

        // Release the player's position
        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Remove all constraints

            // Enable player movement
            PlayerController playerController = playerRigidbody.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetMovementEnabled(true);
            }

            playerRigidbody = null; // Clear the reference
        }

        // Notify listeners that the dialogue has ended
        OnDialogueEnd?.Invoke();
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    private void DisplayChoices(List<Dialogue.DialogueChoice> choices)
    {
        Debug.Log($"Displaying {choices.Count} choices.");

        // Set the flag to indicate that choices are being displayed
        isDisplayingChoices = true;

        // Disable the arrow when choices are displayed
        SetArrowVisibility(false);

        // Clear existing dynamically created choices and reset tracking
        foreach (Transform child in choicesContainer.transform)
        {
            Destroy(child.gameObject); // Only destroy dynamically created objects
        }
        choiceButtons.Clear(); // Clear the list to remove references to destroyed objects
        selectedChoiceIndex = 0;

        // Create a button for each choice
        foreach (var choice in choices)
        {
            GameObject choiceButton = Instantiate(choiceButtonPrefab, choicesContainer.transform);
            choiceButton.SetActive(true);

            TextMeshProUGUI buttonText = choiceButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.choiceText; // Set the button text
            }
            else
            {
                Debug.LogError("Choice button prefab is missing a TextMeshProUGUI component!");
            }

            Button button = choiceButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    HandleChoiceSelection(choice);
                });
            }
            else
            {
                Debug.LogError("Choice button prefab is missing a Button component!");
            }

            choiceButtons.Add(button);
        }

        // Instantiate the marker and attach it to the first button
        if (choiceButtons.Count > 0)
        {
            if (choiceMarker == null)
            {
                // Instantiate the marker from the prefab
                GameObject markerInstance = Instantiate(choiceMarkerPrefab);
                choiceMarker = markerInstance.GetComponent<RectTransform>();
            }

            // Set the marker as a child of the first button
            choiceMarker.SetParent(choiceButtons[0].transform, false); // Make it a child of the first button
            choiceMarker.anchoredPosition = new Vector2(0, 0); // Adjust position relative to the button
            choiceMarker.gameObject.SetActive(true);

            HighlightButton(choiceButtons[0]); // Highlight the first button by default
        }

        choicesContainer.SetActive(true);
    }

    private void HandleChoiceSelection(Dialogue.DialogueChoice choice)
    {
        choicesContainer.SetActive(false);

        choice.onChoiceSelected?.Invoke();

        if (choice.nextLineIndex >= 0 && choice.nextLineIndex < lines.Count)
        {
            currentLineIndex = choice.nextLineIndex;
            DisplayNextSentence();
        }
        else
        {
            EndDialogue();
        }

        choiceButtons.Clear();

        // Reset the flag to indicate that choices are no longer being displayed
        isDisplayingChoices = false;

        // Re-enable the arrow for the next sentence
        SetArrowVisibility(true);
    }

    private void HandleChoiceNavigation()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            selectedChoiceIndex = (selectedChoiceIndex - 1 + choiceButtons.Count) % choiceButtons.Count;
            if (choiceButtons[selectedChoiceIndex] != null)
            {
                HighlightButton(choiceButtons[selectedChoiceIndex]);
            }
        }

        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            selectedChoiceIndex = (selectedChoiceIndex + 1) % choiceButtons.Count;
            if (choiceButtons[selectedChoiceIndex] != null)
            {
                HighlightButton(choiceButtons[selectedChoiceIndex]);
            }
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return))
        {
            if (choiceButtons[selectedChoiceIndex] != null)
            {
                if (choiceSound != null)
                {
                    SFXManager.instance.PlaySFXClip(choiceSound, transform, 1f); // Play choice sound
                }
                choiceButtons[selectedChoiceIndex].onClick.Invoke();
            }
        }
    }

    private void HighlightButton(Button button)
    {
        foreach (var btn in choiceButtons)
        {
            // Reset all buttons to their default state
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white; // Default color
            btn.colors = colors;
        }

        // Highlight the selected button
        ColorBlock selectedColors = button.colors;
        selectedColors.normalColor = Color.yellow; // Highlight color
        button.colors = selectedColors;

        // Move the marker to the selected button and make it a child
        if (choiceMarker != null)
        {
            // Re-parent the marker to the new button
            choiceMarker.SetParent(button.transform, false);

            // Get the button's RectTransform
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            RectTransform markerRect = choiceMarker.GetComponent<RectTransform>();

            // Keep the default x-location of the button
            float defaultX = 0; // Default x-position relative to the button
            float middleY = -buttonRect.rect.height / 2; // Calculate the vertical center of the button
            float markerOffset = markerRect.rect.width / 2; // Offset by half the width of the marker

            // Set the marker's position relative to the button
            choiceMarker.anchoredPosition = new Vector2(defaultX - markerOffset, middleY);
            Debug.Log($"Marker aligned to middle left edge of button: {choiceMarker.anchoredPosition}");
        }

        button.Select();
    }
}
