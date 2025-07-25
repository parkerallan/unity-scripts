using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Helper script to automatically set up Score UI elements
/// Attach this to a Canvas and it will create the necessary UI elements for the score system
/// </summary>
public class ScoreUISetup : MonoBehaviour
{
    [Header("UI Setup")]
    public bool autoSetupUI = true;
    public bool createScoreText = true;
    public bool createComboUI = true;
    public bool createPointsPopup = true;
    
    [Header("UI Positioning")]
    public Vector2 scorePosition = new Vector2(700, 200); // Top-left area
    public Vector2 comboPosition = new Vector2(700, 175); // Center-top area
    public Vector2 popupPosition = new Vector2(700, 140); // Center screen
    
    [Header("UI Styling")]
    public int scoreFontSize = 24;
    public int comboFontSize = 20;
    public int popupFontSize = 18;
    public Color scoreColor = Color.white;
    public Color comboColor = Color.yellow;
    public Color popupColor = Color.white;
    public TMP_FontAsset customFont; // Optional custom font for all text elements
    
    private void Start()
    {
        if (autoSetupUI)
        {
            SetupScoreUI();
        }
    }
    
    [ContextMenu("Setup Score UI")]
    public void SetupScoreUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("ScoreUISetup: No Canvas component found! Attach this script to a Canvas.");
            return;
        }
        
        // Find or create ScoreManager
        ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
            Debug.Log("Created ScoreManager automatically");
        }
        
        // Apply custom font to ScoreManager if provided
        if (customFont != null)
        {
            scoreManager.customFont = customFont;
        }
        
        // Create Score Text
        if (createScoreText && scoreManager.scoreText == null)
        {
            GameObject scoreTextObj = CreateTextElement("ScoreText", scorePosition, scoreFontSize, scoreColor);
            TextMeshProUGUI scoreText = scoreTextObj.GetComponent<TextMeshProUGUI>();
            scoreText.text = "Score: 0";
            scoreText.alignment = TextAlignmentOptions.TopLeft;
            scoreManager.scoreText = scoreText;
            Debug.Log("Created Score Text UI");
        }
        
        // Create Combo UI
        if (createComboUI && scoreManager.comboUI == null)
        {
            GameObject comboUIObj = new GameObject("ComboUI");
            comboUIObj.transform.SetParent(canvas.transform, false);
            
            RectTransform comboRect = comboUIObj.AddComponent<RectTransform>();
            comboRect.anchoredPosition = comboPosition;
            comboRect.sizeDelta = new Vector2(200, 50);
            
            GameObject comboTextObj = CreateTextElement("ComboText", Vector2.zero, comboFontSize, comboColor);
            comboTextObj.transform.SetParent(comboUIObj.transform, false);
            
            TextMeshProUGUI comboText = comboTextObj.GetComponent<TextMeshProUGUI>();
            comboText.text = "Combo x1";
            comboText.alignment = TextAlignmentOptions.Center;
            
            scoreManager.comboUI = comboUIObj;
            scoreManager.comboText = comboText;
            comboUIObj.SetActive(false); // Start hidden
            Debug.Log("Created Combo UI");
        }
        
        // Create Points Popup
        if (createPointsPopup && scoreManager.pointsPopupUI == null)
        {
            GameObject popupUIObj = new GameObject("PointsPopupUI");
            popupUIObj.transform.SetParent(canvas.transform, false);
            
            RectTransform popupRect = popupUIObj.AddComponent<RectTransform>();
            popupRect.anchoredPosition = popupPosition;
            popupRect.sizeDelta = new Vector2(300, 100);
            
            // Add CanvasGroup for fading
            popupUIObj.AddComponent<CanvasGroup>();
            
            GameObject popupTextObj = CreateTextElement("PointsPopupText", Vector2.zero, popupFontSize, popupColor);
            popupTextObj.transform.SetParent(popupUIObj.transform, false);
            
            TextMeshProUGUI popupText = popupTextObj.GetComponent<TextMeshProUGUI>();
            popupText.text = "+100";
            popupText.alignment = TextAlignmentOptions.Center;
            popupText.fontStyle = FontStyles.Bold;
            
            scoreManager.pointsPopupUI = popupUIObj;
            scoreManager.pointsPopupText = popupText;
            popupUIObj.SetActive(false); // Start hidden
            Debug.Log("Created Points Popup UI");
        }
        
        Debug.Log("Score UI setup complete!");
    }
    
    private GameObject CreateTextElement(string name, Vector2 position, int fontSize, Color color)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(transform, false);
        
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 50);
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = name;
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        // Apply custom font if provided
        if (customFont != null)
        {
            textComponent.font = customFont;
        }
        
        return textObj;
    }
    
    [ContextMenu("Test Score System")]
    public void TestScoreSystem()
    {
        ScoreManager scoreManager = FindFirstObjectByType<ScoreManager>();
        if (scoreManager != null)
        {
            // Simulate killing enemies with different point values
            scoreManager.AddScore(100); // First kill
            
            // Wait a bit then add more for combo testing
            StartCoroutine(TestComboSequence(scoreManager));
        }
        else
        {
            Debug.LogWarning("No ScoreManager found for testing!");
        }
    }
    
    private System.Collections.IEnumerator TestComboSequence(ScoreManager scoreManager)
    {
        yield return new UnityEngine.WaitForSeconds(1f);
        scoreManager.AddScore(150); // Second kill - should have combo
        
        yield return new UnityEngine.WaitForSeconds(1f);
        scoreManager.AddScore(200); // Third kill - higher combo
        
        yield return new UnityEngine.WaitForSeconds(1f);
        scoreManager.AddScore(100); // Fourth kill - even higher combo
    }
}
