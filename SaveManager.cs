using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SaveData
{
    [Header("Player Position & Scene")]
    public string currentScene;
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;
    public float playerRotY; // Only Y rotation for character controller
    
    [Header("Player Health")]
    public float playerHealth;
    public float playerMaxHealth;
    
    [Header("Weapon Data")]
    public bool hasGun;
    public bool hasRifle;
    public bool gunActive;
    public bool rifleActive;
    
    [Header("Gun Ammo")]
    public int gunCurrentAmmo;
    public int gunReserveAmmo;
    public int gunMaxAmmo;
    
    [Header("Rifle Ammo")]
    public int rifleCurrentAmmo;
    public int rifleReserveAmmo;
    public int rifleMaxAmmo;
    
    [Header("Save Metadata")]
    public string saveDateTime;
    public float timePlayed;
    
    // Constructor with default values
    public SaveData()
    {
        currentScene = "Home";
        playerPosX = 0f;
        playerPosY = 0f;
        playerPosZ = 0f;
        playerRotY = 0f;
        
        playerHealth = 100f;
        playerMaxHealth = 100f;
        
        hasGun = false;
        hasRifle = false;
        gunActive = false;
        rifleActive = false;
        
        gunCurrentAmmo = 20;
        gunReserveAmmo = 100;
        gunMaxAmmo = 20;
        
        rifleCurrentAmmo = 30;
        rifleReserveAmmo = 200;
        rifleMaxAmmo = 30;
        
        saveDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        timePlayed = 0f;
    }
}

public class SaveManager : MonoBehaviour
{
    [Header("Save Settings")]
    public string saveFileName = "gamesave.json";
    public bool enableDebugLogs = true;
    
    [Header("Auto-Save")]
    public bool autoSaveEnabled = true;
    public float autoSaveInterval = 300f; // Auto-save every 5 minutes
    private float lastAutoSaveTime;
    
    private string savePath;
    
    // Singleton pattern for easy access
    public static SaveManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Set up save path
        savePath = Path.Combine(Application.persistentDataPath, saveFileName);
        
