using UnityEngine;

public class AmmoDrop : MonoBehaviour
{
    [Header("Ammo Drop Settings")]
    public bool isRifleAmmo = false; // Check this for rifle ammo
    public bool isGunAmmo = true; // Check this for gun ammo
    public int ammoAmount = 30; // Amount of ammo to give
    
    [Header("Effects")]
    public GameObject pickupEffect; // Particle effect to play when picked up
    public AudioClip pickupSound; // Sound to play when picked up
    
    [Header("Visual Feedback")]
    public GameObject ammoModel; // The visual model of the ammo drop
    public float bobSpeed = 2f; // Speed of the bobbing animation
    public float bobHeight = 0.5f; // Height of the bobbing animation
    public float rotationSpeed = 50f; // Speed of rotation animation
    
    private Vector3 startPosition;
    private bool hasBeenPickedUp = false;
    
    void Start()
    {
        // Store the starting position for bobbing animation
        startPosition = transform.position;
        
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
    }
    
    void Update()
    {
        // Don't animate if already picked up
        if (hasBeenPickedUp) return;
        
        // Bobbing animation - only move the visual model up and down
        if (ammoModel != null)
        {
            // Ensure bobbing starts above ground level and only goes up
            float bobOffset = Mathf.Abs(Mathf.Sin(Time.time * bobSpeed)) * bobHeight;
            float newY = startPosition.y + 0.5f + bobOffset; // Always at least 0.5 units above start position
            ammoModel.transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            
            // Rotation animation - rotate only around the Y axis (vertical)
            ammoModel.transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if player entered the trigger
        if (other.CompareTag("Player") && !hasBeenPickedUp)
        {
            PickupAmmo(other.gameObject);
        }
    }
    
    private void PickupAmmo(GameObject player)
    {
        hasBeenPickedUp = true;
        bool ammoAdded = false;
        
        // Handle rifle ammo
        if (isRifleAmmo)
        {
            RifleScript rifleScript = player.GetComponent<RifleScript>();
            if (rifleScript != null)
            {
                int oldAmmo = rifleScript.reserveAmmo;
                rifleScript.reserveAmmo += ammoAmount;
                ammoAdded = true;
            }
        }
        
        // Handle gun ammo
        if (isGunAmmo)
        {
            GunScript gunScript = player.GetComponent<GunScript>();
            if (gunScript != null)
            {
                int oldAmmo = gunScript.reserveAmmo;
                gunScript.reserveAmmo += ammoAmount;
                // Force UI update
                if (gunScript.reserveAmmoText != null)
                {
                    gunScript.reserveAmmoText.text = gunScript.reserveAmmo.ToString();
                }
                ammoAdded = true;
            }
        }
        
        // If no ammo was added, reset pickup state
        if (!ammoAdded)
        {
            hasBeenPickedUp = false;
            return;
        }
        
        // Play pickup effects
        PlayPickupEffects();
        
        // Hide or destroy the ammo drop
        StartCoroutine(DestroyAfterEffects());
        
        // Also set a backup destruction in case coroutine fails
        Invoke(nameof(ForceDestroy), 1f);
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
        if (ammoModel != null)
        {
            ammoModel.SetActive(false);
        }
        
        // Wait a bit to let sound/effects play
        yield return new UnityEngine.WaitForSeconds(0.5f);
        
        // Destroy the entire ammo drop object
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
