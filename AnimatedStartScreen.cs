using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class AnimatedStartScreen : MonoBehaviour
{
    [Header("Main Title")]
    public Image titleImage;
    public float titleAnimationDuration = 0.3f;
    public AnimationCurve titleEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Menu Buttons")]
    public Button newGameButton;
    //public Button loadGameButton;
    public Button settingsButton;
    public Button creditsButton;
    public float buttonAnimationDuration = 0.15f;
    public float buttonStaggerDelay = 0.03f;
    public AnimationCurve buttonEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Background")]
    public Image backgroundImage;
    public float backgroundFadeDuration = 0.5f;
    
    [Header("Audio")]
    public AudioSource musicAudioSource; // Drag your AudioSource with Audio Mixer setup here
    public AudioClip buttonHoverSound;
    public AudioClip buttonClickSound;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    
    [Header("Animation Settings")]
    public bool playOnStart = true;
    
    private bool animationComplete = false;
    private Vector3[] originalButtonPositions;
    private Vector3 originalTitlePosition;
    
    private void Start()
    {
       //Debug.Log("AnimatedStartScreen: Start() called");
       //Debug.Log($"TitleImage assigned: {titleImage != null}");
       //Debug.Log($"PlayOnStart: {playOnStart}");
        
        InitializeComponents();
        StoreOriginalPositions();
        
        if (playOnStart)
        {
           //Debug.Log("AnimatedStartScreen: Auto-starting animation");
            StartCoroutine(PlayStartAnimation());
        }
        else
        {
           //Debug.Log("AnimatedStartScreen: Auto-start disabled");
        }
    }
    
    private void Update()
    {
        // Press SPACE to manually start animation for testing
        if (Input.GetKeyDown(KeyCode.Space))
        {
           //Debug.Log("AnimatedStartScreen: Manual start triggered with SPACE key");
            StartAnimation();
        }
    }
    
    private void InitializeComponents()
    {
        // Ensure there's an Audio Listener in the scene
        EnsureAudioListener();
        
        // Setup button hover sounds
        SetupButtonSounds();
        
        // Initially hide all elements
        SetInitialVisibility();
    }
    
    private void EnsureAudioListener()
    {
        // Check if there's already an Audio Listener in the scene
        AudioListener existingListener = FindFirstObjectByType<AudioListener>();
        
        if (existingListener == null)
        {
            // Find the main camera and add an Audio Listener to it
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // If no main camera, find any camera
                mainCamera = FindFirstObjectByType<Camera>();
            }
            
            if (mainCamera != null)
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
               //Debug.Log("AnimatedStartScreen: Added Audio Listener to main camera");
            }
            else
            {
                // If no camera found, add it to this GameObject as a fallback
                gameObject.AddComponent<AudioListener>();
               //Debug.Log("AnimatedStartScreen: Added Audio Listener to this GameObject (no camera found)");
            }
        }
        else
        {
           //Debug.Log("AnimatedStartScreen: Audio Listener already exists in scene");
        }
    }
    
    private void SetupButtonSounds()
    {
        Button[] buttons = { newGameButton, settingsButton, creditsButton };
        
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                // Add hover sound
                EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = button.gameObject.AddComponent<EventTrigger>();
                }
                
                EventTrigger.Entry hoverEntry = new EventTrigger.Entry();
                hoverEntry.eventID = EventTriggerType.PointerEnter;
                hoverEntry.callback.AddListener((data) => PlayHoverSound());
                trigger.triggers.Add(hoverEntry);
                
                // Add click sound
                button.onClick.AddListener(() => PlayClickSound());
            }
        }
    }
    
    private void StoreOriginalPositions()
    {
        // Store button positions
        Button[] buttons = { newGameButton, settingsButton, creditsButton };
        originalButtonPositions = new Vector3[buttons.Length];
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                RectTransform rectTransform = buttons[i].GetComponent<RectTransform>();
                originalButtonPositions[i] = rectTransform.anchoredPosition;
            }
        }
        
        // Store title position
        if (titleImage != null)
        {
            RectTransform rectTransform = titleImage.GetComponent<RectTransform>();
            originalTitlePosition = rectTransform.anchoredPosition;
        }
            
       //Debug.Log("AnimatedStartScreen: Stored original positions");
    }
    
    private void SetInitialVisibility()
    {
       //Debug.Log("AnimatedStartScreen: Setting initial visibility");
        
        // Hide title - don't change scale, just make transparent
        if (titleImage != null)
        {
            titleImage.color = Color.clear;
            // Hide all child images too
            Image[] childImages = titleImage.GetComponentsInChildren<Image>();
            foreach (Image childImage in childImages)
            {
                if (childImage != titleImage) // Don't hide the parent again
                    childImage.color = Color.clear;
            }
           //Debug.Log("AnimatedStartScreen: Set title transparent (no scale change)");
        }
        
        // Hide buttons
        Button[] buttons = { newGameButton, settingsButton, creditsButton };
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                buttons[i].gameObject.SetActive(false);
               //Debug.Log($"AnimatedStartScreen: Hid button {i}");
            }
        }
        
        // Set background to transparent if it exists
        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0);
            
            // Set all child images to transparent too
            Image[] childImages = backgroundImage.GetComponentsInChildren<Image>();
            foreach (Image childImage in childImages)
            {
                if (childImage != backgroundImage) // Don't set the parent again
                    childImage.color = new Color(childImage.color.r, childImage.color.g, childImage.color.b, 0);
            }
            
           //Debug.Log("AnimatedStartScreen: Set background and child images initial state");
        }
        
       //Debug.Log("AnimatedStartScreen: Initial visibility setup complete");
    }
    
    public IEnumerator PlayStartAnimation()
    {
       //Debug.Log("AnimatedStartScreen: Starting animation sequence");
        
        // Start background music - just tell the AudioSource to play
        if (musicAudioSource != null)
        {
            musicAudioSource.Play();
           //Debug.Log("AnimatedStartScreen: Started music via external AudioSource");
        }
        else
        {
           //Debug.LogWarning("AnimatedStartScreen: No music AudioSource assigned");
        }
        
        // Fade in background
        if (backgroundImage != null)
        {
           //Debug.Log("AnimatedStartScreen: Fading in background");
            StartCoroutine(FadeInBackground()); // Start background fade without waiting
        }
        
        // Start title animation immediately (don't wait for background)
       //Debug.Log("AnimatedStartScreen: Starting title animation");
        yield return StartCoroutine(AnimateTitle());
        
        // Animate buttons immediately after title
       //Debug.Log("AnimatedStartScreen: Starting button animations");
        yield return StartCoroutine(AnimateButtons());
        
        animationComplete = true;
       //Debug.Log("AnimatedStartScreen: Animation sequence complete");
        
        // Remove looping postcard animation - postcards stay visible after title
    }
    
    private IEnumerator FadeInBackground()
    {
        float elapsedTime = 0f;
        Color startColor = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0);
        Color endColor = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 1);
        
        // Get all child images in the background
        Image[] childImages = backgroundImage.GetComponentsInChildren<Image>();
        Color[] childStartColors = new Color[childImages.Length];
        Color[] childEndColors = new Color[childImages.Length];
        
        // Store original colors for all child images
        for (int i = 0; i < childImages.Length; i++)
        {
            childStartColors[i] = new Color(childImages[i].color.r, childImages[i].color.g, childImages[i].color.b, 0);
            childEndColors[i] = new Color(childImages[i].color.r, childImages[i].color.g, childImages[i].color.b, 1);
        }
        
        while (elapsedTime < backgroundFadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / backgroundFadeDuration;
            
            // Fade main background image
            backgroundImage.color = Color.Lerp(startColor, endColor, progress);
            
            // Fade all child images
            for (int i = 0; i < childImages.Length; i++)
            {
                if (childImages[i] != backgroundImage) // Don't fade the parent twice
                {
                    childImages[i].color = Color.Lerp(childStartColors[i], childEndColors[i], progress);
                }
            }
            
            yield return null;
        }
        
        // Set final colors
        backgroundImage.color = endColor;
        for (int i = 0; i < childImages.Length; i++)
        {
            if (childImages[i] != backgroundImage)
            {
                childImages[i].color = childEndColors[i];
            }
        }
    }
    
    private IEnumerator AnimateTitle()
    {
        float elapsedTime = 0f;
        Color startColor = Color.clear;
        Color endColor = Color.white;
        
        // Get child images
        Image[] childImages = titleImage.GetComponentsInChildren<Image>();
        
        // Don't change scale - just fade in
        while (elapsedTime < titleAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = titleEaseCurve.Evaluate(elapsedTime / titleAnimationDuration);
            
            Color currentColor = Color.Lerp(startColor, endColor, progress);
            titleImage.color = currentColor;
            
            // Fade in child images too
            foreach (Image childImage in childImages)
            {
                if (childImage != titleImage)
                    childImage.color = currentColor;
            }
            
            yield return null;
        }
        
        titleImage.color = endColor;
        foreach (Image childImage in childImages)
        {
            if (childImage != titleImage)
                childImage.color = endColor;
        }
    }
    
    private IEnumerator AnimateButtons()
    {
        Button[] buttons = { newGameButton, settingsButton, creditsButton };
        
        // Start all button animations simultaneously with stagger
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                StartCoroutine(AnimateButton(buttons[i], i, i * buttonStaggerDelay));
            }
        }
        
        // Wait for all animations to complete (last button starts + animation duration)
        float totalTime = (buttons.Length - 1) * buttonStaggerDelay + buttonAnimationDuration;
        yield return new WaitForSeconds(totalTime);
    }
    
    private IEnumerator AnimateButton(Button button, int index, float delay)
    {
        // Wait for stagger delay
        if (delay > 0)
            yield return new WaitForSeconds(delay);
            
        button.gameObject.SetActive(true);
        
        RectTransform rectTransform = button.GetComponent<RectTransform>();
        Image buttonImage = button.GetComponent<Image>();
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        
        // Cache original values
        Vector2 endPosition = new Vector2(originalButtonPositions[index].x, originalButtonPositions[index].y);
        Vector2 startPosition = new Vector2(endPosition.x, endPosition.y - 50f); // Reduced offset for smoother animation
        
        Color originalImageColor = buttonImage != null ? buttonImage.color : Color.white;
        Color originalTextColor = buttonText != null ? buttonText.color : Color.white;
        
        // Set initial state
        rectTransform.anchoredPosition = startPosition;
        if (buttonImage != null)
            buttonImage.color = new Color(originalImageColor.r, originalImageColor.g, originalImageColor.b, 0);
        if (buttonText != null)
            buttonText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, 0);
        
        float elapsedTime = 0f;
        while (elapsedTime < buttonAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = buttonEaseCurve.Evaluate(elapsedTime / buttonAnimationDuration);
            
            // Animate position
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, progress);
            
            // Animate alpha
            if (buttonImage != null)
                buttonImage.color = new Color(originalImageColor.r, originalImageColor.g, originalImageColor.b, progress);
            if (buttonText != null)
                buttonText.color = new Color(originalTextColor.r, originalTextColor.g, originalTextColor.b, progress);
            
            yield return null;
        }
        
        // Ensure final state
        rectTransform.anchoredPosition = endPosition;
        if (buttonImage != null)
            buttonImage.color = originalImageColor;
        if (buttonText != null)
            buttonText.color = originalTextColor;
    }
    
    private void PlayHoverSound()
    {
        if (buttonHoverSound != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(buttonHoverSound, transform, sfxVolume);
        }
    }
    
    private void PlayClickSound()
    {
        if (buttonClickSound != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(buttonClickSound, transform, sfxVolume);
        }
    }
    
    // Public methods for button functionality
    public void StartNewGame()
    {
       //Debug.Log("Starting new game...");
        // Add your new game logic here
    }
    
    public void LoadGame()
    {
       //Debug.Log("Loading game...");
        // Add your load game logic here
    }
    
    public void OpenSettings()
    {
       //Debug.Log("Opening settings...");
        // Add your settings logic here
    }
    
    public void ShowCredits()
    {
       //Debug.Log("Showing credits...");
        // Add your credits logic here
    }
    
    // Public method to manually start animation
    public void StartAnimation()
    {
        if (!animationComplete)
        {
            StopAllCoroutines();
            StartCoroutine(PlayStartAnimation());
        }
    }
}
