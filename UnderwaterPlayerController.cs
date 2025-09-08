using UnityEngine;

public class UnderwaterPlayerController : MonoBehaviour
{
    [Header("Animator Controllers")]
    public RuntimeAnimatorController normalController;
    public RuntimeAnimatorController underwaterController;

    [Header("Player References")]
    public Animator playerAnimator;
    public Transform playerChest; // Assign the player's chest or root transform in the Inspector

    [Header("Swimming Settings")]
    public float waterSurfaceOffset = 0.5f; // How much above the water surface the player should float
    public float floatForce = 10f; // How strong the floating force is
    public float floatDamping = 0.5f; // Damping to prevent oscillation
    
    [Header("Water Physics")]
    public float waterDrag = 5f; // How much water slows down movement (higher = more resistance)
    public float waterAngularDrag = 10f; // Rotational drag in water
    public float maxVerticalSpeed = 2f; // Maximum speed player can move up/down in water
    public float buoyancyForce = 8f; // Natural upward force in water
    public float surfaceTension = 15f; // Force that keeps player at surface when near it
    
    [Header("Diving Controls")]
    public float surfaceFloatForce = 20f; // Force applied when floating back to surface with "V" (increased)
    public float divingDownwardForce = 8f; // Force applied when diving down
    public float surfaceReachThreshold = 1f; // How close to surface before stopping auto-float (increased)

    private BoxCollider boxCollider;
    private bool isSwimming = false;
    private bool isDiving = false;
    private bool isFloatingToSurface = false; // New state for actively floating to surface
    private float waterSurfaceY; // Y position of the water surface
    private Rigidbody playerRigidbody;
    
