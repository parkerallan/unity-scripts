using UnityEngine;
using System.Collections;

public class Target : MonoBehaviour
{
    public float health = 50f;
    
    [Header("Hit Effect")]
    public float flashDuration = 0.15f;  // How long the red flash lasts
    public Color flashColor = Color.red;  // The color to flash
    
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
        // Handle death logic here
        Destroy(gameObject);
    }
}
