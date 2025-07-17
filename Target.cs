using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Target : MonoBehaviour
{
    public float health = 50f;
    
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
        // Get all renderers in this object and its children
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original colors
        StoreOriginalColors();
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
        health -= amount;
        
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
        Debug.Log($"Target {gameObject.name} is dying...");
        
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