    // Store original physics settings
    private float originalDrag;
    private float originalAngularDrag;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && !boxCollider.isTrigger)
            boxCollider.isTrigger = true;
            
        // Calculate water surface Y position (top of the box collider)
        if (boxCollider != null)
        {
            waterSurfaceY = boxCollider.bounds.max.y;
        }
        
        // Auto-find player components if not assigned
        FindPlayerComponents();
    }
    
    private void FindPlayerComponents()
    {
        // Auto-find player animator if not assigned
        if (playerAnimator == null)
        {
            // First try to find CharModel1 anywhere in the scene hierarchy
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Search for CharModel1 in the player's children
                Transform charModel = player.transform.Find("CharModel1");
                if (charModel == null)
                {
                    // If not direct child, search recursively
                    charModel = FindChildRecursive(player.transform, "CharModel1");
                }
                
                if (charModel != null)
                {
                    playerAnimator = charModel.GetComponent<Animator>();
                    if (playerAnimator != null)
                    {
                        Debug.Log("UnderwaterPlayerController: Assigned CharModel1 Animator to playerAnimator field");
                    }
                }
                else
                {
                    // Try to get any animator from player
                    playerAnimator = player.GetComponentInChildren<Animator>();
                    if (playerAnimator != null)
                    {
                        Debug.Log("UnderwaterPlayerController: Assigned Player Animator to playerAnimator field (fallback)");
                    }
                }
            }
        }
        
        // Auto-find player chest/transform if not assigned
        if (playerChest == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Look for WaterLevel object first (this is what we want for water detection)
                Transform waterLevel = FindChildRecursive(player.transform, "WaterLevel");
                if (waterLevel != null)
                {
                    playerChest = waterLevel;
                    Debug.Log("UnderwaterPlayerController: Assigned WaterLevel transform to playerChest field");
                }
                else
                {
                    // Fallback to CharModel1 if WaterLevel not found
                    Transform charModel = FindChildRecursive(player.transform, "CharModel1");
                    if (charModel != null)
                    {
                        playerChest = charModel;
                        Debug.Log("UnderwaterPlayerController: Assigned CharModel1 transform to playerChest field");
                    }
                    else
                    {
                        // Final fallback to player root
                        playerChest = player.transform;
                        Debug.Log("UnderwaterPlayerController: Assigned Player root transform to playerChest field");
                    }
                }
            }
        }
        
        // Get the player's rigidbody for floating physics
        if (playerChest != null)
        {
            playerRigidbody = playerChest.GetComponent<Rigidbody>();
            if (playerRigidbody == null)
            {
                // Try to get rigidbody from parent objects
                playerRigidbody = playerChest.GetComponentInParent<Rigidbody>();
            }
            
            // Store original physics settings
            if (playerRigidbody != null)
            {
                originalDrag = playerRigidbody.linearDamping;
                originalAngularDrag = playerRigidbody.angularDamping;
                Debug.Log("UnderwaterPlayerController: Found player Rigidbody and stored original physics settings");
            }
            else
            {
                Debug.LogWarning("UnderwaterPlayerController: Could not find player Rigidbody");
            }
        }
        else
        {
            Debug.LogWarning("UnderwaterPlayerController: Could not find player chest/transform");
        }
        
        // Log final assignment status
        Debug.Log($"UnderwaterPlayerController: Final assignments - playerAnimator: {(playerAnimator != null ? "ASSIGNED" : "NULL")}, playerChest: {(playerChest != null ? "ASSIGNED" : "NULL")}");
    }
    
    /// <summary>
    /// Recursively search for a child object by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Check direct children first
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;
        }
        
        // Search recursively in children
        foreach (Transform child in parent)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        
        return null;
    }

    private void Start()
    {
        // Double-check player components in case they weren't available during Awake
        if (playerAnimator == null || playerChest == null)
        {
            Debug.Log("UnderwaterPlayerController: Re-checking for player components in Start()");
            FindPlayerComponents();
        }
    }

    private void Update()
    {
        // Continuously try to find and assign missing components
        if (playerAnimator == null || playerChest == null)
        {
            FindPlayerComponents();
        }
        
        if (playerAnimator == null || normalController == null || underwaterController == null || playerChest == null || boxCollider == null)
            return;

        // Update water surface Y position dynamically
        if (boxCollider != null)
        {
            waterSurfaceY = boxCollider.bounds.max.y;
        }
        
        // Check if the player's chest is inside the water volume
        bool chestInWater = boxCollider.bounds.Contains(playerChest.position);

        if (chestInWater && !isSwimming)
        {
            playerAnimator.runtimeAnimatorController = underwaterController;
            playerAnimator.Rebind();
            isSwimming = true;
            isDiving = false;
            playerAnimator.SetBool("isPaddling", true);
            
            // Apply water physics
            ApplyWaterPhysics();
        }
        else if (!chestInWater && isSwimming)
        {
            playerAnimator.runtimeAnimatorController = normalController;
            playerAnimator.Rebind();
            isSwimming = false;
            isDiving = false;
            isFloatingToSurface = false;
            playerAnimator.SetBool("isPaddling", false);
            playerAnimator.SetBool("isPaddlingForward", false);
            playerAnimator.SetBool("isPaddlingBackward", false);
            playerAnimator.SetBool("isPaddlingLeft", false);
            playerAnimator.SetBool("isPaddlingRight", false);
            
            // Restore original physics
            RestoreOriginalPhysics();
        }

        if (isSwimming)
        {
            // Apply realistic water movement physics
            ApplyWaterMovement();
            
            // Directional paddling is always available in water
            playerAnimator.SetBool("isPaddlingForward", Input.GetKey(KeyCode.W));
            playerAnimator.SetBool("isPaddlingBackward", Input.GetKey(KeyCode.S));
            playerAnimator.SetBool("isPaddlingLeft", Input.GetKey(KeyCode.A));
            playerAnimator.SetBool("isPaddlingRight", Input.GetKey(KeyCode.D));

            // Diving and surfacing controls - Space toggles between dive/surface/normal
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (!isDiving && !isFloatingToSurface)
                {
                    // Normal state -> Start diving
                    isDiving = true;
                    isFloatingToSurface = false;
                    Debug.Log("Started diving");
                }
                else if (isDiving)
                {
                    // Diving -> Start surfacing
                    isDiving = false;
                    isFloatingToSurface = true;
                    Debug.Log("Stopped diving, started floating to surface");
                }
                else if (isFloatingToSurface)
                {
                    // Surfacing -> Return to normal
                    isDiving = false;
                    isFloatingToSurface = false;
                    Debug.Log("Stopped floating to surface, returning to normal");
                }
            }
        }
    }
    
    /// <summary>
    /// Apply water-like physics settings to the rigidbody
    /// </summary>
    private void ApplyWaterPhysics()
    {
        if (playerRigidbody == null) return;
        
        // Increase drag to simulate water resistance
        playerRigidbody.linearDamping = waterDrag;
        playerRigidbody.angularDamping = waterAngularDrag;
    }
    
    /// <summary>
    /// Restore original physics settings when leaving water
    /// </summary>
    private void RestoreOriginalPhysics()
    {
        if (playerRigidbody == null) return;
        
        playerRigidbody.linearDamping = originalDrag;
        playerRigidbody.angularDamping = originalAngularDrag;
    }
    
    /// <summary>
    /// Apply realistic water movement with buoyancy and surface tension
    /// </summary>
    private void ApplyWaterMovement()
    {
        if (playerRigidbody == null) return;
        
        Vector3 velocity = playerRigidbody.linearVelocity;
        
        // Calculate the target Y position (water surface - offset)
        float targetY = waterSurfaceY - waterSurfaceOffset;
        float currentY = playerChest.position.y;
        float distanceFromSurface = targetY - currentY;
        
        // Handle different movement modes
        if (isFloatingToSurface)
        {
            // Active floating to surface - apply strong continuous upward force
            Vector3 surfaceForce = Vector3.up * surfaceFloatForce * Time.deltaTime;
            playerRigidbody.AddForce(surfaceForce, ForceMode.VelocityChange);
            
            // More aggressive upward velocity when actively surfacing
            if (velocity.y < surfaceFloatForce * 0.5f)
            {
                velocity.y = Mathf.Lerp(velocity.y, surfaceFloatForce * 0.5f, Time.deltaTime * 3f);
            }
            
            // Stop floating when actually at or above the surface
            if (currentY >= targetY - surfaceReachThreshold)
            {
                isFloatingToSurface = false;
                Debug.Log($"Reached surface at Y={currentY:F2}, target was Y={targetY:F2} - stopped floating");
            }
            else
            {
                Debug.Log($"Floating to surface: Current Y={currentY:F2}, Target Y={targetY:F2}, Distance={distanceFromSurface:F2}");
            }
        }
        else if (isDiving)
        {
            // Apply downward force when diving
            Vector3 divingForce = Vector3.down * divingDownwardForce * Time.deltaTime;
            playerRigidbody.AddForce(divingForce, ForceMode.VelocityChange);
        }
        else
        {
            // Normal swimming - apply natural buoyancy
            Vector3 buoyancy = Vector3.up * buoyancyForce * Time.deltaTime;
            
            // Apply stronger surface tension when near the target surface
            if (Mathf.Abs(distanceFromSurface) < 2f)
            {
                // Surface tension force - pulls player toward surface level
                float tensionMultiplier = Mathf.Clamp01(2f - Mathf.Abs(distanceFromSurface));
                Vector3 surfaceForce = Vector3.up * distanceFromSurface * surfaceTension * tensionMultiplier * Time.deltaTime;
                buoyancy += surfaceForce;
                
                // Extra damping when very close to surface to prevent oscillation
                if (Mathf.Abs(distanceFromSurface) < 0.5f)
                {
                    velocity.y *= (1f - floatDamping * 2f * Time.deltaTime);
                }
            }
            
            // Apply buoyancy force
            playerRigidbody.AddForce(buoyancy, ForceMode.VelocityChange);
        }
        
        // Clamp vertical velocity to prevent flying out of water (except when actively floating to surface)
        if (!isFloatingToSurface)
        {
            velocity.y = Mathf.Clamp(velocity.y, -maxVerticalSpeed, maxVerticalSpeed);
        }
        else
        {
            // Allow much higher upward velocity when actively floating to surface
            velocity.y = Mathf.Clamp(velocity.y, -maxVerticalSpeed, maxVerticalSpeed * 3f);
        }
        
        // Apply the clamped velocity
        playerRigidbody.linearVelocity = velocity;
    }
    
    /// <summary>
    /// Legacy floating force method (kept for compatibility)
    /// </summary>
    private void ApplyFloatingForce()
    {
        if (playerRigidbody == null || isDiving) return;
        
        // Calculate the target Y position (water surface + offset)
        float targetY = waterSurfaceY - waterSurfaceOffset;
        float currentY = playerChest.position.y;
        
        // Calculate the distance from target
        float distanceFromSurface = targetY - currentY;
        
        // Apply upward force if player is below the target surface level
        if (distanceFromSurface > 0.1f)
        {
            Vector3 floatingForce = Vector3.up * floatForce * distanceFromSurface;
            playerRigidbody.AddForce(floatingForce, ForceMode.Force);
        }
        
        // Apply damping to reduce vertical velocity when near surface
        if (Mathf.Abs(distanceFromSurface) < 1f)
        {
            Vector3 velocity = playerRigidbody.linearVelocity;
            velocity.y *= (1f - floatDamping * Time.deltaTime);
            playerRigidbody.linearVelocity = velocity;
        }
    }
}