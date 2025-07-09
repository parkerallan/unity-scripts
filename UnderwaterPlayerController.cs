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

    private BoxCollider boxCollider;
    private bool isSwimming = false;
    private bool isDiving = false;
    private float waterSurfaceY; // Y position of the water surface
    private Rigidbody playerRigidbody;

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
        
        // Get the player's rigidbody for floating physics
        if (playerChest != null)
        {
            playerRigidbody = playerChest.GetComponent<Rigidbody>();
            if (playerRigidbody == null)
            {
                // Try to get rigidbody from parent objects
                playerRigidbody = playerChest.GetComponentInParent<Rigidbody>();
            }
        }
    }

    private void Update()
    {
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
        }
        else if (!chestInWater && isSwimming)
        {
            playerAnimator.runtimeAnimatorController = normalController;
            playerAnimator.Rebind();
            isSwimming = false;
            isDiving = false;
            playerAnimator.SetBool("isPaddling", false);
            playerAnimator.SetBool("isPaddlingForward", false);
            playerAnimator.SetBool("isPaddlingBackward", false);
            playerAnimator.SetBool("isPaddlingLeft", false);
            playerAnimator.SetBool("isPaddlingRight", false);
        }

        if (isSwimming)
        {
            // Apply floating force to keep player at water surface
            ApplyFloatingForce();
            
            // Directional paddling is always available in water
            playerAnimator.SetBool("isPaddlingForward", Input.GetKey(KeyCode.W));
            playerAnimator.SetBool("isPaddlingBackward", Input.GetKey(KeyCode.S));
            playerAnimator.SetBool("isPaddlingLeft", Input.GetKey(KeyCode.A));
            playerAnimator.SetBool("isPaddlingRight", Input.GetKey(KeyCode.D));

            // Diving logic placeholder (do nothing for now)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                isDiving = !isDiving;
                // Add diving logic here later
            }
        }
    }
    
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