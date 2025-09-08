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
    
    [Header("Player Prefab")]
    public GameObject playerPrefab; // Player prefab to instantiate if no player exists
    
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
        // Load the correct scene if different from current
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != saveData.currentScene)
        {
            // Subscribe to scene loaded event and load the scene
            SceneManager.sceneLoaded += (scene, mode) => OnSceneLoadedForSave(saveData);
            SceneManager.LoadScene(saveData.currentScene);
            return;
        }
        
        // Apply data in current scene
        ApplyDataToCurrentScene(saveData);
    }
    
    private void OnSceneLoadedForSave(SaveData saveData)
    {
        // Unsubscribe from event
        SceneManager.sceneLoaded -= (scene, mode) => OnSceneLoadedForSave(saveData);
        
        // Apply the save data to the newly loaded scene
        StartCoroutine(ApplyDataAfterSceneLoad(saveData));
    }
    
    private System.Collections.IEnumerator ApplyDataAfterSceneLoad(SaveData saveData)
    {
        // Wait for scene to initialize
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        ApplyDataToCurrentScene(saveData);
        SetGameStateAfterLoad();
    }
    
    /// <summary>
    /// Apply save data to the current scene
    /// </summary>
    private void ApplyDataToCurrentScene(SaveData saveData)
    {
        GameObject player = GameObject.FindWithTag("Player");
        
        // If no player exists in the scene, create one from prefab
        if (player == null)
        {
            player = CreatePlayerInScene(saveData);
        }
        
        if (player != null)
        {
            Vector3 loadPosition = new Vector3(saveData.playerPosX, saveData.playerPosY, saveData.playerPosZ);
            Quaternion loadRotation = Quaternion.Euler(0, saveData.playerRotY, 0);
            
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
            
            Target playerTarget = player.GetComponent<Target>();
            if (playerTarget != null)
            {
                playerTarget.health = saveData.playerHealth;
            }
            
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
                }
            }
        }
        else
        {
            Debug.LogError("SaveManager: Failed to create or find player object for loading save data!");
        }
        
        SetGameStateAfterLoad();
    }
    
    /// <summary>
    /// Create a player object in the current scene from the prefab
    /// </summary>
    /// <param name="saveData">Save data to determine spawn position</param>
    /// <returns>The created player GameObject, or null if failed</returns>
    private GameObject CreatePlayerInScene(SaveData saveData)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("SaveManager: No player prefab assigned! Cannot create player for loading save.");
            return null;
        }
        
        try
        {
            // Try to find a spawn point in the current scene first
            Vector3 spawnPosition = FindBestSpawnPosition(saveData);
            Quaternion spawnRotation = Quaternion.Euler(0, saveData.playerRotY, 0);
            
            GameObject newPlayer = Instantiate(playerPrefab, spawnPosition, spawnRotation);
            
            // Make sure it has the Player tag
            if (!newPlayer.CompareTag("Player"))
            {
                newPlayer.tag = "Player";
            }
            
            if (enableDebugLogs)
            {
                Debug.Log($"SaveManager: Created player object at position ({spawnPosition.x:F2}, {spawnPosition.y:F2}, {spawnPosition.z:F2})");
            }
            
            return newPlayer;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SaveManager: Failed to create player object - {e.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Find the best spawn position for the player in the current scene
    /// </summary>
    /// <param name="saveData">Save data with original position</param>
    /// <returns>The best spawn position</returns>
    private Vector3 FindBestSpawnPosition(SaveData saveData)
    {
        // First try to use saved position
        Vector3 savedPosition = new Vector3(saveData.playerPosX, saveData.playerPosY, saveData.playerPosZ);
        
        // Look for spawn points in the scene
        GameObject spawnPoint = GameObject.FindWithTag("SpawnPoint");
        if (spawnPoint != null)
        {
            if (enableDebugLogs)
                Debug.Log("SaveManager: Using SpawnPoint for player creation");
            return spawnPoint.transform.position;
        }
        
        // Look for any object named "PlayerSpawn" or similar
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.ToLower().Contains("spawn") || obj.name.ToLower().Contains("start"))
            {
                if (enableDebugLogs)
                    Debug.Log($"SaveManager: Using {obj.name} for player spawn");
                return obj.transform.position;
            }
        }
        
        // If no spawn points found, use the saved position
        if (enableDebugLogs)
            Debug.Log("SaveManager: No spawn points found, using saved position");
        return savedPosition;
    }
    
    /// <summary>
    /// Set proper game state after loading (ensure no overlays, proper cursor, etc.)
    /// </summary>
    private void SetGameStateAfterLoad()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Hide any scene transition overlays
        try
        {
            if (SceneTransitionOverlay.Instance != null)
            {
                SceneTransitionOverlay.Instance.HideOverlay();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"SaveManager: Could not hide overlay: {e.Message}");
        }
        
        // Handle MenuManager properly to ensure player controls are enabled
        MenuManager menuManager = FindAnyObjectByType<MenuManager>();
        if (menuManager != null)
        {
            // Close all menus and reset menu state
            if (menuManager.MainMenu != null) menuManager.MainMenu.SetActive(false);
            if (menuManager.PauseMenu != null) menuManager.PauseMenu.SetActive(false);
            menuManager.isSubMenuOpen = false;
            
            // Directly enable player controls using reflection since ClosePauseMenu doesn't work when menu is already hidden
            try
            {
                var enableMethod = typeof(MenuManager).GetMethod("EnablePlayerControls", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (enableMethod != null)
                {
                    enableMethod.Invoke(menuManager, null);
                    if (enableDebugLogs)
                        Debug.Log("SaveManager: Player controls enabled via EnablePlayerControls method");
                }
                else
                {
                    Debug.LogWarning("SaveManager: Could not find EnablePlayerControls method");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SaveManager: Could not enable player controls: {e.Message}");
            }
        }
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
