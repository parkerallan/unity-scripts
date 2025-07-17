using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A programmatic building entry system that can be used to transition between scenes
/// without requiring trigger colliders. This component can be attached to the GameManager
/// or used as a standalone system.
/// </summary>
public class ProgrammaticBuildingEntry : MonoBehaviour
{
    [Header("Player Reference")]
    public GameObject player; // Reference to the player GameObject
    
    [Header("Debug Settings")]
    public bool enableDebugLogs = true;
    
    // Private fields for tracking scene transitions
    private string _targetSpawnPointName;
    private Vector3 _targetSpawnPosition;
    private Quaternion _targetSpawnRotation;
    private bool _useSpecificPosition = false;
    
    private void Start()
    {
        // Try to find the player if not assigned
        if (player == null)
        {
            player = GameObject.FindWithTag("Player");
            if (player == null && enableDebugLogs)
            {
                Debug.LogWarning("ProgrammaticBuildingEntry: No player GameObject assigned and none found with 'Player' tag.");
            }
        }
    }
    
    // ===============================
    // Public API Methods
    // ===============================
    
    /// <summary>
    /// Enter a building by loading a scene and positioning the player at a named spawn point
    /// </summary>
    /// <param name="sceneToLoad">Name of the scene to load</param>
    /// <param name="spawnPointName">Optional specific spawn point name. If null, will search for "SpawnPoint" tag</param>
    public void EnterBuilding(string sceneToLoad, string spawnPointName = null)
    {
        if (!ValidateSceneTransition(sceneToLoad)) return;
        
        if (enableDebugLogs)
            Debug.Log($"ProgrammaticBuildingEntry: Entering building - Scene: {sceneToLoad}, Spawn Point: {spawnPointName ?? "Default"}");
        
        // Store spawn point name for the scene loaded callback
        _targetSpawnPointName = spawnPointName;
        _useSpecificPosition = false;
        
        InitiateSceneTransition(sceneToLoad);
    }
    
    /// <summary>
    /// Enter a building with a specific position and rotation (useful for precise positioning)
    /// </summary>
    /// <param name="sceneToLoad">Name of the scene to load</param>
    /// <param name="spawnPosition">Exact position to place the player</param>
    /// <param name="spawnRotation">Exact rotation for the player</param>
    public void EnterBuildingAtPosition(string sceneToLoad, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!ValidateSceneTransition(sceneToLoad)) return;
        
        if (enableDebugLogs)
            Debug.Log($"ProgrammaticBuildingEntry: Entering building at position - Scene: {sceneToLoad}, Position: {spawnPosition}");
        
        // Store the target position and rotation
        _targetSpawnPosition = spawnPosition;
        _targetSpawnRotation = spawnRotation;
        _useSpecificPosition = true;
        
