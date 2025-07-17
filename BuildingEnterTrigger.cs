using UnityEngine;
using UnityEngine.SceneManagement;

public class BuildingEnterTrigger : MonoBehaviour
{
    public string sceneToLoad; // Name of the scene to load
    public string spawnPointName; // Name of the spawn point to use in the target scene
    public GameObject markerEffect; // Reference to the particle effect
    public bool isPlayerInRange = false; // Track if the player is in range
    public GameObject player; // Reference to the player GameObject

    private void Update()
    {
        // Check if the player is in range and presses the "E" key
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            EnterBuilding(); // Trigger the scene transition
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is the player
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            player = other.gameObject; // Assign the player reference dynamically
            Debug.Log("Player is at the building entrance. Press 'E' to enter.");

            // Activate the particle effect
            if (markerEffect != null)
            {
                markerEffect.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the object exiting the trigger is the player
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            Debug.Log("Player left the building entrance.");

            // Deactivate the particle effect
            if (markerEffect != null)
            {
                markerEffect.SetActive(false);
            }
        }
    }

    private void EnterBuilding()
    {
        if (player == null)
        {
            Debug.LogError("Player reference is null. Cannot transfer to the new scene.");
            return;
        }

        Debug.Log($"Entering building and loading scene: {sceneToLoad}");

        // Detach the player from its parent to make it a root GameObject
        if (player.transform.parent != null)
        {
            player.transform.SetParent(null);
        }

        // Persist the player GameObject
        DontDestroyOnLoad(player);

        // Load the specified scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to the sceneLoaded event
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("Scene name is not set in the BuildingEnterTrigger script!");
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe from the sceneLoaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Check for duplicate players and remove any existing ones in the scene
        GameObject[] existingPlayers = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log($"Found {existingPlayers.Length} player objects in scene");
        
        foreach (GameObject existingPlayer in existingPlayers)
        {
            // Only destroy players that are NOT our persistent player AND don't have DontDestroyOnLoad
            if (existingPlayer != player && existingPlayer.scene == scene)
            {
                Debug.Log($"Destroying duplicate player: {existingPlayer.name}");
                Destroy(existingPlayer);
            }
        }

        // Find the spawn point in the loaded scene
        Transform spawnPoint = FindSpawnPoint();
        if (spawnPoint != null)
        {
            // Reposition the player at the spawn point using proper transform handling
            SetPlayerPosition(spawnPoint.position, spawnPoint.rotation);
            Debug.Log($"Player repositioned to spawn point: {spawnPoint.name} at {spawnPoint.position}");
        }
        else
        {
            string searchTerm = !string.IsNullOrEmpty(spawnPointName) ? $"named '{spawnPointName}'" : "with 'SpawnPoint' tag";
            Debug.LogWarning($"No spawn point found {searchTerm} in scene '{scene.name}'.");
        }

        Debug.Log($"Scene {scene.name} loaded successfully.");
    }

    private Transform FindSpawnPoint()
    {
        // If a specific spawn point name is provided, search for it
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            GameObject namedSpawnPoint = GameObject.Find(spawnPointName);
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
            
            Debug.Log($"BuildingEnterTrigger: Positioned player accounting for CharModel1 offset: {offset}");
        }
        else
        {
            // No CharModel1 found, position the player directly
            player.transform.position = targetPosition;
            
            Debug.Log($"BuildingEnterTrigger: No CharModel1 found, positioned player directly");
        }
    }
}
