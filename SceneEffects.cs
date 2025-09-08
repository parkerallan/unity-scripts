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
    public float teleportDelay = 2f;
    
    [Header("Script Control Settings")]
    public MonoBehaviour scriptToEnable;
    
    [Header("Block Removal Settings")]
    public GameObject blockToRemove;
    
    public void Teleport(string sceneName, string spawnPointName)
    {
        StartCoroutine(TeleportSequence(sceneName, spawnPointName));
    }
    
    private IEnumerator TeleportSequence(string sceneName, string spawnPointName)
    {
        Debug.Log($"SceneEffects: Starting teleport to {sceneName} at {spawnPointName}");
        
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
        yield return new WaitForSeconds(teleportDelay);
        
        // Teleport to specified scene and spawn point
        if (buildingEntry != null)
        {
            Debug.Log($"SceneEffects: Teleporting to scene {sceneName} at spawn point {spawnPointName}");
            buildingEntry.EnterBuilding(sceneName, spawnPointName);
        }
        else
        {
            Debug.LogError("SceneEffects: No building entry - cannot teleport");
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
        if (blockToRemove != null)
        {
            blockToRemove.SetActive(false);
            Debug.Log($"SceneEffects: Disabled GameObject {blockToRemove.name}");
        }
        else
        {
            Debug.LogWarning("SceneEffects: No GameObject assigned to remove!");
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