        InitiateSceneTransition(sceneToLoad);
    }
    
    /// <summary>
    /// Enter a building at a specific position with Vector3 values
    /// </summary>
    /// <param name="sceneToLoad">Name of the scene to load</param>
    /// <param name="x">X position</param>
    /// <param name="y">Y position</param>
    /// <param name="z">Z position</param>
    /// <param name="rotationY">Y rotation in degrees (optional)</param>
    public void EnterBuildingAtPosition(string sceneToLoad, float x, float y, float z, float rotationY = 0f)
    {
        Vector3 position = new Vector3(x, y, z);
        Quaternion rotation = Quaternion.Euler(0, rotationY, 0);
        EnterBuildingAtPosition(sceneToLoad, position, rotation);
    }
    
    /// <summary>
    /// Set the player reference (useful when the player is spawned dynamically)
    /// </summary>
    /// <param name="playerGameObject">The player GameObject</param>
    public void SetPlayer(GameObject playerGameObject)
    {
        player = playerGameObject;
        if (enableDebugLogs)
            Debug.Log($"ProgrammaticBuildingEntry: Player reference set to {playerGameObject.name}");
    }
    
    // ===============================
    // Private Helper Methods
    // ===============================
    
    private bool ValidateSceneTransition(string sceneToLoad)
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("ProgrammaticBuildingEntry: Scene name cannot be null or empty!");
            return false;
        }
        
        if (player == null)
        {
            // Try to find the player one more time
            player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogError("ProgrammaticBuildingEntry: Player reference is null and no GameObject with 'Player' tag found!");
                return false;
            }
        }
        
        return true;
    }
    
    private void InitiateSceneTransition(string sceneToLoad)
    {
        // Detach the player from its parent to make it a root GameObject
        if (player.transform.parent != null)
        {
            player.transform.SetParent(null);
        }
        
        // Persist the player GameObject
        DontDestroyOnLoad(player);
        
        // Subscribe to the sceneLoaded event and load the scene
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneToLoad);
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe from the sceneLoaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Check for duplicate players and remove any existing ones in the scene
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        if (enableDebugLogs)
            Debug.Log($"ProgrammaticBuildingEntry: Found {existingPlayers.Length} player objects in scene");
        
        foreach (GameObject existingPlayer in existingPlayers)
        {
            // Only destroy players that are NOT our persistent player AND don't have DontDestroyOnLoad
            if (existingPlayer != player && existingPlayer.scene == scene)
            {
                if (enableDebugLogs)
                    Debug.Log($"ProgrammaticBuildingEntry: Destroying duplicate player: {existingPlayer.name}");
                Destroy(existingPlayer);
            }
        }
        
        if (_useSpecificPosition)
        {
            // Use the specific position and rotation provided
            SetPlayerPosition(_targetSpawnPosition, _targetSpawnRotation);
            if (enableDebugLogs)
                Debug.Log($"ProgrammaticBuildingEntry: Player positioned at specific location in scene '{scene.name}'");
            _useSpecificPosition = false; // Reset flag
        }
        else
        {
            // Find and use a spawn point
            Transform spawnPoint = FindSpawnPoint();
            if (spawnPoint != null)
            {
                SetPlayerPosition(spawnPoint.position, spawnPoint.rotation);
                if (enableDebugLogs)
                    Debug.Log($"ProgrammaticBuildingEntry: Player repositioned to spawn point: {spawnPoint.name}");
            }
            else
            {
                string searchTerm = !string.IsNullOrEmpty(_targetSpawnPointName) ? $"named '{_targetSpawnPointName}'" : "with 'SpawnPoint' tag";
                Debug.LogWarning($"ProgrammaticBuildingEntry: No spawn point found {searchTerm} in scene '{scene.name}'.");
            }
        }
        
        if (enableDebugLogs)
            Debug.Log($"ProgrammaticBuildingEntry: Scene {scene.name} loaded successfully.");
        
        // Reset spawn point name for next use
        _targetSpawnPointName = null;
    }
    
    private Transform FindSpawnPoint()
    {
        // If a specific spawn point name is provided, search for it
        if (!string.IsNullOrEmpty(_targetSpawnPointName))
        {
            GameObject namedSpawnPoint = GameObject.Find(_targetSpawnPointName);
            if (namedSpawnPoint != null)
            {
                return namedSpawnPoint.transform;
            }
        }
        
        // Fall back to finding any spawn point with the "SpawnPoint" tag
        GameObject spawnPointObj = GameObject.FindWithTag("SpawnPoint");
        return spawnPointObj?.transform;
    }
    
    /// <summary>
    /// Properly set the player position accounting for child transform hierarchy
    /// This method handles cases where the player has child objects (like CharModel1) that might have offsets
    /// </summary>
    /// <param name="targetPosition">The target world position</param>
    /// <param name="targetRotation">The target world rotation</param>
    private void SetPlayerPosition(Vector3 targetPosition, Quaternion targetRotation)
    {
        // First, set the player's rotation to align properly
        player.transform.rotation = targetRotation;
        
        // Check if the player has a CharModel1 child (common naming pattern)
        Transform charModel = player.transform.Find("CharModel1");
        
        if (charModel != null)
        {
            // If CharModel1 exists, we need to account for its local position offset
            // Calculate the offset between the player's position and CharModel1's world position
            Vector3 charModelWorldPos = charModel.position;
            Vector3 offset = charModelWorldPos - player.transform.position;
            
            // Set the player position so that CharModel1 ends up at the target position
            player.transform.position = targetPosition - offset;
            
            if (enableDebugLogs)
                Debug.Log($"ProgrammaticBuildingEntry: Positioned player accounting for CharModel1 offset: {offset}");
        }
        else
        {
            // No CharModel1 found, position the player directly
            player.transform.position = targetPosition;
            
            if (enableDebugLogs)
                Debug.Log($"ProgrammaticBuildingEntry: No CharModel1 found, positioned player directly");
        }
    }

    // ===============================
    // Utility Methods for Common Scenarios
    // ===============================
    
    /// <summary>
    /// Quick method to return to a main world/hub scene
    /// </summary>
    /// <param name="hubSceneName">Name of the hub scene (default: "MainWorld")</param>
    /// <param name="spawnPointName">Spawn point in the hub scene (default: null)</param>
    public void ReturnToHub(string hubSceneName = "MainWorld", string spawnPointName = null)
    {
        EnterBuilding(hubSceneName, spawnPointName);
    }
    
    /// <summary>
    /// Enter a building with a fade effect (requires additional implementation)
    /// This is a placeholder for future fade transition implementation
    /// </summary>
    /// <param name="sceneToLoad">Scene to load</param>
    /// <param name="spawnPointName">Spawn point name</param>
    /// <param name="fadeTime">Time for fade effect</param>
    public void EnterBuildingWithFade(string sceneToLoad, string spawnPointName = null, float fadeTime = 1f)
    {
        // TODO: Implement fade effect
        // For now, just call the regular enter building method
        if (enableDebugLogs)
            Debug.Log($"ProgrammaticBuildingEntry: Fade transition not yet implemented. Using instant transition.");
        EnterBuilding(sceneToLoad, spawnPointName);
    }
}