        if (enableDebugLogs)
            Debug.Log($"SaveManager: Save file path: {savePath}");
    }
    
    void Start()
    {
        lastAutoSaveTime = Time.time;
    }
    
    void Update()
    {
        // Auto-save functionality
        if (autoSaveEnabled && Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            AutoSave();
            lastAutoSaveTime = Time.time;
        }
    }
    
    /// <summary>
    /// Save the current game state to JSON file
    /// </summary>
    public bool SaveGame()
    {
        try
        {
            SaveData saveData = CollectGameData();
            string jsonData = JsonUtility.ToJson(saveData, true);
            
            File.WriteAllText(savePath, jsonData);
            
            if (enableDebugLogs)
                Debug.Log($"SaveManager: Game saved successfully at {saveData.saveDateTime}");
            
            // Show UI notification if available
            if (SaveLoadUI.Instance != null)
            {
                SaveLoadUI.Instance.ShowSaveNotification(true);
            }
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: Failed to save game - {e.Message}");
            
            // Show UI notification if available
            if (SaveLoadUI.Instance != null)
            {
                SaveLoadUI.Instance.ShowSaveNotification(false);
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Load game state from JSON file
    /// </summary>
    public bool LoadGame()
    {
        try
        {
            if (!File.Exists(savePath))
            {
                Debug.LogWarning("SaveManager: No save file found");
                
                // Show UI notification if available
                if (SaveLoadUI.Instance != null)
                {
                    SaveLoadUI.Instance.ShowNoSaveFileNotification();
                }
                
                return false;
            }
            
            string jsonData = File.ReadAllText(savePath);
            SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
            
            if (saveData != null)
            {
                ApplyGameData(saveData);
                
                if (enableDebugLogs)
                    Debug.Log($"SaveManager: Game loaded successfully from {saveData.saveDateTime}");
                
                // Show UI notification if available
                if (SaveLoadUI.Instance != null)
                {
                    SaveLoadUI.Instance.ShowLoadNotification(true);
                }
                
                return true;
            }
            else
            {
                Debug.LogError("SaveManager: Failed to parse save data");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: Failed to load game - {e.Message}");
            
            // Show UI notification if available
            if (SaveLoadUI.Instance != null)
            {
                SaveLoadUI.Instance.ShowLoadNotification(false);
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Check if a save file exists
    /// </summary>
    public bool SaveFileExists()
    {
        return File.Exists(savePath);
    }
    
    /// <summary>
    /// Get save file info without loading the game
    /// </summary>
    public SaveData GetSaveInfo()
    {
        try
        {
            if (!File.Exists(savePath))
                return null;
            
            string jsonData = File.ReadAllText(savePath);
            return JsonUtility.FromJson<SaveData>(jsonData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: Failed to read save info - {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Delete the save file
    /// </summary>
    public bool DeleteSave()
    {
        try
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                
                if (enableDebugLogs)
                    Debug.Log("SaveManager: Save file deleted");
                
                return true;
            }
            else
            {
                Debug.LogWarning("SaveManager: No save file to delete");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: Failed to delete save file - {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Auto-save (called automatically based on interval)
    /// </summary>
    public void AutoSave()
    {
        if (enableDebugLogs)
            Debug.Log("SaveManager: Auto-saving...");
        
        bool success = SaveGame();
        
        // Show auto-save notification if successful
        if (success && SaveLoadUI.Instance != null)
        {
            SaveLoadUI.Instance.ShowAutoSaveNotification();
        }
    }
    
    /// <summary>
    /// Collect all game data for saving
    /// </summary>
    private SaveData CollectGameData()
    {
        SaveData saveData = new SaveData();
        
        // Current scene
        saveData.currentScene = SceneManager.GetActiveScene().name;
        
        // Find player in current scene
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Player position and rotation
            Vector3 playerPos = player.transform.position;
            saveData.playerPosX = playerPos.x;
            saveData.playerPosY = playerPos.y;
            saveData.playerPosZ = playerPos.z;
            saveData.playerRotY = player.transform.eulerAngles.y;
            
            // Player health
            Target playerTarget = player.GetComponent<Target>();
            if (playerTarget != null)
            {
                saveData.playerHealth = playerTarget.health;
                saveData.playerMaxHealth = playerTarget.maxHealth;
            }
            
            // Weapon data
            GunScript gunScript = player.GetComponent<GunScript>();
            if (gunScript != null)
            {
                saveData.hasGun = true;
                saveData.gunActive = gunScript.isGunActive;
                saveData.gunCurrentAmmo = gunScript.currentAmmo;
                saveData.gunReserveAmmo = gunScript.reserveAmmo;
                saveData.gunMaxAmmo = gunScript.maxAmmo;
            }
            
            RifleScript rifleScript = player.GetComponent<RifleScript>();
            if (rifleScript != null)
            {
                saveData.hasRifle = true;
                saveData.rifleActive = rifleScript.isRifleActive;
                saveData.rifleCurrentAmmo = rifleScript.currentAmmo;
                saveData.rifleReserveAmmo = rifleScript.reserveAmmo;
                saveData.rifleMaxAmmo = rifleScript.maxAmmo;
            }
        }
        else
        {
            Debug.LogWarning("SaveManager: Player not found in scene for saving");
        }
        
        // Save metadata
        saveData.saveDateTime = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        saveData.timePlayed = Time.time; // Simple time tracking
        
        if (enableDebugLogs)
        {
            Debug.Log($"SaveManager: Collected data - Scene: {saveData.currentScene}, " +
                      $"Position: ({saveData.playerPosX:F2}, {saveData.playerPosY:F2}, {saveData.playerPosZ:F2}), " +
                      $"Health: {saveData.playerHealth}/{saveData.playerMaxHealth}, " +
                      $"Gun: {(saveData.hasGun ? $"{saveData.gunCurrentAmmo}/{saveData.gunReserveAmmo}" : "None")}, " +
                      $"Rifle: {(saveData.hasRifle ? $"{saveData.rifleCurrentAmmo}/{saveData.rifleReserveAmmo}" : "None")}");
        }
        
        return saveData;
    }
    
    /// <summary>
    /// Apply loaded data to the game
    /// </summary>
    private void ApplyGameData(SaveData saveData)
    {
        // Show the transition overlay immediately when starting load process
        try
        {
            if (SceneTransitionOverlay.Instance != null)
            {
                SceneTransitionOverlay.Instance.ShowOverlay();
                if (enableDebugLogs)
                    Debug.Log("SaveManager: Transition overlay shown for load process");
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"SaveManager: Could not show transition overlay: {e.Message}");
        }
        
        // Load the correct scene if different from current
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != saveData.currentScene)
        {
            if (enableDebugLogs)
                Debug.Log($"SaveManager: Loading scene {saveData.currentScene}");
            
            // Use ProgrammaticBuildingEntry to load scene with proper positioning
            ProgrammaticBuildingEntry buildingEntry = FindAnyObjectByType<ProgrammaticBuildingEntry>();
            if (buildingEntry != null)
            {
                // Store save data to apply after scene loads
                StartCoroutine(LoadSceneAndApplyData(saveData));
                return;
            }
            else
            {
                // Fallback to direct scene loading
                SceneManager.sceneLoaded += (scene, mode) => OnSceneLoadedApplyData(saveData);
                SceneManager.LoadScene(saveData.currentScene);
                return;
            }
        }
        
        // Apply data in current scene
        ApplyDataToCurrentScene(saveData);
    }
    
    /// <summary>
    /// Load scene and apply save data
    /// </summary>
    private System.Collections.IEnumerator LoadSceneAndApplyData(SaveData saveData)
    {
        if (enableDebugLogs)
            Debug.Log($"SaveManager: Starting scene transition to {saveData.currentScene}");
        
        // Load the scene using ProgrammaticBuildingEntry (which will also show overlay)
        ProgrammaticBuildingEntry buildingEntry = FindAnyObjectByType<ProgrammaticBuildingEntry>();
        if (buildingEntry != null)
        {
            // Note: LoadScene will show its own overlay, but we already showed one
            // This ensures the overlay is visible throughout the entire process
            buildingEntry.LoadScene(saveData.currentScene);
        }
        else
        {
            // Fallback: direct scene loading
            SceneManager.LoadScene(saveData.currentScene);
        }
        
        // Wait longer for scene to fully load and initialize
        yield return new WaitForSeconds(2f);
        
        if (enableDebugLogs)
            Debug.Log("SaveManager: Scene loaded, applying save data...");
        
        // Apply the saved data
        ApplyDataToCurrentScene(saveData);
        
        // Additional wait to ensure everything is properly set up
        yield return new WaitForSeconds(0.5f);
        
        // Final cleanup to ensure proper state
        SetGameStateAfterLoad();
        
        if (enableDebugLogs)
            Debug.Log("SaveManager: Load process completed");
    }
    
    /// <summary>
    /// Callback for when scene loads during game loading
    /// </summary>
    private void OnSceneLoadedApplyData(SaveData saveData)
    {
        SceneManager.sceneLoaded -= (scene, mode) => OnSceneLoadedApplyData(saveData);
        
        // Wait a frame for scene initialization
        StartCoroutine(DelayedApplyData(saveData));
    }
    
    private System.Collections.IEnumerator DelayedApplyData(SaveData saveData)
    {
        yield return null; // Wait one frame
        yield return new WaitForSeconds(1f); // Wait additional time for scene setup
        
        ApplyDataToCurrentScene(saveData);
        
        // Additional cleanup
        yield return new WaitForSeconds(0.5f);
        SetGameStateAfterLoad();
    }
    
    /// <summary>
    /// Apply save data to the current scene
    /// </summary>
    private void ApplyDataToCurrentScene(SaveData saveData)
    {
        // Find player in the loaded scene
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            // Apply player position and rotation
            Vector3 loadPosition = new Vector3(saveData.playerPosX, saveData.playerPosY, saveData.playerPosZ);
            Quaternion loadRotation = Quaternion.Euler(0, saveData.playerRotY, 0);
            
            // Handle CharacterController positioning
            CharacterController characterController = player.GetComponent<CharacterController>();
            if (characterController != null)
            {
                characterController.enabled = false;
                player.transform.SetPositionAndRotation(loadPosition, loadRotation);
                characterController.enabled = true;
            }
            else
            {
                player.transform.SetPositionAndRotation(loadPosition, loadRotation);
            }
            
            if (enableDebugLogs)
                Debug.Log($"SaveManager: Player positioned at {loadPosition}");
            
            // Apply player health
            Target playerTarget = player.GetComponent<Target>();
            if (playerTarget != null)
            {
                playerTarget.health = saveData.playerHealth;
                // Note: maxHealth is set in Start(), so we don't override it here
                
                if (enableDebugLogs)
                    Debug.Log($"SaveManager: Player health set to {saveData.playerHealth}");
            }
            
            // Apply weapon data
            if (saveData.hasGun)
            {
                GunScript gunScript = player.GetComponent<GunScript>();
                if (gunScript != null)
                {
                    gunScript.currentAmmo = saveData.gunCurrentAmmo;
                    gunScript.reserveAmmo = saveData.gunReserveAmmo;
                    gunScript.maxAmmo = saveData.gunMaxAmmo;
                    
                    if (saveData.gunActive)
                    {
                        gunScript.ActivateGun();
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"SaveManager: Gun ammo set to {saveData.gunCurrentAmmo}/{saveData.gunReserveAmmo}");
                }
            }
            
            if (saveData.hasRifle)
            {
                RifleScript rifleScript = player.GetComponent<RifleScript>();
                if (rifleScript != null)
                {
                    rifleScript.currentAmmo = saveData.rifleCurrentAmmo;
                    rifleScript.reserveAmmo = saveData.rifleReserveAmmo;
                    rifleScript.maxAmmo = saveData.rifleMaxAmmo;
                    
                    if (saveData.rifleActive)
                    {
                        rifleScript.ActivateRifle();
                    }
                    
                    if (enableDebugLogs)
                        Debug.Log($"SaveManager: Rifle ammo set to {saveData.rifleCurrentAmmo}/{saveData.rifleReserveAmmo}");
                }
            }
        }
        else
        {
            Debug.LogError("SaveManager: Player not found in loaded scene");
        }
        
        // Ensure proper game state after loading
        SetGameStateAfterLoad();
        
        if (enableDebugLogs)
            Debug.Log($"SaveManager: Game state applied successfully in scene {saveData.currentScene}");
    }
    
    /// <summary>
    /// Set proper game state after loading (ensure no overlays, proper cursor, etc.)
    /// </summary>
    private void SetGameStateAfterLoad()
    {
        // Hide any transition overlays
        try
        {
            if (SceneTransitionOverlay.Instance != null)
            {
                SceneTransitionOverlay.Instance.HideOverlay();
                SceneTransitionOverlay.Instance.ForceHideOverlay();
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"SaveManager: Could not hide overlay: {e.Message}");
        }
        
        // Set proper game state (resumed, not paused)
        Time.timeScale = 1f;
        
        // Set proper cursor state for gameplay
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Find and configure MenuManager for proper game state
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        if (menuManager != null)
        {
            // Hide all menus
            if (menuManager.MainMenu != null) menuManager.MainMenu.SetActive(false);
            if (menuManager.PauseMenu != null) menuManager.PauseMenu.SetActive(false);
            if (menuManager.SettingsMenu != null) menuManager.SettingsMenu.SetActive(false);
            if (menuManager.CreditsMenu != null) menuManager.CreditsMenu.SetActive(false);
            if (menuManager.SoundMenu != null) menuManager.SoundMenu.SetActive(false);
            if (menuManager.VideoMenu != null) menuManager.VideoMenu.SetActive(false);
            if (menuManager.ExitMenu != null) menuManager.ExitMenu.SetActive(false);
            
            // Reset submenu state
            menuManager.isSubMenuOpen = false;
            
            // Force refresh UI components to prevent overlay issues
            menuManager.RefreshUIComponents();
            
            if (enableDebugLogs)
                Debug.Log("SaveManager: MenuManager state reset after load");
        }
        
        // Enable player controls (in case they were disabled by menu system)
        PlayerController playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        UnderwaterPlayerController underwaterController = FindAnyObjectByType<UnderwaterPlayerController>();
        if (underwaterController != null)
        {
            underwaterController.enabled = true;
        }
        
        // Enable weapon scripts
        GunScript gunScript = FindAnyObjectByType<GunScript>();
        if (gunScript != null)
        {
            gunScript.enabled = true;
        }
        
        RifleScript rifleScript = FindAnyObjectByType<RifleScript>();
        if (rifleScript != null)
        {
            rifleScript.enabled = true;
        }
        
        WeaponManager weaponManager = FindAnyObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            weaponManager.enabled = true;
        }
        
        if (enableDebugLogs)
            Debug.Log("SaveManager: Game state properly set after load - ready for gameplay");
    }
    
    /// <summary>
    /// Quick save method for easy access
    /// </summary>
    public void QuickSave()
    {
        if (enableDebugLogs)
            Debug.Log("SaveManager: Quick saving...");
        
        SaveGame();
    }
    
    /// <summary>
    /// Quick load method for easy access
    /// </summary>
    public void QuickLoad()
    {
        if (enableDebugLogs)
            Debug.Log("SaveManager: Quick loading...");
        
        LoadGame();
    }
}
