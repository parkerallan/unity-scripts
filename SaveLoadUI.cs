using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class SaveLoadUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject notificationPanel; // Panel that contains the notification UI
    public TextMeshProUGUI notificationText; // Text component for showing messages
    public Image notificationBackground; // Background image for styling
    
    [Header("Notification Settings")]
    public float notificationDuration = 2f; // How long to show notifications
    public Color saveColor = Color.green; // Color for save notifications
    public Color loadColor = Color.blue; // Color for load notifications
    public Color errorColor = Color.red; // Color for error notifications
    public Color defaultColor = Color.white; // Default background color
    
    [Header("Animation Settings")]
    public bool useAnimation = true;
    public float fadeInTime = 0.3f;
    public float fadeOutTime = 0.3f;
    
    private Coroutine currentNotificationCoroutine;
    
    // Singleton pattern for easy access
    public static SaveLoadUI Instance { get; private set; }
    
    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Initially hide the notification panel
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show a save notification
    /// </summary>
    public void ShowSaveNotification(bool success = true)
    {
        string message = success ? "Game Saved" : "Save Failed";
        Color backgroundColor = success ? saveColor : errorColor;
        
        ShowNotification(message, backgroundColor);
    }
    
    /// <summary>
    /// Show a load notification
    /// </summary>
    public void ShowLoadNotification(bool success = true)
    {
        string message = success ? "Game Loaded" : "Load Failed";
        Color backgroundColor = success ? loadColor : errorColor;
        
        ShowNotification(message, backgroundColor);
    }
    
    /// <summary>
    /// Show a custom notification
    /// </summary>
    public void ShowNotification(string message, Color backgroundColor = default)
    {
        if (backgroundColor == default)
        {
            backgroundColor = defaultColor;
        }
        
        // Stop any existing notification
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
        }
        
        // Start new notification
        currentNotificationCoroutine = StartCoroutine(DisplayNotification(message, backgroundColor));
    }
    
    /// <summary>
    /// Coroutine to handle notification display with animation
    /// </summary>
    private IEnumerator DisplayNotification(string message, Color backgroundColor)
    {
        // Set up the notification
        if (notificationText != null)
        {
            notificationText.text = message;
        }
        
        if (notificationBackground != null)
        {
            notificationBackground.color = backgroundColor;
        }
        
        // Show the notification panel
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(true);
        }
        
        // Fade in animation
        if (useAnimation)
        {
            yield return StartCoroutine(FadeNotification(0f, 1f, fadeInTime));
        }
        
        // Wait for the notification duration
        yield return new WaitForSeconds(notificationDuration);
        
        // Fade out animation
        if (useAnimation)
        {
            yield return StartCoroutine(FadeNotification(1f, 0f, fadeOutTime));
        }
        
        // Hide the notification panel
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        currentNotificationCoroutine = null;
    }
    
    /// <summary>
    /// Handle fade animation for the notification
    /// </summary>
    private IEnumerator FadeNotification(float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        
        CanvasGroup canvasGroup = notificationPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && notificationPanel != null)
        {
            canvasGroup = notificationPanel.AddComponent<CanvasGroup>();
        }
        
        if (canvasGroup != null)
        {
            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime; // Use unscaled time for UI
                float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / duration);
                canvasGroup.alpha = alpha;
                yield return null;
            }
            
            canvasGroup.alpha = endAlpha;
        }
    }
    
    /// <summary>
    /// Show auto-save notification
    /// </summary>
    public void ShowAutoSaveNotification()
    {
        ShowNotification("Auto-Saved", Color.yellow);
    }
    
    /// <summary>
    /// Show no save file notification
    /// </summary>
    public void ShowNoSaveFileNotification()
    {
        ShowNotification("No Save File Found", errorColor);
    }
    
    /// <summary>
    /// Force hide any active notification
    /// </summary>
    public void HideNotification()
    {
        if (currentNotificationCoroutine != null)
        {
            StopCoroutine(currentNotificationCoroutine);
            currentNotificationCoroutine = null;
        }
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }
}
