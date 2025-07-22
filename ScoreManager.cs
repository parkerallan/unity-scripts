using UnityEngine;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    [Header("Score Settings")]
    public int currentScore = 0;
    public int basePointsPerKill = 100;
    
    [Header("Combo System")]
    public float comboTimeWindow = 3.0f; // Time window to maintain combo
    public int currentCombo = 0;
    public int maxCombo = 0;
    public float comboMultiplier = 1.5f; // Multiplier increases per combo level
    
    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI pointsPopupText; // For showing points earned
    public GameObject comboUI; // Container for combo display
    public GameObject pointsPopupUI; // Container for points popup
    
    [Header("Font Settings")]
    public TMP_FontAsset customFont; // Drag your custom font asset here
    
    [Header("Visual Effects")]
    public float popupDisplayTime = 2.0f;
    public float fadeOutTime = 1.0f;
    public Color normalPointsColor = Color.white;
    public Color comboPointsColor = Color.yellow;
    public Color highComboPointsColor = Color.red;
    
    // Private variables
    private float lastKillTime;
    private Coroutine comboTimerCoroutine;
    private Coroutine pointsPopupCoroutine;
    
    // Singleton instance
    private static ScoreManager _instance;
    public static ScoreManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ScoreManager>();
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
    }
    
    private void Start()
    {
        // Apply custom font to all text elements
        ApplyCustomFont();
        
        UpdateScoreDisplay();
        UpdateComboDisplay();
        
        // Hide combo UI initially
        if (comboUI != null)
        {
            comboUI.SetActive(false);
        }
        
        // Hide points popup initially
        if (pointsPopupUI != null)
        {
            pointsPopupUI.SetActive(false);
        }
        
        Debug.Log("ScoreManager initialized successfully");
    }
    
    private void ApplyCustomFont()
    {
        if (customFont == null) return;
        
        if (scoreText != null)
        {
            scoreText.font = customFont;
        }
        
        if (comboText != null)
        {
            comboText.font = customFont;
        }
        
        if (pointsPopupText != null)
        {
            pointsPopupText.font = customFont;
        }
        
        Debug.Log("Custom font applied to all score UI elements");
    }
    
    public void AddScore(int enemyPoints)
    {
        // Calculate combo multiplier
        float multiplier = 1.0f + (currentCombo * (comboMultiplier - 1.0f));
        int pointsEarned = Mathf.RoundToInt(enemyPoints * multiplier);
        
        // Add to total score
        currentScore += pointsEarned;
        
        // Increase combo
        currentCombo++;
        
        // Update max combo if necessary
        if (currentCombo > maxCombo)
        {
            maxCombo = currentCombo;
        }
        
        // Update last kill time
        lastKillTime = Time.time;
        
        // Start or restart combo timer
        if (comboTimerCoroutine != null)
        {
            StopCoroutine(comboTimerCoroutine);
        }
        comboTimerCoroutine = StartCoroutine(ComboTimer());
        
        // Update displays
        UpdateScoreDisplay();
        UpdateComboDisplay();
        ShowPointsPopup(pointsEarned, currentCombo > 1);
        
        Debug.Log($"Score added: {pointsEarned} points (x{multiplier:F1} multiplier), Total: {currentScore}, Combo: {currentCombo}");
    }
    
    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore:N0}";
        }
    }
    
    private void UpdateComboDisplay()
    {
        if (currentCombo > 1)
        {
            if (comboUI != null)
            {
                comboUI.SetActive(true);
            }
            
            if (comboText != null)
            {
                comboText.text = $"Combo x{currentCombo}";
                
                // Change color based on combo level
                if (currentCombo >= 10)
                {
                    comboText.color = highComboPointsColor;
                }
                else if (currentCombo >= 5)
                {
                    comboText.color = comboPointsColor;
                }
                else
                {
                    comboText.color = normalPointsColor;
                }
            }
        }
        else
        {
            if (comboUI != null)
            {
                comboUI.SetActive(false);
            }
        }
    }
    
    private void ShowPointsPopup(int points, bool isCombo)
    {
        if (pointsPopupUI != null && pointsPopupText != null)
        {
            // Stop any existing popup coroutine
            if (pointsPopupCoroutine != null)
            {
                StopCoroutine(pointsPopupCoroutine);
            }
            
            pointsPopupCoroutine = StartCoroutine(DisplayPointsPopup(points, isCombo));
        }
    }
    
    private IEnumerator DisplayPointsPopup(int points, bool isCombo)
    {
        // Show the popup
        pointsPopupUI.SetActive(true);
        
        // Set text and color
        string popupText = $"+{points:N0}";
        if (isCombo)
        {
            popupText += $" (x{currentCombo})";
            pointsPopupText.color = currentCombo >= 10 ? highComboPointsColor : comboPointsColor;
        }
        else
        {
            pointsPopupText.color = normalPointsColor;
        }
        
        pointsPopupText.text = popupText;
        
        // Make sure it's fully visible
        CanvasGroup popupCanvasGroup = pointsPopupUI.GetComponent<CanvasGroup>();
        if (popupCanvasGroup == null)
        {
            popupCanvasGroup = pointsPopupUI.AddComponent<CanvasGroup>();
        }
        popupCanvasGroup.alpha = 1.0f;
        
        // Wait for display time
        yield return new WaitForSeconds(popupDisplayTime);
        
        // Fade out
        float elapsedTime = 0f;
        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(1.0f, 0.0f, elapsedTime / fadeOutTime);
            popupCanvasGroup.alpha = alpha;
            yield return null;
        }
        
        // Hide the popup
        pointsPopupUI.SetActive(false);
        pointsPopupCoroutine = null;
    }
    
    private IEnumerator ComboTimer()
    {
        yield return new WaitForSeconds(comboTimeWindow);
        
        // Check if enough time has passed since last kill
        if (Time.time - lastKillTime >= comboTimeWindow)
        {
            Debug.Log($"Combo ended at {currentCombo} kills");
            currentCombo = 0;
            UpdateComboDisplay();
        }
        
        comboTimerCoroutine = null;
    }
    
    // Public methods for external access
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    public int GetCurrentCombo()
    {
        return currentCombo;
    }
    
    public int GetMaxCombo()
    {
        return maxCombo;
    }
    
    public void ResetScore()
    {
        currentScore = 0;
        currentCombo = 0;
        UpdateScoreDisplay();
        UpdateComboDisplay();
        
        if (pointsPopupUI != null)
        {
            pointsPopupUI.SetActive(false);
        }
        
        Debug.Log("Score reset");
    }
    
    // Method to manually add points (for testing or special events)
    public void AddBonusPoints(int bonusPoints)
    {
        currentScore += bonusPoints;
        UpdateScoreDisplay();
        ShowPointsPopup(bonusPoints, false);
        Debug.Log($"Bonus points added: {bonusPoints}");
    }
}
