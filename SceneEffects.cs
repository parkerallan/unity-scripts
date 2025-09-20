using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SceneEffects : MonoBehaviour
{
    private Animator playerAnimator;
    private Image fadeImage;
    private ProgrammaticBuildingEntry buildingEntry;
    private Transform charModel1Transform;
    
    [Header("CharModel1 Transformation Settings")]
    public Vector3 underworldPosition = new Vector3(0, 0, 0);
    public Vector3 underworldRotation = new Vector3(0, 0, 0);
    
    private void Start()
    {
        // Auto-find all required components
        FindPlayerAnimator();
        FindFadeImage();
        FindBuildingEntry();
        FindCharModel1Transform();
        
        // Automatically fade in when scene loads (in case we're coming from a black fade)
        StartCoroutine(FadeInOnStart());
        
        // Check if boss was defeated and enable victory dialogue if so
        CheckForBossVictory();
    }
    
    private void FindPlayerAnimator()
    {
        // Try multiple ways to find the player animator
        GameObject player1 = GameObject.Find("Player1");
        if (player1 != null)
        {
            playerAnimator = player1.GetComponent<Animator>();
            Debug.Log("SceneEffects: Found Player1 with Animator");
        }
        else
        {
            // Try CharModel1
            GameObject charModel = GameObject.Find("CharModel1");
            if (charModel != null)
            {
                playerAnimator = charModel.GetComponent<Animator>();
                Debug.Log("SceneEffects: Found CharModel1 with Animator");
            }
            else
            {
                // Try finding by Player tag
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerAnimator = player.GetComponentInChildren<Animator>();
                    Debug.Log("SceneEffects: Found Player by tag with Animator");
                }
                else
                {
                    Debug.LogError("SceneEffects: Could not find player animator!");
                }
            }
        }
    }
    
    private void FindCharModel1Transform()
    {
        GameObject charModel1 = GameObject.Find("CharModel1");
        if (charModel1 != null)
        {
            charModel1Transform = charModel1.transform;
            Debug.Log("SceneEffects: Found CharModel1 transform");
        }
        else
        {
            Debug.LogWarning("SceneEffects: Could not find CharModel1 transform!");
        }
    }
    
    private void FindFadeImage()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"SceneEffects: Found {canvases.Length} canvases");
        
        foreach (Canvas canvas in canvases)
        {
            Debug.Log($"SceneEffects: Checking canvas: {canvas.name}");
            
            Transform fadeTransform = canvas.transform.Find("FadeImage");
            if (fadeTransform == null)
                fadeTransform = canvas.transform.Find("BlackScreen");
            if (fadeTransform == null)
                fadeTransform = canvas.transform.Find("ScreenFade");
                
            if (fadeTransform != null)
            {
                fadeImage = fadeTransform.GetComponent<Image>();
                if (fadeImage != null)
                {
                    Debug.Log($"SceneEffects: Found fade image: {fadeTransform.name}");
                    break;
                }
            }
        }
        
        if (fadeImage == null)
        {
            Debug.Log("SceneEffects: No fade image found - creating one automatically");
            CreateFadeImage();
        }
    }
    
    private void CreateFadeImage()
    {
        // Find or create a canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("FadeCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999; // High priority
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Create fade image
        GameObject fadeObj = new GameObject("FadeImage");
        fadeObj.transform.SetParent(canvas.transform, false);
        
        fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0); // Black, fully transparent
        
        // Make it fullscreen
        RectTransform rectTransform = fadeObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Initially hide it
        fadeObj.SetActive(false);
        
        Debug.Log("SceneEffects: Created fade image automatically");
    }
    
    private void FindBuildingEntry()
    {
        buildingEntry = FindFirstObjectByType<ProgrammaticBuildingEntry>();
        if (buildingEntry != null)
        {
            Debug.Log("SceneEffects: Found ProgrammaticBuildingEntry");
        }
        else
        {
            Debug.LogError("SceneEffects: Could not find ProgrammaticBuildingEntry!");
        }
    }
    
    public void TurnToUnderworld()
    {
        Debug.Log("SceneEffects: TurnToUnderworld called");
        Debug.Log($"SceneEffects: playerAnimator = {(playerAnimator != null ? "Found" : "NULL")}");
        Debug.Log($"SceneEffects: fadeImage = {(fadeImage != null ? "Found" : "NULL")}");
        Debug.Log($"SceneEffects: buildingEntry = {(buildingEntry != null ? "Found" : "NULL")}");
        
        StartCoroutine(TurnToUnderworldSequence());
    }
    
    private IEnumerator TurnToUnderworldSequence()
    {
        Debug.Log("SceneEffects: Starting underworld sequence");
        
        // First, transform CharModel1 to the specified position/rotation
        if (charModel1Transform != null)
        {
            Debug.Log($"SceneEffects: Moving CharModel1 to position {underworldPosition} and rotation {underworldRotation}");
            charModel1Transform.position = underworldPosition;
            charModel1Transform.rotation = Quaternion.Euler(underworldRotation);
        }
        else
        {
            Debug.LogWarning("SceneEffects: CharModel1 transform not found - skipping transformation");
        }
        
        // Play SitDown animation
        if (playerAnimator != null)
        {
            Debug.Log("SceneEffects: Triggering SitDown animation");
            playerAnimator.SetTrigger("SitDown");
        }
        else
        {
            Debug.LogError("SceneEffects: No player animator - skipping animation");
        }
        
        // Wait 2 seconds
        // Debug.Log("SceneEffects: Waiting 2 seconds");
        // yield return new WaitForSeconds(3f);
        
        // Fade to black
        if (fadeImage != null)
        {
            Debug.Log("SceneEffects: Starting fade to black");
            yield return StartCoroutine(FadeToBlack());
        }
        else
        {
            Debug.LogError("SceneEffects: No fade image - skipping fade");
        }
        
        // Wait 3 second
        Debug.Log("SceneEffects: Waiting 3 second before scene load");
        yield return new WaitForSeconds(3f);
        
        // Destroy fade image before scene transition to prevent it carrying over
        if (fadeImage != null)
        {
            Destroy(fadeImage.gameObject);
            fadeImage = null;
        }
        
        // Load SchoolUnderworld scene at AtChairUnderworld spawn point
        if (buildingEntry != null)
        {
            Debug.Log("SceneEffects: Loading SchoolUnderworld scene");
            buildingEntry.LoadScene("SchoolUnderworld", "AtChairUnderworld");
        }
        else
        {
            Debug.LogError("SceneEffects: No building entry - cannot load scene");
        }
    }
    
    private IEnumerator FadeToBlack()
    {
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;
        
        float elapsedTime = 0f;
        while (elapsedTime < 2f)
        {
            elapsedTime += Time.deltaTime;
            color.a = elapsedTime / 2f;
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 1f;
        fadeImage.color = color;
    }
    
    private IEnumerator FadeInOnStart()
    {
        yield return new WaitForSeconds(0.5f);
        
        if (fadeImage != null && fadeImage.gameObject.activeInHierarchy)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
            fadeImage.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator FadeFromBlack()
    {
        if (fadeImage == null) yield break;
        
        fadeImage.gameObject.SetActive(true);
        Color color = fadeImage.color;
        color.a = 1f; // Start fully black
        fadeImage.color = color;
        
        float elapsedTime = 0f;
        while (elapsedTime < 2f)
        {
            elapsedTime += Time.deltaTime;
            color.a = 1f - (elapsedTime / 2f); // Fade from black to transparent
            fadeImage.color = color;
            yield return null;
        }
        
        color.a = 0f;
        fadeImage.color = color;
        fadeImage.gameObject.SetActive(false);
        Debug.Log("SceneEffects: Fade in complete");
    }
    
    public void FadeIn()
    {
        StartCoroutine(FadeFromBlack());
    }
    
    [Header("Bomb Gates Settings")]
    public float bombDelay = 3f;
    public ParticleSystem explosionEffect;
    public AudioClip explosionSFX;
    public Transform[] objectsToTransform = new Transform[3];
    public Vector3[] newPositions = new Vector3[3];
    public Vector3[] newRotations = new Vector3[3];
    public Vector3[] newScales = new Vector3[3];
    
    [Header("Teleport Settings")]
    public ParticleSystem teleportEffect;
    public AudioClip teleportSFX;
    
    [Header("Script Control Settings")]
    public MonoBehaviour scriptToEnable;
    
    [Header("Block Removal Settings")]
    public GameObject[] blocksToRemove = new GameObject[5]; // Array of objects to disable
    
    [Header("Block Enable Settings")]
    public GameObject[] blocksToEnable = new GameObject[5]; // Array of objects to enable
    
    public void Teleport()
    {
        StartCoroutine(TeleportSequence());
    }
    
    private IEnumerator TeleportSequence()
    {

        
        // Play teleport particle effect
        if (teleportEffect != null)
        {
            teleportEffect.Play();
        }
        
        // Play teleport sound effect
        if (teleportSFX != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(teleportSFX, transform, 1f);
        }
        
        // Wait for specified delay
        yield return new WaitForSeconds(2f);
        
        // Disable the GameObject this script is attached to
        Debug.Log($"SceneEffects: Disabling GameObject {gameObject.name}");
        gameObject.SetActive(false);
    }
    
    public void TeleportToTown()
    {
        StartCoroutine(TeleportToTownSequence());
    }
    
    private IEnumerator TeleportToTownSequence()
    {
        Debug.Log("SceneEffects: Starting teleport to Town scene");
        
        // Play teleport particle effect
        if (teleportEffect != null)
        {
            teleportEffect.Play();
        }
        
        // Play teleport sound effect
        if (teleportSFX != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(teleportSFX, transform, 1f);
        }
        
        // Wait for specified delay
        yield return new WaitForSeconds(2f);
        
        // Teleport to Town scene at FrontOfBus spawn point
        if (buildingEntry != null)
        {
            Debug.Log("SceneEffects: Teleporting to Town scene at FrontOfSchool spawn point");
            
            // Set a flag to indicate boss was defeated (this will persist across scene loads)
            PlayerPrefs.SetInt("BossDefeated", 1);
            PlayerPrefs.Save();
            
            buildingEntry.EnterBuilding("Town", "FrontOfSchool");
        }
        else
        {
            Debug.LogError("SceneEffects: No building entry - cannot teleport to Town");
        }
    }
    
    public void TeleportToHome()
    {
        StartCoroutine(TeleportToHomeSequence());
    }
    
    private IEnumerator TeleportToHomeSequence()
    {
        Debug.Log("SceneEffects: Starting teleport to Home scene");
        
        // Play teleport particle effect
        if (teleportEffect != null)
        {
            teleportEffect.Play();
        }
        
        // Play teleport sound effect
        if (teleportSFX != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(teleportSFX, transform, 1f);
        }
        
        // Wait for specified delay
        yield return new WaitForSeconds(2f);
        
        // Teleport to Home scene at BedroomSpawnPoint
        if (buildingEntry != null)
        {
            Debug.Log("SceneEffects: Teleporting to Home scene at BedroomSpawnPoint");
            
            // Set a flag to indicate we should start the laying sequence in Home scene
            PlayerPrefs.SetInt("StartLayingSequence", 1);
            
            // Also set a flag to ensure proper positioning at spawn point
            PlayerPrefs.SetInt("PositionAtSpawn", 1);
            PlayerPrefs.SetString("TargetSpawnPoint", "BedroomSpawnPoint");
            PlayerPrefs.Save();
            
            buildingEntry.EnterBuilding("Home", "BedroomSpawnPoint");
        }
        else
        {
            Debug.LogError("SceneEffects: No building entry - cannot teleport to Home");
        }
    }
    
    public void EnableScript()
    {
        if (scriptToEnable != null)
        {
            scriptToEnable.enabled = true;
            Debug.Log($"SceneEffects: Enabled script {scriptToEnable.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("SceneEffects: No script assigned to enable!");
        }
    }
    
    public void RemoveBlock()
    {
        Debug.Log($"SceneEffects: RemoveBlock called - processing {blocksToRemove.Length} objects");
        
        int removedCount = 0;
        for (int i = 0; i < blocksToRemove.Length; i++)
        {
            if (blocksToRemove[i] != null)
            {
                blocksToRemove[i].SetActive(false);
                Debug.Log($"SceneEffects: Disabled GameObject {blocksToRemove[i].name}");
                removedCount++;
            }
        }
        
        if (removedCount == 0)
        {
            Debug.LogWarning("SceneEffects: No GameObjects assigned to remove!");
        }
        else
        {
            Debug.Log($"SceneEffects: Successfully disabled {removedCount} GameObjects");
        }
    }
    
    public void EnableBlock()
    {
        Debug.Log($"SceneEffects: EnableBlock called - processing {blocksToEnable.Length} objects");
        
        int enabledCount = 0;
        for (int i = 0; i < blocksToEnable.Length; i++)
        {
            if (blocksToEnable[i] != null)
            {
                blocksToEnable[i].SetActive(true);
                Debug.Log($"SceneEffects: Enabled GameObject {blocksToEnable[i].name}");
                enabledCount++;
            }
        }
        
        if (enabledCount == 0)
        {
            Debug.LogWarning("SceneEffects: No GameObjects assigned to enable!");
        }
        else
        {
            Debug.Log($"SceneEffects: Successfully enabled {enabledCount} GameObjects");
        }
    }
    
    // ===============================
    // Boss Victory Dialogue System
    // ===============================
    
    [Header("Boss Victory Dialogue")]
    public GameObject victoryDialogueObject; // Assign the dialogue object that should appear after boss defeat
    
    /// <summary>
    /// Call this method in the Town scene Start() to check if boss was defeated
    /// and enable the victory dialogue if so
    /// </summary>
    public void CheckForBossVictory()
    {
        if (PlayerPrefs.GetInt("BossDefeated", 0) == 1)
        {
            Debug.Log("SceneEffects: Boss was defeated - enabling victory dialogue");
            
            if (victoryDialogueObject != null)
            {
                victoryDialogueObject.SetActive(true);
                Debug.Log($"SceneEffects: Enabled victory dialogue object: {victoryDialogueObject.name}");
                
                // Clear the flag so it only happens once
                PlayerPrefs.SetInt("BossDefeated", 0);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogWarning("SceneEffects: Victory dialogue object not assigned!");
            }
        }
        else
        {
            Debug.Log("SceneEffects: No boss victory detected");
        }
        
        // Check for laying sequence flag (for TeleportToHome)
        CheckForLayingSequence();
        
        // Check for positioning flag (for precise spawn point positioning)
        CheckForSpawnPointPositioning();
    }
    
    /// <summary>
    /// Call this method in the Home scene Start() to check if we should start the laying sequence
    /// </summary>
    public void CheckForLayingSequence()
    {
        if (PlayerPrefs.GetInt("StartLayingSequence", 0) == 1)
        {
            Debug.Log("SceneEffects: Starting laying sequence in Home scene");
            
            // Clear the flag so it only happens once
            PlayerPrefs.SetInt("StartLayingSequence", 0);
            PlayerPrefs.Save();
            
            // Start the laying sequence
            StartCoroutine(HandleLayingSequence());
        }
        else
        {
            Debug.Log("SceneEffects: No laying sequence requested");
        }
    }
    
    /// <summary>
    /// Check if we need to position the player precisely at a spawn point
    /// </summary>
    public void CheckForSpawnPointPositioning()
    {
        if (PlayerPrefs.GetInt("PositionAtSpawn", 0) == 1)
        {
            string targetSpawnPoint = PlayerPrefs.GetString("TargetSpawnPoint", "");
            Debug.Log($"SceneEffects: Positioning player at spawn point: {targetSpawnPoint}");
            
            // Clear the flags so it only happens once
            PlayerPrefs.SetInt("PositionAtSpawn", 0);
            PlayerPrefs.DeleteKey("TargetSpawnPoint");
            PlayerPrefs.Save();
            
            // Position the player at the spawn point
            StartCoroutine(PositionPlayerAtSpawnPoint(targetSpawnPoint));
        }
        else
        {
            Debug.Log("SceneEffects: No spawn point positioning requested");
        }
    }
    
    /// <summary>
    /// Position the player exactly at the specified spawn point
    /// </summary>
    private System.Collections.IEnumerator PositionPlayerAtSpawnPoint(string spawnPointName)
    {
        // Wait a moment for scene to fully initialize
        yield return new WaitForSeconds(0.1f);
        
        // Find the spawn point GameObject
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint == null)
        {
            Debug.LogError($"SceneEffects: Could not find spawn point: {spawnPointName}");
            yield break;
        }
        
        // Find the player GameObject
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("CharModel1");
        }
        
        if (player == null)
        {
            Debug.LogError("SceneEffects: Could not find player to position");
            yield break;
        }
        
        // Position and rotate the player to match the spawn point exactly
        Transform playerTransform = player.transform;
        Transform spawnTransform = spawnPoint.transform;
        
        playerTransform.position = spawnTransform.position;
        playerTransform.rotation = spawnTransform.rotation;
        
        Debug.Log($"SceneEffects: Player positioned at {spawnTransform.position} with rotation {spawnTransform.rotation.eulerAngles}");
        
        // If player has a CharacterController, disable and re-enable it to ensure position sticks
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false;
            yield return null;
            controller.enabled = true;
            Debug.Log("SceneEffects: CharacterController refreshed for positioning");
        }
        
        // If player has a Rigidbody, stop any momentum
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            Debug.Log("SceneEffects: Player movement reset");
        }
    }
    
    /// <summary>
    /// Handle the animation and dialogue sequence similar to HandleNewGameSequence() but for Home scene
    /// </summary>
    private System.Collections.IEnumerator HandleLayingSequence()
    {
        Debug.Log("SceneEffects: Starting laying sequence");
        
        // Wait longer for scene to fully initialize
        yield return new WaitForSeconds(0.2f);
        
        // Find the player and ensure proper positioning first
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("CharModel1");
        }
        
        if (player != null)
        {
            // Ensure player is at the correct spawn point with correct rotation
            GameObject spawnPoint = GameObject.Find("BedroomSpawnPoint");
            if (spawnPoint != null)
            {
                // Disable CharacterController if present to ensure positioning works
                CharacterController controller = player.GetComponent<CharacterController>();
                if (controller != null)
                {
                    controller.enabled = false;
                }
                
                // Set position and rotation to match spawn point exactly
                player.transform.position = spawnPoint.transform.position;
                player.transform.rotation = spawnPoint.transform.rotation;
                
                // Re-enable CharacterController
                if (controller != null)
                {
                    yield return new WaitForEndOfFrame();
                    controller.enabled = true;
                }
                
                Debug.Log($"SceneEffects: Player positioned at spawn point with rotation {spawnPoint.transform.rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("SceneEffects: BedroomSpawnPoint not found for positioning");
            }
        }
        
        // Wait another frame for positioning to settle
        yield return new WaitForEndOfFrame();
        
        // Now find and set the animation
        Animator playerAnimator = null;
        if (player != null)
        {
            playerAnimator = player.GetComponentInChildren<Animator>();
            if (playerAnimator == null)
            {
                playerAnimator = player.GetComponent<Animator>();
            }
        }
        
        if (playerAnimator != null)
        {
            Debug.Log("SceneEffects: Setting Laying animation");
            playerAnimator.SetTrigger("Laying");
            // Wait for animation to register
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            Debug.LogWarning("SceneEffects: Could not find player animator for laying sequence");
        }
        
        // Wait for scene to fully initialize and fade-in to complete
        yield return new WaitForSeconds(0.5f);
        
        // Find or create dialogue trigger for the sequence
        ProgrammaticDialogueTrigger dialogueTrigger = FindFirstObjectByType<ProgrammaticDialogueTrigger>();
        if (dialogueTrigger == null)
        {
            // Create one if it doesn't exist
            GameObject dialogueObj = new GameObject("LayingSequenceDialogueTrigger");
            dialogueTrigger = dialogueObj.AddComponent<ProgrammaticDialogueTrigger>();
            
            // Set player reference
            if (player != null)
            {
                dialogueTrigger.playerTransform = player.transform;
            }
        }
        
        // Start the dialogue after fade-in is complete
        if (dialogueTrigger != null)
        {
            string[] speakers = { 
                "Alice", 
                "Mom", 
                "Alice", 
                "Alice" 
            };
            
            string[] sentences = { 
                "I'm so tired... what a long day.",
                "Get up, you're going to be late!",
                "Huh? Mom?",
                "It worked... everything's gone back to normal!"
            };
            
            Debug.Log("SceneEffects: Starting laying sequence dialogue");
            
            // Find DialogueManager to subscribe to its completion event
            DialogueManager dialogueManager = FindFirstObjectByType<DialogueManager>();
            if (dialogueManager != null)
            {
                // Subscribe to the dialogue end event
                System.Action onDialogueComplete = null;
                onDialogueComplete = () =>
                {
                    // Unsubscribe to prevent memory leaks
                    dialogueManager.OnDialogueEnd -= onDialogueComplete;
                    
                    Debug.Log("SceneEffects: Dialogue completed, loading StartScreen");
                    
                    // Load StartScreen scene after dialogue completes
                    if (buildingEntry != null)
                    {
                        buildingEntry.LoadScene("StartScreen", "StartPoint");
                    }
                    else
                    {
                        Debug.LogError("SceneEffects: No building entry - cannot load StartScreen scene");
                    }
                };
                
                dialogueManager.OnDialogueEnd += onDialogueComplete;
            }
            else
            {
                Debug.LogError("SceneEffects: Could not find DialogueManager to subscribe to completion event");
            }
            
            // Start the dialogue
            dialogueTrigger.StartMultiLineDialogue(speakers, sentences);
        }
        else
        {
            Debug.LogWarning("SceneEffects: Could not find or create dialogue trigger for laying sequence");
        }
    }
    
    // ===============================
    // Weapon Manager Control Methods
    // ===============================
    
    private WeaponManager FindPlayerWeaponManager()
    {
        // Try multiple ways to find the player with WeaponManager
        GameObject player1 = GameObject.Find("CharModel1");
        if (player1 != null)
        {
            WeaponManager wm = player1.GetComponent<WeaponManager>();
            if (wm != null)
            {
                Debug.Log("SceneEffects: Found WeaponManager on Player1");
                return wm;
            }
        }
        
        // Try finding by Player tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            WeaponManager wm = player.GetComponent<WeaponManager>();
            if (wm != null)
            {
                Debug.Log("SceneEffects: Found WeaponManager on Player");
                return wm;
            }
        }
        
        // Try finding any WeaponManager in the scene
        WeaponManager weaponManager = FindFirstObjectByType<WeaponManager>();
        if (weaponManager != null)
        {
            Debug.Log("SceneEffects: Found WeaponManager in scene");
            return weaponManager;
        }
        
        Debug.LogError("SceneEffects: Could not find WeaponManager!");
        return null;
    }
    
    public void EnablePlayerWeapons()
    {
        WeaponManager weaponManager = FindPlayerWeaponManager();
        if (weaponManager != null)
        {
            weaponManager.EnableWeapons();
            Debug.Log("SceneEffects: Enabled player weapons");
        }
    }
    
    public void GivePlayerGun()
    {
        WeaponManager weaponManager = FindPlayerWeaponManager();
        if (weaponManager != null)
        {
            weaponManager.SetObtainedGun(true);
            Debug.Log("SceneEffects: Gave player the gun");
        }
    }
    
    public void GivePlayerRifle()
    {
        WeaponManager weaponManager = FindPlayerWeaponManager();
        if (weaponManager != null)
        {
            weaponManager.SetObtainedRifle(true);
            Debug.Log("SceneEffects: Gave player the rifle");
        }
    }
    
    public void BombTheGates()
    {
        StartCoroutine(BombTheGatesSequence());
    }
    
    private IEnumerator BombTheGatesSequence()
    {
        // Wait for initial delay
        yield return new WaitForSeconds(bombDelay);
        
        // Play particle effect
        if (explosionEffect != null)
        {
            explosionEffect.Play();
        }
        
        // Play explosion sound effect
        if (explosionSFX != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(explosionSFX, transform, 1f);
        }
        
        // Transform the 3 objects
        for (int i = 0; i < objectsToTransform.Length && i < 3; i++)
        {
            if (objectsToTransform[i] != null)
            {
                if (newPositions.Length > i)
                    objectsToTransform[i].position = newPositions[i];
                if (newRotations.Length > i)
                    objectsToTransform[i].rotation = Quaternion.Euler(newRotations[i]);
                if (newScales.Length > i)
                    objectsToTransform[i].localScale = newScales[i];
            }
        }
    }
}
