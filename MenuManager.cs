using UnityEngine;
using TMPro;
using System.Collections.Generic;

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
                Time.timeScale = 1f; // Resume the game
                Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
                Cursor.visible = false; // Hide the cursor
                return;
            }

            if (Time.timeScale == 1f)
            {
                // Pause the game
                Cursor.lockState = CursorLockMode.None; // Unlock the cursor
                Cursor.visible = true; // Show the cursor
                Time.timeScale = 0f; // Pause the game
                PauseMenu.SetActive(true); // Show the pause menu
            }
            else
            {
                // Resume the game
                Time.timeScale = 1f; // Resume the game
                PauseMenu.SetActive(false); // Hide the pause menu
                Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
                Cursor.visible = false; // Hide the cursor
            }
        }
    }
    public void ClosePauseMenu()
    {
        if (PauseMenu.activeSelf && !isSubMenuOpen)
        {
            // If the pause menu is open and no sub-menu is open, close the pause menu
            PauseMenu.SetActive(false); // Hide the pause menu
            Time.timeScale = 1f; // Resume the game
            Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
            Cursor.visible = false; // Hide the cursor
        }
        else if (!PauseMenu.activeSelf && isSubMenuOpen)
        {
            PauseMenu.SetActive(false); // Show the pause menu
            SoundMenu.SetActive(false); // Hide the sound menu
            VideoMenu.SetActive(false); // Hide the video menu
            isSubMenuOpen = false; // Mark that no sub-menu is open
            Time.timeScale = 1f; // Resume the game
            Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
            Cursor.visible = false; // Hide the cursor
        }
    }
    public void StartGame()
    {
        // Called when the player starts the game from the Main Menu
        MainMenu.SetActive(false); // Hide the Main Menu
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor
        Cursor.visible = false; // Hide the cursor
        Time.timeScale = 1f; // Resume the game
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
}
