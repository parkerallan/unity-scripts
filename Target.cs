using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class Target : MonoBehaviour
{
    public float health = 50f;
    private float maxHealth; // Store the original max health
    
    [Header("Target Info")]
    public string targetName = "Enemy"; // Name to display in health bar
    public bool isPlayer = false; // Set to true if this is the player
    public bool isEnemy = true; // Set to true if this gives points when killed
    public int pointValue = 100; // Points awarded when this target is killed
    
    // Private variables
    private bool isDead = false; // Prevent multiple scoring from same enemy
    
    [Header("UI Health Bar")]
    public GameObject healthBarUI; // Reference to the health bar UI GameObject
    public Slider healthBarSlider; // Reference to the health bar slider
    public Image healthBarFill; // Reference to the slider's fill image for gradient coloring
    public TMPro.TextMeshProUGUI targetNameText; // Reference to the target name text
    public float healthBarDisplayTime = 3f; // How long to show health bar after taking damage
    private Coroutine hideHealthBarCoroutine;
    
    [Header("Hit Effect")]
    public float flashDuration = 0.15f;  // How long the red flash lasts
    public Color flashColor = Color.red;  // The color to flash
    
    [Header("Death Effects")]
    public bool deathAnimation = false;  // Play death animation from character controller
    public bool vaporize = false;        // Play vaporize particle effect
    public bool explode = false;         // Play explode particle effect
    public bool bloodSpurt = false;      // Play blood spurt particle effect
    
    [Header("Death Effect References")]
    public ParticleSystem vaporizeEffect;
    public ParticleSystem explodeEffect;
    public ParticleSystem bloodSpurtEffect;
    public UnityEvent OnDeathAnimation;  // Event for death animation - drag script and function here
    public float effectDelay = 2.0f;     // Delay before destroying object after effects
    
    private Renderer[] renderers;
    private Color[] originalColors;
    private bool isFlashing = false;
    
    void Start()
    {
        // Store the max health for health bar calculations
        maxHealth = health;
        
        // Get all renderers in this object and its children
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original colors
        StoreOriginalColors();
        
        // Find and assign health bar UI components
        RefreshHealthBarReferences();
        
        // Initialize target name display
        UpdateTargetNameDisplay();
    }
    
    // Method to refresh health bar UI references - called when returning to scenes
    public void RefreshHealthBarReferences()
    {
        // Try to find health bar UI components if they're not assigned
        if (healthBarUI == null)
        {
            healthBarUI = GameObject.Find("EnemyHealthBarUI");
            if (healthBarUI == null)
            {
                healthBarUI = GameObject.Find("HealthBarUI");
            }
        }
        
        if (healthBarSlider == null)
        {
            GameObject healthBarObj = GameObject.Find("EnemyHealthBar");
            if (healthBarObj == null)
            {
                healthBarObj = GameObject.Find("HealthBar");
            }
            
            if (healthBarObj != null)
            {
                healthBarSlider = healthBarObj.GetComponent<Slider>();
            }
        }
        
        if (healthBarFill == null && healthBarSlider != null)
        {
            // Try to find the fill image in the slider
            healthBarFill = healthBarSlider.fillRect?.GetComponent<Image>();
        }
        
        if (targetNameText == null)
        {
            // Try to find target name text - could be child of health bar UI
            if (healthBarUI != null)
            {
                targetNameText = healthBarUI.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
            
            // If still not found, try by name
            if (targetNameText == null)
            {
                GameObject nameTextObj = GameObject.Find("TargetNameText");
                if (nameTextObj != null)
                {
                    targetNameText = nameTextObj.GetComponent<TMPro.TextMeshProUGUI>();
                }
            }
        }
        
        // Initially hide the health bar for enemies
        if (!isPlayer && healthBarUI != null)
        {
            healthBarUI.SetActive(false);
        }
        
        // Log what we found for debugging
        Debug.Log($"Target {gameObject.name}: Health bar references - " +
                  $"UI: {(healthBarUI != null ? "Found" : "Missing")}, " +
                  $"Slider: {(healthBarSlider != null ? "Found" : "Missing")}, " +
                  $"Fill: {(healthBarFill != null ? "Found" : "Missing")}, " +
                  $"NameText: {(targetNameText != null ? "Found" : "Missing")}");
    }
    
    void StoreOriginalColors()
    {
        originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            // We're assuming the material uses the _Color property
            // If your shader uses a different property, adjust accordingly
            if (renderers[i].material.HasProperty("_Color"))
            {
                originalColors[i] = renderers[i].material.color;
            }
        }
    }
    
    public void TakeDamage(float amount)
    {
        // Don't take damage if already dead
        if (isDead) return;
        
        health -= amount;
        
        // Show and update health bar
        if (isPlayer)
        {
            // For player, just update the health bar without showing/hiding logic
            UpdateHealthBar();
        }
        else
        {
            // For enemies, show health bar with timer
            ShowHealthBar();
            UpdateHealthBar();
        }
        
        // Flash red when taking damage
        if (!isFlashing)
        {
            StartCoroutine(FlashEffect());
        }
        
        if (health <= 0f)
        {
            Die();
        }
    }
    
    void ShowHealthBar()
    {
        // Only show/hide logic for enemies, not players
        if (!isPlayer && healthBarUI != null)
        {
            healthBarUI.SetActive(true);
            
            // Ensure slider is enabled
            if (healthBarSlider != null)
            {
                healthBarSlider.gameObject.SetActive(true);
            }
            
            // Cancel any existing hide coroutine
            if (hideHealthBarCoroutine != null)
            {
                StopCoroutine(hideHealthBarCoroutine);
            }
            
            // Start new hide coroutine
            hideHealthBarCoroutine = StartCoroutine(HideHealthBarAfterDelay());
        }
    }
    
    void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            // Calculate health percentage (0 to 1)
            float healthPercentage = Mathf.Clamp01(health / maxHealth);
            healthBarSlider.value = healthPercentage;
            
            // Update gradient color based on health percentage
            UpdateHealthBarColor(healthPercentage);
        }
    }
    
    void UpdateHealthBarColor(float healthPercentage)
    {
        if (healthBarFill != null)
        {
            Color healthColor;
            
            if (healthPercentage <= 0.5f)
            {
                // Interpolate between Red (0.0) and Yellow (0.5)
                float t = healthPercentage / 0.5f; // Normalize to 0-1 range
                healthColor = Color.Lerp(Color.red, Color.yellow, t);
            }
            else
            {
                // Interpolate between Yellow (0.5) and Green (1.0)
                float t = (healthPercentage - 0.5f) / 0.5f; // Normalize to 0-1 range
                healthColor = Color.Lerp(Color.yellow, Color.green, t);
            }
            
            healthBarFill.color = healthColor;
        }
    }
    
    void UpdateTargetNameDisplay()
    {
        if (targetNameText != null)
        {
            targetNameText.text = targetName;
        }
    }
    
    IEnumerator HideHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(healthBarDisplayTime);
        
        if (healthBarUI != null)
        {
            healthBarUI.SetActive(false);
        }
        
        if (healthBarSlider != null)
        {
            healthBarSlider.gameObject.SetActive(false);
        }
    }
    
    IEnumerator FlashEffect()
    {
        isFlashing = true;
        
        // Change to flash color
        SetColor(flashColor);
        
        // Wait for flash duration
        yield return new WaitForSeconds(flashDuration);
        
        // Revert to original colors
        ResetColors();
        
        isFlashing = false;
    }
    
    void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                renderers[i].material.color = color;
            }
        }
    }
    
    void ResetColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_Color"))
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }
    
    void Die()
    {
        // Prevent multiple calls to Die()
        if (isDead) return;
        isDead = true;
        
        Debug.Log($"Target {gameObject.name} is dying...");
        
        // Award points if this is an enemy (only once)
        if (isEnemy && !isPlayer && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(pointValue);
            Debug.Log($"Awarded {pointValue} points for killing {targetName}");
        }
        
        // Hide health bar when dying (only for enemies)
        if (!isPlayer)
        {
            if (healthBarUI != null)
            {
                healthBarUI.SetActive(false);
            }
            
            if (healthBarSlider != null)
            {
                healthBarSlider.gameObject.SetActive(false);
            }
        }
        
        // Play death animation if enabled
        if (deathAnimation)
        {
            PlayDeathAnimation();
        }
        
        // Play particle effects based on flags
        if (vaporize && vaporizeEffect != null)
        {
            PlayVaporizeEffect();
        }
        
        if (explode && explodeEffect != null)
        {
            PlayExplodeEffect();
        }
        
        if (bloodSpurt && bloodSpurtEffect != null)
        {
            PlayBloodSpurtEffect();
        }
        
        // Destroy the object after a delay to allow effects to play
        StartCoroutine(DestroyAfterDelay());
    }
    
    void PlayDeathAnimation()
    {
        // Invoke the death animation event - this can call any function from any script
        if (OnDeathAnimation != null)
        {
            Debug.Log("Invoking death animation event");
            OnDeathAnimation.Invoke();
        }
        else
        {
            Debug.LogWarning("No death animation event configured");
        }
    }
    
    void PlayVaporizeEffect()
    {
        Debug.Log("Playing vaporize effect");
        vaporizeEffect.transform.position = transform.position;
        vaporizeEffect.Play();
    }
    
    void PlayExplodeEffect()
    {
        Debug.Log("Playing explode effect");
        explodeEffect.transform.position = transform.position;
        explodeEffect.Play();
    }
    
    void PlayBloodSpurtEffect()
    {
        Debug.Log("Playing blood spurt effect");
        bloodSpurtEffect.transform.position = transform.position;
        bloodSpurtEffect.Play();
    }
    
    IEnumerator DestroyAfterDelay()
    {
        // Wait for effects to play
        yield return new WaitForSeconds(effectDelay);
        
        Debug.Log($"Destroying target {gameObject.name}");
        Destroy(gameObject);
    }
}
