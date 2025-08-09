using UnityEngine;

public class HealthContainer : MonoBehaviour
{
    [Header("Health Container Settings")]
    public float healthAmount = 25f; // Amount of health to give
    public bool isFullHeal = false; // Check this to fully restore player health
    
    [Header("Effects")]
    public GameObject pickupEffect; // Particle effect to play when picked up
    public AudioClip pickupSound; // Sound to play when picked up
    
    [Header("Visual Feedback")]
    public GameObject containerModel; // The visual model of the health container
    public float bobSpeed = 2f; // Speed of the bobbing animation
    public float bobHeight = 0.3f; // Height of the bobbing animation
    public float rotationSpeed = 30f; // Speed of rotation animation
    public Color glowColor = Color.green; // Color for health glow effect
    
    [Header("Glow Effect")]
    public Light glowLight; // Optional light component for glow effect
    public float glowIntensity = 2f; // Base intensity of the glow
    public float pulseSpeed = 3f; // Speed of glow pulsing
    
    private Vector3 startPosition;
    private bool hasBeenPickedUp = false;
    private float baseLightIntensity;
    
    void Start()
    {
        // Store the starting position for bobbing animation
        startPosition = transform.position;
        
        // Store base light intensity if glow light is assigned
        if (glowLight != null)
        {
            baseLightIntensity = glowLight.intensity;
            glowLight.color = glowColor;
        }
        
        // Ensure we have a trigger collider for pickup behavior
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            // Add a sphere collider if none exists
            SphereCollider sphereCol = gameObject.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 1.5f;
        }
        
        Debug.Log($"Health Container initialized: {(isFullHeal ? "Full Heal" : $"+{healthAmount} HP")}");
    }
    
    void Update()
    {
        // Don't animate if already picked up
        if (hasBeenPickedUp) return;
        
        // Bobbing animation - only move the visual model up and down
        if (containerModel != null)
        {
            // Ensure bobbing starts above ground level and only goes up
            float bobOffset = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobHeight;
            float newY = startPosition.y + 0.5f + bobOffset; // Always at least 0.5 units above start position
            containerModel.transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Rotation animation - rotate only around the Y axis (vertical)
            containerModel.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
        }
        
        // Pulsing glow effect
        if (glowLight != null)
        {
            float pulseMultiplier = 1f + (Mathf.Sin(Time.time * pulseSpeed) * 0.5f);
            glowLight.intensity = baseLightIntensity * glowIntensity * pulseMultiplier;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if player entered the trigger
        if (other.CompareTag("Player") && !hasBeenPickedUp)
        {
            PickupHealthContainer(other.gameObject);
        }
    }
    
    private void PickupHealthContainer(GameObject player)
    {
        // Immediately stop animations
        hasBeenPickedUp = true;
        
        // Find the player's Target component (which manages health)
        Target playerTarget = player.GetComponent<Target>();
        if (playerTarget == null)
        {
            Debug.LogWarning("Player does not have a Target component! Cannot restore health.");
            hasBeenPickedUp = false;
            return;
        }
        
        // Check if player actually needs health (only for partial heal)
        if (!isFullHeal && playerTarget.health >= playerTarget.maxHealth)
        {
            Debug.Log("Player already at full health, health container not consumed.");
            hasBeenPickedUp = false;
            return;
        }
        float healedAmount;
        
        if (isFullHeal)
        {
            // Full heal - restore to max health
            healedAmount = playerTarget.FullHeal();
            Debug.Log($"Player fully healed! Restored {healedAmount} HP");
        }
        else
        {
            // Partial heal - add health amount (Target script handles max limit)
            healedAmount = playerTarget.RestoreHealth(healthAmount);
            Debug.Log($"Player healed! Restored {healedAmount} HP (requested {healthAmount})");
        }
        
        // If no healing occurred, don't consume the container
        if (healedAmount <= 0f)
        {
            Debug.Log("No healing needed, health container not consumed.");
            hasBeenPickedUp = false;
            return;
        }
        
        // Immediately hide the visual model to stop movement
        if (containerModel != null)
        {
            containerModel.SetActive(false);
        }
        
        // Turn off glow light immediately
        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
        
        // Play pickup effects
        PlayPickupEffects();
        
        // Destroy immediately after effects (much faster)
        Destroy(gameObject, 0.1f);
    }
    
    private void PlayPickupEffects()
    {
        // Play pickup sound
        if (pickupSound != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(pickupSound, transform, 1f);
        }
        
        // Play pickup particle effect
        if (pickupEffect != null)
        {
            GameObject effect = Instantiate(pickupEffect, transform.position, transform.rotation);
            
            // Get the particle system component and destroy after it finishes
            ParticleSystem particles = effect.GetComponent<ParticleSystem>();
            if (particles != null)
            {
                var main = particles.main;
                Destroy(effect, main.duration + main.startLifetime.constantMax);
            }
            else
            {
                // If no particle system, destroy after 2 seconds
                Destroy(effect, 2f);
            }
        }
    }
    
    private System.Collections.IEnumerator DestroyAfterEffects()
    {
        // Hide the visual model immediately
        if (containerModel != null)
        {
            containerModel.SetActive(false);
        }
        
        // Turn off glow light
        if (glowLight != null)
        {
            glowLight.enabled = false;
        }
        
        // Wait a bit to let sound/effects play
        yield return new UnityEngine.WaitForSeconds(0.5f);
        
        // Destroy the entire health container object
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    private void ForceDestroy()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
}
