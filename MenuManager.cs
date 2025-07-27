using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject PauseMenu;
    public GameObject SoundMenu;
    public GameObject ExitMenu;
    public GameObject VideoMenu;
    //public GameObject LoadGameMenu;
    //public GameObject SaveGameMenu;
    public GameObject MainMenu;
    public bool isSubMenuOpen = false;
    public TMP_Dropdown resolutionDropdown; // Reference to the quality dropdown
    Resolution[] resolutions;

    //private bool isGameStarted = false;

    // void Start()
    // {
    //     // Show the Main Menu when the game starts
    //     Cursor.lockState = CursorLockMode.None; // Unlock the cursor
    //     Cursor.visible = true; // Show the cursor
    //     Time.timeScale = 0f; // Pause the game
    //     MainMenu.SetActive(true); // Show the Main Menu
    //     PauseMenu.SetActive(false); // Ensure Pause Menu is hidden
    //     SoundMenu.SetActive(false); // Ensure Sound Menu is hidden
    // }

    void Start()
    {
        // Ensure EventSystem exists for UI interactions
        EnsureEventSystemExists();
        
        // Ensure Canvas is properly configured
        EnsureCanvasConfiguration();
        
        // Define the most common resolutions
        resolutions = new Resolution[]
        {
            new Resolution { width = 1920, height = 1080 },  // 1080p
            new Resolution { width = 2560, height = 1440 },  // 2K
            new Resolution { width = 3840, height = 2160 }   // 4K
        };

        resolutionDropdown.ClearOptions(); // Clear existing options
        List<string> options = new List<string>(); // Create a list for resolution options

        int currentResolutionIndex = 0; // Initialize the current resolution index

        for (int i = 0; i < resolutions.Length; i++)
        {
            // Add each resolution to the options list
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);

            // Check if this resolution matches the current screen resolution
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i; // Store the index of the current resolution
            }
        }

        resolutionDropdown.AddOptions(options); // Add options to the dropdown
        resolutionDropdown.value = currentResolutionIndex; // Set the current resolution index
        resolutionDropdown.RefreshShownValue(); // Refresh the dropdown to show the current value
        
        Debug.Log("MenuManager: Initialized successfully in scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    void Update()
    {
        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If a submenu is open, close it and return to the pause menu
            if (isSubMenuOpen)
            {
                if (SoundMenu.activeSelf)
                {
                    CloseSoundMenu(); // Close the sound menu
                }
                else if (VideoMenu.activeSelf)
                {
                    CloseVideoMenu(); // Close the video menu
                }
                else if (ExitMenu.activeSelf)
                {
                    CloseExitConfirmationModal(); // Close the exit confirmation modal
                }
            }
            else
            {
                // If no submenu is open, toggle the pause menu
                OpenPauseMenu();
            }
        }
    }

    void OpenPauseMenu()
    {
        // Prevent toggling the pause menu if a sub-menu is open
        if (isSubMenuOpen) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If the modal is open, close both the modal and the pause menu
            if (ExitMenu.activeSelf)
            {
                ExitMenu.SetActive(false); // Hide the modal
                PauseMenu.SetActive(false); // Hide the pause menu
                ResumeGame();
                return;
            }

            if (Time.timeScale == 1f)
            {
                PauseGame();
            }
            else
            {
                ResumeGame();
            }
        }
    }

    // Centralized method to pause the game
    private void PauseGame()
    {
        Time.timeScale = 0f; // Pause the game
        SetCursorState(true, CursorLockMode.None); // Show and unlock cursor
        PauseMenu.SetActive(true); // Show the pause menu
        
        // Disable player controls
        DisablePlayerControls();
        
        // Ensure UI interactions work properly when paused
        Canvas.ForceUpdateCanvases();
        
        Debug.Log("MenuManager: Game paused");
    }

    // Centralized method to resume the game
    private void ResumeGame()
    {
        Time.timeScale = 1f; // Resume the game
        SetCursorState(false, CursorLockMode.Locked); // Hide and lock cursor
        PauseMenu.SetActive(false); // Hide the pause menu
        
        // Re-enable player controls
        EnablePlayerControls();
        
        Debug.Log("MenuManager: Game resumed");
    }

    // Centralized cursor state management
    private void SetCursorState(bool visible, CursorLockMode lockMode)
    {
        Cursor.visible = visible;
        Cursor.lockState = lockMode;
        Debug.Log($"MenuManager: Cursor visibility: {visible}, Lock mode: {lockMode}");
    }

    // Disable all player controls when paused
    private void DisablePlayerControls()
    {
        // Disable PlayerController
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("MenuManager: PlayerController disabled");
        }

        // Disable UnderwaterPlayerController
        UnderwaterPlayerController underwaterController = FindAnyObjectByType<UnderwaterPlayerController>();
        if (underwaterController != null)
        {
            underwaterController.enabled = false;
            Debug.Log("MenuManager: UnderwaterPlayerController disabled");
        }

        // Disable weapon scripts
        GunScript gunScript = FindAnyObjectByType<GunScript>();
        if (gunScript != null)
        {
            gunScript.enabled = false;
            Debug.Log("MenuManager: GunScript disabled");
        }

        RifleScript rifleScript = FindAnyObjectByType<RifleScript>();
        if (rifleScript != null)
        {
            rifleScript.enabled = false;
            Debug.Log("MenuManager: RifleScript disabled");
        }

        // Disable WeaponManager
        WeaponManager weaponManager = FindAnyObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.enabled = false;
            Debug.Log("MenuManager: WeaponManager disabled");
        }

        // Disable all building enter triggers
        BuildingEnterTrigger[] buildingTriggers = FindObjectsByType<BuildingEnterTrigger>(FindObjectsSortMode.None);
        foreach (BuildingEnterTrigger trigger in buildingTriggers)
        {
            trigger.enabled = false;
        }
        if (buildingTriggers.Length > 0)
        {
            Debug.Log($"MenuManager: {buildingTriggers.Length} BuildingEnterTriggers disabled");
        }
    }

    // Re-enable all player controls when resumed
    private void EnablePlayerControls()
    {
        // Enable PlayerController
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("MenuManager: PlayerController enabled");
        }

        // Enable UnderwaterPlayerController
        UnderwaterPlayerController underwaterController = FindAnyObjectByType<UnderwaterPlayerController>();
        if (underwaterController != null)
        {
            underwaterController.enabled = true;
            Debug.Log("MenuManager: UnderwaterPlayerController enabled");
        }

        // Enable weapon scripts
        GunScript gunScript = FindAnyObjectByType<GunScript>();
        if (gunScript != null)
        {
            gunScript.enabled = true;
            Debug.Log("MenuManager: GunScript enabled");
        }

        RifleScript rifleScript = FindAnyObjectByType<RifleScript>();
        if (rifleScript != null)
        {
            rifleScript.enabled = true;
            Debug.Log("MenuManager: RifleScript enabled");
        }

        // Enable WeaponManager
        WeaponManager weaponManager = FindAnyObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.enabled = true;
            Debug.Log("MenuManager: WeaponManager enabled");
        }

        // Enable all building enter triggers
        BuildingEnterTrigger[] buildingTriggers = FindObjectsByType<BuildingEnterTrigger>(FindObjectsSortMode.None);
        foreach (BuildingEnterTrigger trigger in buildingTriggers)
        {
            trigger.enabled = true;
        }
        if (buildingTriggers.Length > 0)
        {
            Debug.Log($"MenuManager: {buildingTriggers.Length} BuildingEnterTriggers enabled");
        }
    }
    public void ClosePauseMenu()
    {
        if (PauseMenu.activeSelf && !isSubMenuOpen)
        {
            // If the pause menu is open and no sub-menu is open, close the pause menu
            ResumeGame();
        }
        else if (!PauseMenu.activeSelf && isSubMenuOpen)
        {
            PauseMenu.SetActive(false); // Show the pause menu
            SoundMenu.SetActive(false); // Hide the sound menu
            VideoMenu.SetActive(false); // Hide the video menu
            isSubMenuOpen = false; // Mark that no sub-menu is open
            ResumeGame();
        }
    }

    public void StartGame()
    {
        // Called when the player starts the game from the Main Menu
        MainMenu.SetActive(false); // Hide the Main Menu
        ResumeGame();
        //isGameStarted = true; // Mark the game as started
    }
    public void OpenSoundMenu()
    {
        SoundMenu.SetActive(true); // Show the sound menu
        PauseMenu.SetActive(false); // Hide the pause menu
        isSubMenuOpen = true; // Mark that a sub-menu is open
    }
    public void CloseSoundMenu()
    {
        SoundMenu.SetActive(false); // Hide the sound menu
        PauseMenu.SetActive(true); // Show the pause menu
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    public void OpenVideoMenu()
    {
        VideoMenu.SetActive(true); // Show the video menu
        PauseMenu.SetActive(false); // Hide the pause menu
        isSubMenuOpen = true; // Mark that a sub-menu is open
    }
    public void CloseVideoMenu()
    {
        VideoMenu.SetActive(false); // Hide the video menu
        PauseMenu.SetActive(true); // Show the pause menu
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    public void OpenExitConfirmationModal()
    {
        ExitMenu.SetActive(true); // Show the exit confirmation modal
    }
    public void CloseExitConfirmationModal()
    {
        ExitMenu.SetActive(false); // Hide the exit confirmation modal
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    public void QuitGame()
    {
        // Quit the application
        Debug.Log("Quitting the game...");
        Application.Quit();
    }

    public void setQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true); // Force Unity to apply expensive changes
        Debug.Log($"Quality level set to: {QualitySettings.names[qualityIndex]}");
    }
    public void setResolution(int resolutionIndex)
    {
        // Set the screen resolution based on the selected index
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        Debug.Log($"Resolution set to: {resolution.width} x {resolution.height}");
    }

    public void setScreenMode(bool isFullScreen)
    {
        // Set the screen mode (fullscreen or windowed)
        Screen.fullScreen = isFullScreen;
        Debug.Log($"Screen mode set to: {(isFullScreen ? "Fullscreen" : "Windowed")}");
    }

    public void setScreenMode(int modeIndex)
    {
        // Map the dropdown index to FullScreenMode
        FullScreenMode screenMode = FullScreenMode.ExclusiveFullScreen; // Default to Fullscreen
        switch (modeIndex)
        {
            case 0:
                screenMode = FullScreenMode.ExclusiveFullScreen; // Fullscreen
                break;
            case 1:
                screenMode = FullScreenMode.FullScreenWindow; // Borderless
                break;
        }

        // Apply the selected screen mode
        Screen.fullScreenMode = screenMode;
        Debug.Log($"Screen mode set to: {screenMode}");
    }

    // Helper method to ensure EventSystem exists for UI interactions
    private void EnsureEventSystemExists()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogWarning("MenuManager: No EventSystem found in scene! Creating one...");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("MenuManager: EventSystem created successfully");
        }
        else
        {
            Debug.Log("MenuManager: EventSystem found and active");
        }
    }

    // Helper method to ensure Canvas is properly configured for UI interactions
    private void EnsureCanvasConfiguration()
    {
        // Find all canvases related to our menu system
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in canvases)
        {
            // Check if this canvas contains any of our menu objects
            if (IsMenuCanvas(canvas))
            {
                Debug.Log($"MenuManager: Configuring canvas: {canvas.name}");
                
                // Ensure Canvas has proper settings for UI interaction
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // Ensure GraphicRaycaster exists for button clicks
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster == null)
                {
                    raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
                    Debug.Log($"MenuManager: Added GraphicRaycaster to {canvas.name}");
                }
                
                // Ensure CanvasScaler exists for proper scaling
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null)
                {
                    scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                    scaler.matchWidthOrHeight = 0.5f;
                    Debug.Log($"MenuManager: Added CanvasScaler to {canvas.name}");
                }
                
                // Make sure the canvas is set to use unscaled time for UI interactions
                // This is crucial when Time.timeScale is set to 0
                Canvas.ForceUpdateCanvases();
            }
        }
    }

    // Helper method to check if a canvas contains our menu objects
    private bool IsMenuCanvas(Canvas canvas)
    {
        Transform canvasTransform = canvas.transform;
        
        // Check if any of our menu GameObjects are children of this canvas
        return (PauseMenu != null && PauseMenu.transform.IsChildOf(canvasTransform)) ||
               (SoundMenu != null && SoundMenu.transform.IsChildOf(canvasTransform)) ||
               (VideoMenu != null && VideoMenu.transform.IsChildOf(canvasTransform)) ||
               (ExitMenu != null && ExitMenu.transform.IsChildOf(canvasTransform)) ||
               (MainMenu != null && MainMenu.transform.IsChildOf(canvasTransform));
    }

    // Method to fix UI interaction issues that might occur in different scenes
    public void RefreshUIComponents()
    {
        Debug.Log("MenuManager: Refreshing UI components...");
        
        // Check for overlay blocking issues
        CheckForOverlayBlocking();
        
        EnsureEventSystemExists();
        EnsureCanvasConfiguration();
        
        // Force update all buttons to ensure they're properly configured
        Button[] allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);
        foreach (Button button in allButtons)
        {
            if (IsButtonInMenuSystem(button))
            {
                // Ensure button is interactable
                button.interactable = true;
                
                // Make sure button has proper raycast target settings
                Image buttonImage = button.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.raycastTarget = true;
                }
            }
        }
        
        Debug.Log($"MenuManager: Refreshed {allButtons.Length} buttons in scene");
    }

    // Helper method to check for and fix overlay blocking issues
    private void CheckForOverlayBlocking()
    {
        // Check if SceneTransitionOverlay is blocking UI
        if (SceneTransitionOverlay.Instance != null && SceneTransitionOverlay.Instance.IsBlockingUI())
        {
            Debug.LogWarning("MenuManager: SceneTransitionOverlay is blocking UI! Force hiding...");
            SceneTransitionOverlay.Instance.ForceHideOverlay();
        }
        
        // Check for any high-priority canvases that might be blocking UI
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas.sortingOrder >= 1000 && canvas.gameObject.activeInHierarchy)
            {
                GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
                if (raycaster != null && raycaster.enabled)
                {
                    // Check if this is our overlay canvas
                    if (canvas.name.Contains("OverlayCanvas") || canvas.name.Contains("Transition"))
                    {
                        Debug.LogWarning($"MenuManager: Found blocking overlay canvas: {canvas.name}, disabling raycaster");
                        raycaster.enabled = false;
                    }
                }
            }
        }
    }

    // Helper method to check if a button belongs to our menu system
    private bool IsButtonInMenuSystem(Button button)
    {
        Transform buttonTransform = button.transform;
        
        return (PauseMenu != null && buttonTransform.IsChildOf(PauseMenu.transform)) ||
               (SoundMenu != null && buttonTransform.IsChildOf(SoundMenu.transform)) ||
               (VideoMenu != null && buttonTransform.IsChildOf(VideoMenu.transform)) ||
               (ExitMenu != null && buttonTransform.IsChildOf(ExitMenu.transform)) ||
               (MainMenu != null && buttonTransform.IsChildOf(MainMenu.transform));
    }

    // Call this method when returning from scene transitions
    private void OnEnable()
    {
        Debug.Log("MenuManager: OnEnable called - refreshing UI components");
        // Delay the refresh slightly to ensure scene is fully loaded
        Invoke(nameof(RefreshUIComponents), 0.1f);
    }
}
