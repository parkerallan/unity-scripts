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
    public GameObject CreditsMenu;
    public GameObject SettingsMenu;
    //public GameObject LoadGameMenu;
    //public GameObject SaveGameMenu;
    public GameObject MainMenu;
    public bool isSubMenuOpen = false;
    public TMP_Dropdown resolutionDropdown; // Reference to the quality dropdown
    Resolution[] resolutions;

    [Header("Scene Transition")]
    public ProgrammaticBuildingEntry buildingEntry; // Reference to the building entry system

    [Header("Save/Load System")]
    public SaveManager saveManager; // Reference to the save manager

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
        
        // Update button states based on save file availability
        UpdateMenuButtonStates();
        
        // Ensure SceneTransitionOverlay instance exists
        EnsureSceneTransitionOverlay();
        
        Debug.Log("MenuManager: Initialized successfully in scene: " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// Ensure SceneTransitionOverlay instance exists for scene transitions
    /// </summary>
    private void EnsureSceneTransitionOverlay()
    {
        try
        {
            Debug.Log("MenuManager: Creating/Finding SceneTransitionOverlay instance...");
            
            // Force access to Instance property to create it if needed
            var overlay = SceneTransitionOverlay.Instance;
            if (overlay != null)
            {
                Debug.Log($"MenuManager: SceneTransitionOverlay instance confirmed - GameObject: {overlay.gameObject.name}");
            }
            else
            {
                Debug.LogError("MenuManager: SceneTransitionOverlay instance is null after creation attempt!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MenuManager: Failed to ensure SceneTransitionOverlay: {e.Message}");
            Debug.LogError($"MenuManager: Stack trace: {e.StackTrace}");
        }
    }

    void Update()
    {
        // Quick save/load shortcuts (only when not in menus)
        if (!isSubMenuOpen && (PauseMenu == null || !PauseMenu.activeSelf) && (MainMenu == null || !MainMenu.activeSelf))
        {
            // F5 for quick save
            if (Input.GetKeyDown(KeyCode.F5))
            {
                SaveGame();
            }
            
            // F9 for quick load
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LoadGame();
            }
        }
        
        // Check if the Escape key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // If a submenu is open, close it and return to the pause menu
            if (isSubMenuOpen)
            {
                if (SoundMenu != null && SoundMenu.activeSelf)
                {
                    CloseSoundMenu(); // Close the sound menu
                }
                else if (VideoMenu != null && VideoMenu.activeSelf)
                {
                    CloseVideoMenu(); // Close the video menu
                }
                else if (SettingsMenu != null && SettingsMenu.activeSelf)
                {
                    CloseSettingsMenu(); // Close the settings menu
                }
                else if (CreditsMenu != null && CreditsMenu.activeSelf)
                {
                    CloseCreditsMenu(); // Close the credits menu
                }
                else if (ExitMenu != null && ExitMenu.activeSelf)
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
            if (ExitMenu != null && ExitMenu.activeSelf)
            {
                ExitMenu.SetActive(false); // Hide the modal
                if (PauseMenu != null) PauseMenu.SetActive(false); // Hide the pause menu
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
        if (PauseMenu != null) PauseMenu.SetActive(true); // Show the pause menu
        
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
        if (PauseMenu != null) PauseMenu.SetActive(false); // Hide the pause menu
        
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
        // Add null check for title screen compatibility
        if (PauseMenu == null)
        {
            Debug.LogWarning("MenuManager: PauseMenu not assigned - this is normal for title screen");
            return;
        }
        
        if (PauseMenu.activeSelf && !isSubMenuOpen)
        {
            // If the pause menu is open and no sub-menu is open, close the pause menu
            ResumeGame();
        }
        else if (!PauseMenu.activeSelf && isSubMenuOpen)
        {
            PauseMenu.SetActive(false); // Show the pause menu
            if (SoundMenu != null) SoundMenu.SetActive(false); // Hide the sound menu
            if (VideoMenu != null) VideoMenu.SetActive(false); // Hide the video menu
            isSubMenuOpen = false; // Mark that no sub-menu is open
            ResumeGame();
        }
    }

    public void OpenNewGame()
    {
        // Start a new game by transitioning to the bedroom scene
        Debug.Log("MenuManager: Starting new game - transitioning to Home scene at BedroomSpawnPoint");
        
        if (buildingEntry != null)
        {
            // Hide the main menu before transitioning
            if (MainMenu != null) MainMenu.SetActive(false);
            
            // Use LoadScene instead of EnterBuilding since there's no existing player in menu scene
            buildingEntry.LoadScene("Home", "BedroomSpawnPoint");
        }
        else
        {
            Debug.LogError("MenuManager: ProgrammaticBuildingEntry reference not assigned! Cannot start new game.");
        }
    }
    
    /// <summary>
    /// Save the current game state
    /// </summary>
    public void SaveGame()
    {
        // Try to find SaveManager if not assigned
        if (saveManager == null)
        {
            saveManager = FindAnyObjectByType<SaveManager>();
        }
        
        if (saveManager != null)
        {
            bool success = saveManager.SaveGame();
            if (success)
            {
                Debug.Log("MenuManager: Game saved successfully");
                // Update button states after successful save
                UpdateMenuButtonStates();
            }
            else
            {
                Debug.LogError("MenuManager: Failed to save game");
            }
        }
        else
        {
            Debug.LogError("MenuManager: SaveManager not found - cannot save game");
        }
    }
    
    /// <summary>
    /// Load a saved game
    /// </summary>
    public void LoadGame()
    {
        // Try to find SaveManager if not assigned
        if (saveManager == null)
        {
            saveManager = FindAnyObjectByType<SaveManager>();
        }
        
        if (saveManager != null)
        {
            if (saveManager.SaveFileExists())
            {
                Debug.Log("MenuManager: Starting game load...");
                
                // Show transition overlay immediately
                try
                {
                    if (SceneTransitionOverlay.Instance != null)
                    {
                        SceneTransitionOverlay.Instance.ShowOverlay();
                        Debug.Log("MenuManager: Transition overlay shown for load");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"MenuManager: Could not show transition overlay: {e.Message}");
                }
                
                // Hide any open menus immediately
                if (MainMenu != null) MainMenu.SetActive(false);
                if (PauseMenu != null) PauseMenu.SetActive(false);
                CloseAllSubMenus();
                
                // Ensure proper game state before loading
                Time.timeScale = 1f;
                isSubMenuOpen = false;
                
                bool success = saveManager.LoadGame();
                if (success)
                {
                    Debug.Log("MenuManager: Game loaded successfully");
                    
                    // Additional cleanup after successful load
                    StartCoroutine(PostLoadCleanup());
                }
                else
                {
                    Debug.LogError("MenuManager: Failed to load game");
                    
                    // Hide overlay on failed load
                    try
                    {
                        if (SceneTransitionOverlay.Instance != null)
                        {
                            SceneTransitionOverlay.Instance.ForceHideOverlay();
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"MenuManager: Could not hide overlay after failed load: {e.Message}");
                    }
                    
                    // If load failed and we're in a menu scene, show main menu again
                    if (MainMenu != null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Contains("Menu"))
                    {
                        MainMenu.SetActive(true);
                    }
                }
            }
            else
            {
                Debug.LogWarning("MenuManager: No save file found to load");
            }
        }
        else
        {
            Debug.LogError("MenuManager: SaveManager not found - cannot load game");
        }
    }
    
    /// <summary>
    /// Cleanup after loading to ensure proper game state
    /// </summary>
    private System.Collections.IEnumerator PostLoadCleanup()
    {
        // Wait a frame for scene to fully initialize
        yield return null;
        
        // Ensure all menus are hidden
        if (MainMenu != null) MainMenu.SetActive(false);
        if (PauseMenu != null) PauseMenu.SetActive(false);
        CloseAllSubMenus();
        
        // Set proper game state
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        isSubMenuOpen = false;
        
        // Re-enable player controls that may have been disabled by pause menu
        EnablePlayerControls();
        
        // Wait additional frame and refresh building triggers specifically
        yield return new WaitForSeconds(0.2f);
        RefreshBuildingTriggers();
        
        // Force refresh UI to clear any overlay issues
        RefreshUIComponents();
        
        Debug.Log("MenuManager: Post-load cleanup completed");
    }
    
    private void RefreshBuildingTriggers()
    {
        BuildingEnterTrigger[] triggers = FindObjectsByType<BuildingEnterTrigger>(FindObjectsSortMode.None);
        foreach (BuildingEnterTrigger trigger in triggers)
        {
            trigger.enabled = false;
            trigger.enabled = true;
        }
    }
    
    /// <summary>
    /// Check if a save file exists
    /// </summary>
    public bool HasSaveFile()
    {
        // Try to find SaveManager if not assigned
        if (saveManager == null)
        {
            saveManager = FindAnyObjectByType<SaveManager>();
        }
        
        if (saveManager != null)
        {
            return saveManager.SaveFileExists();
        }
        
        return false;
    }
    
    /// <summary>
    /// Delete the current save file
    /// </summary>
    public void DeleteSave()
    {
        // Try to find SaveManager if not assigned
        if (saveManager == null)
        {
            saveManager = FindAnyObjectByType<SaveManager>();
        }
        
        if (saveManager != null)
        {
            bool success = saveManager.DeleteSave();
            if (success)
            {
                Debug.Log("MenuManager: Save file deleted successfully");
            }
            else
            {
                Debug.LogWarning("MenuManager: No save file to delete or deletion failed");
            }
        }
        else
        {
            Debug.LogError("MenuManager: SaveManager not found - cannot delete save");
        }
    }
    
    /// <summary>
    /// Close all sub-menus (helper method for loading)
    /// </summary>
    private void CloseAllSubMenus()
    {
        if (SoundMenu != null) SoundMenu.SetActive(false);
        if (VideoMenu != null) VideoMenu.SetActive(false);
        if (ExitMenu != null) ExitMenu.SetActive(false);
        if (CreditsMenu != null) CreditsMenu.SetActive(false);
        if (SettingsMenu != null) SettingsMenu.SetActive(false);
        isSubMenuOpen = false;
    }
    
    /// <summary>
    /// Button handler for Save Game button clicks
    /// </summary>
    public void OnSaveButtonClicked()
    {
        Debug.Log("MenuManager: Save button clicked");
        SaveGame();
    }
    
    /// <summary>
    /// Button handler for Load Game button clicks
    /// </summary>
    public void OnLoadButtonClicked()
    {
        Debug.Log("MenuManager: Load button clicked");
        
        // Show overlay immediately for visual feedback
        try
        {
            if (SceneTransitionOverlay.Instance != null)
            {
                SceneTransitionOverlay.Instance.ShowOverlay();
                Debug.Log("MenuManager: Overlay shown from Load button click");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"MenuManager: Could not show overlay from button: {e.Message}");
        }
        
        LoadGame();
    }
    
    /// <summary>
    /// Button handler for Continue button clicks (same as load)
    /// </summary>
    public void OnContinueButtonClicked()
    {
        Debug.Log("MenuManager: Continue button clicked");
        
        // Show overlay immediately for visual feedback
        try
        {
            if (SceneTransitionOverlay.Instance != null)
            {
                SceneTransitionOverlay.Instance.ShowOverlay();
                Debug.Log("MenuManager: Overlay shown from Continue button click");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"MenuManager: Could not show overlay from button: {e.Message}");
        }
        
        LoadGame();
    }
    
    /// <summary>
    /// Enhanced New Game button handler
    /// </summary>
    public void OnNewGameButtonClicked()
    {
        Debug.Log("MenuManager: New Game button clicked");
        OpenNewGame();
    }
    
    /// <summary>
    /// Update button states based on save file existence
    /// </summary>
    public void UpdateMenuButtonStates()
    {
        bool hasSaveFile = HasSaveFile();
        
        // Find and update Continue button (usually in main menu)
        Button continueButton = GameObject.Find("ContinueButton")?.GetComponent<Button>();
        if (continueButton != null)
        {
            continueButton.interactable = hasSaveFile;
            Debug.Log($"MenuManager: Continue button {(hasSaveFile ? "enabled" : "disabled")}");
        }
        
        // Find and update Load button (usually in pause menu)
        Button loadButton = GameObject.Find("LoadButton")?.GetComponent<Button>();
        if (loadButton != null)
        {
            loadButton.interactable = hasSaveFile;
            Debug.Log($"MenuManager: Load button {(hasSaveFile ? "enabled" : "disabled")}");
        }
        
        // Save button is always available (if it exists)
        Button saveButton = GameObject.Find("SaveButton")?.GetComponent<Button>();
        if (saveButton != null)
        {
            saveButton.interactable = true;
        }
        
        // New Game button is always available (if it exists)
        Button newGameButton = GameObject.Find("NewGameButton")?.GetComponent<Button>();
        if (newGameButton != null)
        {
            newGameButton.interactable = true;
        }
    }
    
    public void OpenSettingsMenu()
    {
        // Open settings menu from main title screen
        Debug.Log("MenuManager: Opening Settings Menu from main screen");
        if (SettingsMenu != null)
        {
            SettingsMenu.SetActive(true); // Show the settings menu
            if (MainMenu != null) MainMenu.SetActive(false); // Hide the main menu
            isSubMenuOpen = true; // Mark that a sub-menu is open
        }
        else
        {
            Debug.LogWarning("MenuManager: SettingsMenu not assigned - cannot open settings");
        }
    }
    
    public void OpenCreditsMenu()
    {
        // Open credits menu from main title screen
        Debug.Log("MenuManager: Opening Credits Menu from main screen");
        if (CreditsMenu != null)
        {
            CreditsMenu.SetActive(true); // Show the credits menu
            if (MainMenu != null) MainMenu.SetActive(false); // Hide the main menu
            isSubMenuOpen = true; // Mark that a sub-menu is open
        }
        else
        {
            Debug.LogWarning("MenuManager: CreditsMenu not assigned - cannot open credits");
        }
    }
    
    public void CloseToMainMenu()
    {
        // Return to main menu from any submenu
        Debug.Log("MenuManager: Returning to Main Menu");
        if (SoundMenu != null) SoundMenu.SetActive(false); // Hide settings menu
        if (VideoMenu != null) VideoMenu.SetActive(false); // Hide video menu
        if (ExitMenu != null) ExitMenu.SetActive(false); // Hide exit menu
        if (CreditsMenu != null) CreditsMenu.SetActive(false); // Hide credits menu
        if (SettingsMenu != null) SettingsMenu.SetActive(false); // Hide settings menu
        if (MainMenu != null) MainMenu.SetActive(true); // Show main menu
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    
    public void CloseSettingsMenu()
    {
        if (SettingsMenu != null) SettingsMenu.SetActive(false); // Hide the settings menu
        
        // Check if we came from main menu or pause menu (with null checks)
        bool mainMenuExists = MainMenu != null;
        bool pauseMenuExists = PauseMenu != null;
        bool mainMenuActive = mainMenuExists && MainMenu.activeSelf;
        bool pauseMenuActive = pauseMenuExists && PauseMenu.activeSelf;
        
        if (mainMenuExists && !mainMenuActive && !pauseMenuActive)
        {
            // We came from main menu, return to main menu
            MainMenu.SetActive(true);
            Debug.Log("MenuManager: Returning to Main Menu from Settings");
        }
        else if (pauseMenuExists)
        {
            // We came from pause menu, return to pause menu
            PauseMenu.SetActive(true);
            Debug.Log("MenuManager: Returning to Pause Menu from Settings");
        }
        else
        {
            // No valid menu to return to, default to main menu if available
            if (mainMenuExists)
            {
                MainMenu.SetActive(true);
                Debug.Log("MenuManager: Defaulting to Main Menu from Settings");
            }
        }
        
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    
    public void CloseCreditsMenu()
    {
        if (CreditsMenu != null) CreditsMenu.SetActive(false); // Hide the credits menu
        
        // Check if we came from main menu or pause menu (with null checks)
        bool mainMenuExists = MainMenu != null;
        bool pauseMenuExists = PauseMenu != null;
        bool mainMenuActive = mainMenuExists && MainMenu.activeSelf;
        bool pauseMenuActive = pauseMenuExists && PauseMenu.activeSelf;
        
        if (mainMenuExists && !mainMenuActive && !pauseMenuActive)
        {
            // We came from main menu, return to main menu
            MainMenu.SetActive(true);
            Debug.Log("MenuManager: Returning to Main Menu from Credits");
        }
        else if (pauseMenuExists)
        {
            // We came from pause menu, return to pause menu
            PauseMenu.SetActive(true);
            Debug.Log("MenuManager: Returning to Pause Menu from Credits");
        }
        else
        {
            // No valid menu to return to, default to main menu if available
            if (mainMenuExists)
            {
                MainMenu.SetActive(true);
                Debug.Log("MenuManager: Defaulting to Main Menu from Credits");
            }
        }
        
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    
    public void OpenSoundMenu()
    {
        if (SoundMenu != null) SoundMenu.SetActive(true); // Show the sound menu
        if (PauseMenu != null) PauseMenu.SetActive(false); // Hide the pause menu
        isSubMenuOpen = true; // Mark that a sub-menu is open
    }
    public void CloseSoundMenu()
    {
        if (SoundMenu != null) SoundMenu.SetActive(false); // Hide the sound menu
        
        // Check if we came from main menu or pause menu (with null checks)
        bool mainMenuExists = MainMenu != null;
        bool pauseMenuExists = PauseMenu != null;
        bool mainMenuActive = mainMenuExists && MainMenu.activeSelf;
        bool pauseMenuActive = pauseMenuExists && PauseMenu.activeSelf;
        
        if (mainMenuExists && !mainMenuActive && !pauseMenuActive)
        {
            // We came from main menu, return to main menu
            MainMenu.SetActive(true);
            Debug.Log("MenuManager: Returning to Main Menu from Settings");
        }
        else if (pauseMenuExists)
        {
            // We came from pause menu, return to pause menu
            PauseMenu.SetActive(true);
            Debug.Log("MenuManager: Returning to Pause Menu from Settings");
        }
        else
        {
            // No valid menu to return to, default to main menu if available
            if (mainMenuExists)
            {
                MainMenu.SetActive(true);
                Debug.Log("MenuManager: Defaulting to Main Menu from Settings");
            }
        }
        
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    public void OpenVideoMenu()
    {
        if (VideoMenu != null) VideoMenu.SetActive(true); // Show the video menu
        if (PauseMenu != null) PauseMenu.SetActive(false); // Hide the pause menu
        isSubMenuOpen = true; // Mark that a sub-menu is open
    }
    public void CloseVideoMenu()
    {
        if (VideoMenu != null) VideoMenu.SetActive(false); // Hide the video menu
        if (PauseMenu != null) PauseMenu.SetActive(true); // Show the pause menu
        isSubMenuOpen = false; // Mark that no sub-menu is open
    }
    public void OpenExitConfirmationModal()
    {
        if (ExitMenu != null) ExitMenu.SetActive(true); // Show the exit confirmation modal
    }
    public void CloseExitConfirmationModal()
    {
        if (ExitMenu != null) ExitMenu.SetActive(false); // Hide the exit confirmation modal
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
               (SettingsMenu != null && SettingsMenu.transform.IsChildOf(canvasTransform)) ||
               (CreditsMenu != null && CreditsMenu.transform.IsChildOf(canvasTransform)) ||
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
               (SettingsMenu != null && buttonTransform.IsChildOf(SettingsMenu.transform)) ||
               (CreditsMenu != null && buttonTransform.IsChildOf(CreditsMenu.transform)) ||
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
