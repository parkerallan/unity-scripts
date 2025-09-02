using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows LoadingCanvas with image and animation during scene transitions
/// </summary>
public class SceneTransitionOverlay : MonoBehaviour
{
    [Header("Loading UI Settings")]
    public float minimumShowTime = 1.5f; // Minimum time to show loading canvas
    
    [Header("Custom Loading UI")]
    public string loadingCanvasName = "LoadingCanvas"; // Name of canvas to find in scene
    public bool enableCustomLoadingUI = true; // Whether to look for custom loading UI
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Private components
    private GameObject _customLoadingCanvas;
    private GameObject _createdOverlayCanvas;
    
    // State tracking
    private bool _isShowing = false;
    private float _showStartTime;
    private Coroutine _hideCoroutine;
    private bool _hideRequested = false;
    private bool _minimumTimeElapsed = false;
    
    // Singleton instance for easy access
    private static SceneTransitionOverlay _instance;
    public static SceneTransitionOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.Log("SceneTransitionOverlay: Instance is null, attempting to find or create...");
                
                // Try to find existing instance first
                _instance = FindFirstObjectByType<SceneTransitionOverlay>();
                if (_instance == null)
                {
                    Debug.Log("SceneTransitionOverlay: No existing instance found, creating new one...");
                    
                    // Create new instance only if none exists
                    GameObject overlayObj = new GameObject("SceneTransitionOverlay");
                    _instance = overlayObj.AddComponent<SceneTransitionOverlay>();
                    DontDestroyOnLoad(overlayObj);
                    
                    Debug.Log($"SceneTransitionOverlay: Created new instance - GameObject: {overlayObj.name}");
                }
                else
                {
                    Debug.Log($"SceneTransitionOverlay: Found existing instance - GameObject: {_instance.gameObject.name}");
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        // Ensure singleton
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (enableDebugLogs)
            Debug.Log("SceneTransitionOverlay: Singleton instance created");
    }
    
