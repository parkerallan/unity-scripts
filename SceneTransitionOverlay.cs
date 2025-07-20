using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple screen overlay during scene transitions - just shows a solid color
/// </summary>
public class SceneTransitionOverlay : MonoBehaviour
{
    [Header("Overlay Settings")]
    public Color overlayColor = new Color(0.443f, 0.871f, 0.765f, 0f); // Transparent - no color overlay
    public float minimumShowTime = 2.0f; // Minimum time to show overlay (increased for loading canvas)
    
    [Header("Custom Loading UI")]
    public string loadingCanvasName = "LoadingCanvas"; // Name of canvas to find in scene
    public bool enableCustomLoadingUI = true; // Whether to look for custom loading UI
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    // Private components
    private Canvas _overlayCanvas;
    private Image _overlayImage;
    private GameObject _customLoadingCanvas;
    
    // State tracking
    private bool _isShowing = false;
    private float _showStartTime;
    
    // Singleton instance for easy access
    private static SceneTransitionOverlay _instance;
    public static SceneTransitionOverlay Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find existing instance first
                _instance = FindFirstObjectByType<SceneTransitionOverlay>();
                if (_instance == null)
                {
                    // Create new instance only if none exists
                    GameObject overlayObj = new GameObject("SceneTransitionOverlay");
                    _instance = overlayObj.AddComponent<SceneTransitionOverlay>();
                    DontDestroyOnLoad(overlayObj);
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
        
        CreateOverlayUI();
    }
    
    private void OnDestroy()
    {
        // Clear singleton reference if this is the instance being destroyed
        if (_instance == this)
        {
            _instance = null;
        }
    }
    
    private void CreateOverlayUI()
    {
        // Create canvas
        GameObject canvasObj = new GameObject("SimpleOverlayCanvas");
        canvasObj.transform.SetParent(transform);
        
        _overlayCanvas = canvasObj.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.sortingOrder = 1000; // Ensure it's on top
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create overlay image
        GameObject imageObj = new GameObject("OverlayImage");
        imageObj.transform.SetParent(canvasObj.transform);
        
        _overlayImage = imageObj.AddComponent<Image>();
        _overlayImage.color = overlayColor;
        
        // Set up full screen rect
        RectTransform imageRect = _overlayImage.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.sizeDelta = Vector2.zero;
        imageRect.anchoredPosition = Vector2.zero;
        
        // Start with overlay hidden
        _overlayCanvas.gameObject.SetActive(false);
        
        if (enableDebugLogs)
            Debug.Log("SceneTransitionOverlay: Simple UI created successfully");
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
                // Set it to render on top of our overlay
                loadingCanvas.sortingOrder = 1001; // Higher than our overlay (1000)
                _customLoadingCanvas.SetActive(true);
                
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
    }
    
    /// <summary>
    /// Show the overlay immediately
    /// </summary>
    public void ShowOverlay()
    {
        if (_isShowing)
        {
            if (enableDebugLogs)
                Debug.LogWarning("SceneTransitionOverlay: Overlay already showing");
            return;
        }
        
        if (_overlayCanvas == null)
        {
            CreateOverlayUI();
        }
        
        _isShowing = true;
        _showStartTime = Time.time;
        _overlayCanvas.gameObject.SetActive(true);
        
        // Try to find and activate custom loading UI
        FindAndActivateCustomLoadingUI();
        
        if (enableDebugLogs)
            Debug.Log("SceneTransitionOverlay: Overlay shown");
    }
    
    /// <summary>
    /// Hide the overlay (respects minimum show time)
    /// </summary>
    public void HideOverlay()
    {
        if (!_isShowing)
        {
            if (enableDebugLogs)
                Debug.LogWarning("SceneTransitionOverlay: No overlay to hide");
            return;
        }
        
        float elapsedTime = Time.time - _showStartTime;
        
        if (elapsedTime < minimumShowTime)
        {
            float remainingTime = minimumShowTime - elapsedTime;
            if (enableDebugLogs)
                Debug.Log($"SceneTransitionOverlay: Waiting {remainingTime:F2}s more for minimum show time");
            
            Invoke(nameof(DoHideOverlay), remainingTime);
        }
        else
        {
            DoHideOverlay();
        }
    }
    
    private void DoHideOverlay()
    {
        if (_overlayCanvas != null)
        {
            _overlayCanvas.gameObject.SetActive(false);
        }
        
        // Deactivate custom loading UI
        DeactivateCustomLoadingUI();
        
        _isShowing = false;
        
        if (enableDebugLogs)
            Debug.Log("SceneTransitionOverlay: Overlay hidden");
    }
    
    /// <summary>
    /// Force hide the overlay immediately
    /// </summary>
    public void ForceHideOverlay()
    {
        CancelInvoke(nameof(DoHideOverlay));
        DoHideOverlay();
    }
    
    /// <summary>
    /// Check if overlay is currently showing
    /// </summary>
    public bool IsShowing
    {
        get { return _isShowing; }
    }
}