    private void Start()
    {
        // Ensure the overlay persists across scene changes
        if (_instance == this)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Clear singleton reference if this is the instance being destroyed
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    /// <summary>
    /// Create or find loading canvas that persists across scenes
    /// </summary>
    private void CreateOrFindLoadingCanvas()
    {
        Debug.Log("SceneTransitionOverlay: CreateOrFindLoadingCanvas called");
        
        // First try to find existing custom loading canvas in current scene
        if (enableCustomLoadingUI)
        {
            Debug.Log("SceneTransitionOverlay: Looking for custom loading UI...");
            FindAndActivateCustomLoadingUI();
            if (_customLoadingCanvas != null)
            {
                Debug.Log("SceneTransitionOverlay: Using custom loading canvas");
                return; // Found existing canvas, we're done
            }
            else
            {
                Debug.Log("SceneTransitionOverlay: No custom loading canvas found");
            }
        }
        
        // If no custom canvas found, create a simple overlay that persists
        if (_createdOverlayCanvas == null)
        {
            Debug.Log("SceneTransitionOverlay: Creating new persistent overlay...");
            CreatePersistentOverlay();
        }
        else
        {
            // Reactivate existing persistent overlay
            _createdOverlayCanvas.SetActive(true);
            Debug.Log("SceneTransitionOverlay: Reactivated existing persistent overlay canvas");
        }
    }
    
    /// <summary>
    /// Create a simple persistent loading overlay
    /// </summary>
    private void CreatePersistentOverlay()
    {
        Debug.Log("SceneTransitionOverlay: CreatePersistentOverlay starting...");
            
        // Create main canvas GameObject
        _createdOverlayCanvas = new GameObject("PersistentLoadingCanvas");
        DontDestroyOnLoad(_createdOverlayCanvas);
        
        Debug.Log($"SceneTransitionOverlay: Created canvas GameObject: {_createdOverlayCanvas.name}");
        
        // Add and configure Canvas component
        Canvas canvas = _createdOverlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        Debug.Log($"SceneTransitionOverlay: Canvas component added with sorting order: {canvas.sortingOrder}");
        
        // Add CanvasScaler for proper UI scaling
        CanvasScaler scaler = _createdOverlayCanvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Add GraphicRaycaster for UI interactions
        _createdOverlayCanvas.AddComponent<GraphicRaycaster>();
        
        // Create background image (semi-transparent black)
        GameObject backgroundObj = new GameObject("Background");
        backgroundObj.transform.SetParent(_createdOverlayCanvas.transform, false);
        
        Image backgroundImage = backgroundObj.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        
        // Stretch background to fill screen
        RectTransform backgroundRect = backgroundImage.rectTransform;
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        backgroundRect.anchoredPosition = Vector2.zero;
        
        Debug.Log("SceneTransitionOverlay: Background image created and configured");
        
        // Create loading text
        GameObject loadingTextObj = new GameObject("LoadingText");
        loadingTextObj.transform.SetParent(_createdOverlayCanvas.transform, false);
        
        Text loadingText = loadingTextObj.AddComponent<Text>();
        loadingText.text = "Loading...";
        
        // Try to load the legacy runtime font, with fallback
        Font loadingFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (loadingFont == null)
        {
            // Fallback to default font if LegacyRuntime.ttf fails
            loadingFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Debug.LogWarning("SceneTransitionOverlay: LegacyRuntime.ttf not found, falling back to Arial.ttf");
        }
        if (loadingFont == null)
        {
            // Last resort fallback
            loadingFont = Resources.FindObjectsOfTypeAll<Font>()[0];
            Debug.LogWarning("SceneTransitionOverlay: Using fallback font: " + (loadingFont ? loadingFont.name : "null"));
        }
        
        loadingText.font = loadingFont;
        loadingText.fontSize = 48;
        loadingText.color = Color.white;
        loadingText.alignment = TextAnchor.MiddleCenter;
        
        // Position loading text in center
        RectTransform textRect = loadingText.rectTransform;
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(400, 100);
        
        Debug.Log("SceneTransitionOverlay: Loading text created and positioned");
        
        // Add simple animation to loading text
        Animator textAnimator = loadingTextObj.AddComponent<Animator>();
        textAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        
        // Create a simple fade animation using a coroutine instead of animator
        StartCoroutine(AnimateLoadingText(loadingText));
        
        _createdOverlayCanvas.SetActive(true);
        
        Debug.Log($"SceneTransitionOverlay: Persistent overlay created and activated. Canvas active: {_createdOverlayCanvas.activeSelf}");
    }
    
    /// <summary>
    /// Simple text animation coroutine
    /// </summary>
    private System.Collections.IEnumerator AnimateLoadingText(Text loadingText)
    {
        if (loadingText == null) yield break;
        
        float time = 0f;
        Color originalColor = loadingText.color;
        
        while (_isShowing && loadingText != null)
        {
            time += Time.unscaledDeltaTime;
            float alpha = (Mathf.Sin(time * 2f) + 1f) * 0.5f; // Pulse between 0 and 1
            alpha = Mathf.Lerp(0.3f, 1f, alpha); // Keep it visible, just pulse
            
            Color newColor = originalColor;
            newColor.a = alpha;
            loadingText.color = newColor;
            
            yield return null;
        }
        
        // Restore original color when done
        if (loadingText != null)
        {
            loadingText.color = originalColor;
        }
    }
    /// <summary>
    /// Find and activate custom loading canvas if it exists
    /// </summary>
    private void FindAndActivateCustomLoadingUI()
    {
        if (!enableCustomLoadingUI)
            return;
        
        if (enableDebugLogs)
            Debug.Log($"SceneTransitionOverlay: Searching for custom loading canvas named: {loadingCanvasName}");
            
        // Try to find the custom loading canvas by name
        GameObject loadingCanvasObj = GameObject.Find(loadingCanvasName);
        
        // If not found with GameObject.Find, try searching through all canvases
        if (loadingCanvasObj == null)
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (enableDebugLogs)
                Debug.Log($"SceneTransitionOverlay: Found {allCanvases.Length} total canvases in scene");
            
            foreach (Canvas canvas in allCanvases)
            {
                if (enableDebugLogs)
                    Debug.Log($"SceneTransitionOverlay: Checking canvas: {canvas.name} (active: {canvas.gameObject.activeInHierarchy})");
                
                if (canvas.name == loadingCanvasName)
                {
                    loadingCanvasObj = canvas.gameObject;
                    if (enableDebugLogs)
                        Debug.Log($"SceneTransitionOverlay: Found matching canvas: {canvas.name}");
                    break;
                }
            }
        }
        
        if (loadingCanvasObj != null)
        {
            _customLoadingCanvas = loadingCanvasObj;
            
            // Make sure it has a Canvas component
            Canvas loadingCanvas = _customLoadingCanvas.GetComponent<Canvas>();
            if (loadingCanvas != null)
            {
                // Set it to render on top of everything
                loadingCanvas.sortingOrder = 1001;
                loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                // Make it persist across scene changes
                DontDestroyOnLoad(_customLoadingCanvas);
                
                // Activate the canvas first
                _customLoadingCanvas.SetActive(true);
                
                // Force canvas to update immediately
                Canvas.ForceUpdateCanvases();
                
                // Wait a frame then trigger animations
                StartCoroutine(TriggerAnimationsNextFrame());
                
                if (enableDebugLogs)
                    Debug.Log($"SceneTransitionOverlay: Found and activated custom loading canvas: {loadingCanvasName}");
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"SceneTransitionOverlay: Found {loadingCanvasName} but it doesn't have a Canvas component");
                _customLoadingCanvas = null;
            }
        }
        else
        {
            if (enableDebugLogs)
                Debug.LogWarning($"SceneTransitionOverlay: No custom loading canvas found with name: {loadingCanvasName}");
        }
    }
    
    /// <summary>
    /// Trigger animations on the next frame to ensure canvas is ready
    /// </summary>
    private System.Collections.IEnumerator TriggerAnimationsNextFrame()
    {
        yield return null; // Wait one frame
        
        if (_customLoadingCanvas == null) yield break;
        
        // Trigger the "Loading" animation on all animators
        Animator[] animators = _customLoadingCanvas.GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
        {
            if (animator != null)
            {
                // Set the animator to use unscaled time to work during scene loading
                animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                
                // Enable the gameobject first
                animator.gameObject.SetActive(true);
                
                // Wait another frame to ensure animator is ready
                yield return null;
                
                // Trigger the animation
                if (animator.runtimeAnimatorController != null)
                {
                    animator.SetTrigger("Loading");
                    if (enableDebugLogs)
                        Debug.Log($"SceneTransitionOverlay: Triggered 'Loading' animation on {animator.name} with unscaled time");
                }
            }
        }
        
        // Also check for Image components named "Image" specifically
        Image[] images = _customLoadingCanvas.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            if (img.name == "Image" || img.name.ToLower().Contains("loading"))
            {
                img.gameObject.SetActive(true);
                Animator imgAnimator = img.GetComponent<Animator>();
                if (imgAnimator != null && imgAnimator.runtimeAnimatorController != null)
                {
                    imgAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    imgAnimator.SetTrigger("Loading");
                    if (enableDebugLogs)
                        Debug.Log($"SceneTransitionOverlay: Found and triggered animation on Image component: {img.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// Deactivate custom loading canvas
    /// </summary>
    private void DeactivateCustomLoadingUI()
    {
        if (_customLoadingCanvas != null)
        {
            _customLoadingCanvas.SetActive(false);
            if (enableDebugLogs)
                Debug.Log("SceneTransitionOverlay: Deactivated custom loading canvas");
        }
        
        if (_createdOverlayCanvas != null)
        {
            _createdOverlayCanvas.SetActive(false);
            if (enableDebugLogs)
                Debug.Log("SceneTransitionOverlay: Deactivated created overlay canvas");
        }
    }
    
    /// <summary>
    /// Show the loading canvas with image and animation
    /// </summary>
    public void ShowOverlay()
    {
        Debug.Log("SceneTransitionOverlay: ShowOverlay called");
        Debug.Log($"SceneTransitionOverlay: Current state - _isShowing: {_isShowing}, GameObject: {gameObject.name}");
            
        _isShowing = true;
        _showStartTime = Time.unscaledTime; // Use unscaled time for accurate tracking during scene loading
        _hideRequested = false;
        _minimumTimeElapsed = false;
        
        // Stop any existing hide coroutine
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            Debug.Log("SceneTransitionOverlay: Stopped existing hide coroutine");
        }
        
        // Create or activate the LoadingCanvas BEFORE scene transition
        Debug.Log("SceneTransitionOverlay: Calling CreateOrFindLoadingCanvas...");
        CreateOrFindLoadingCanvas();
        
        // Start minimum time tracker using unscaled time
        _hideCoroutine = StartCoroutine(MinimumTimeTracker());
        
        Debug.Log($"SceneTransitionOverlay: LoadingCanvas shown - will hide after minimum {minimumShowTime}s AND scene load completes");
        Debug.Log($"SceneTransitionOverlay: Custom canvas active: {(_customLoadingCanvas != null && _customLoadingCanvas.activeSelf)}");
        Debug.Log($"SceneTransitionOverlay: Created canvas active: {(_createdOverlayCanvas != null && _createdOverlayCanvas.activeSelf)}");
    }
    
    private System.Collections.IEnumerator MinimumTimeTracker()
    {
        float startTime = Time.unscaledTime;
        
        if (enableDebugLogs)
            Debug.Log($"SceneTransitionOverlay: Starting minimum time tracker for {minimumShowTime}s at unscaled time {startTime:F2}");
        
        yield return new WaitForSecondsRealtime(minimumShowTime);
        
        float endTime = Time.unscaledTime;
        float actualWaitTime = endTime - startTime;
        
        _minimumTimeElapsed = true;
        
        if (enableDebugLogs)
            Debug.Log($"SceneTransitionOverlay: Minimum time ({minimumShowTime}s) elapsed - actual wait: {actualWaitTime:F2}s");
        
        // Check if we can hide now (both conditions met)
        CheckAndHideIfReady();
        _hideCoroutine = null;
    }
    
    private void CheckAndHideIfReady()
    {
        if (_minimumTimeElapsed && _hideRequested)
        {
            if (enableDebugLogs)
                Debug.Log("SceneTransitionOverlay: Both conditions met - hiding LoadingCanvas now");
            DoHideOverlay();
        }
        else if (enableDebugLogs)
        {
            string waiting = !_minimumTimeElapsed ? "minimum time" : "";
            waiting += (!_minimumTimeElapsed && !_hideRequested) ? " and " : "";
            waiting += !_hideRequested ? "scene load completion" : "";
            Debug.Log($"SceneTransitionOverlay: Still waiting for: {waiting}");
        }
    }
    
    /// <summary>
    /// Hide the loading canvas - waits for minimum time to elapse
    /// </summary>
    public void HideOverlay()
    {
        _hideRequested = true;
        
        if (enableDebugLogs)
        {
            float elapsedTime = Time.unscaledTime - _showStartTime;
            Debug.Log($"SceneTransitionOverlay: Hide requested after {elapsedTime:F2}s - scene loading complete");
        }
        
        // Check if we can hide now (both conditions met)
        CheckAndHideIfReady();
    }
    
    private void DoHideOverlay()
    {
        // Reset state
        _hideRequested = false;
        _minimumTimeElapsed = false;
        
        // Only deactivate the LoadingCanvas
        DeactivateCustomLoadingUI();
        
        _isShowing = false;
        
        if (enableDebugLogs)
        {
            float totalTime = Time.unscaledTime - _showStartTime;
            Debug.Log($"SceneTransitionOverlay: LoadingCanvas hidden after {totalTime:F2}s total");
        }
    }
    
    /// <summary>
    /// Force hide the loading canvas immediately
    /// </summary>
    public void ForceHideOverlay()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        DoHideOverlay();
        
        if (enableDebugLogs)
            Debug.Log("SceneTransitionOverlay: LoadingCanvas force hidden");
    }
    
    /// <summary>
    /// Completely destroy - just deactivates loading canvas
    /// </summary>
    public void DestroyOverlay()
    {
        if (_hideCoroutine != null)
        {
            StopCoroutine(_hideCoroutine);
            _hideCoroutine = null;
        }
        
        DeactivateCustomLoadingUI();
        _isShowing = false;
        
        if (enableDebugLogs)
            Debug.Log("SceneTransitionOverlay: LoadingCanvas destroyed/deactivated");
    }
    
    /// <summary>
    /// Check if the loading canvas is blocking UI interactions - always returns false
    /// </summary>
    public bool IsBlockingUI()
    {
        // LoadingCanvas should never block UI interactions
        return false;
    }
    
    /// <summary>
    /// Check if loading canvas is currently showing
    /// </summary>
    public bool IsShowing
    {
        get { return _isShowing; }
    }
}
