using UnityEngine;
public enum FireMode {
    Single,
    Burst
}
public class PlayerController : MonoBehaviour {
    [SerializeField] private float _speed = 4f;
    [SerializeField] private float _jumpForce = 200f;
    [SerializeField] private Rigidbody _rb;
    
    // References for camera-based movement and combat
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform aimCamera;
    [SerializeField] private Transform freelookCamera;
    [SerializeField] private Transform combatLookAt;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] public FireMode _fireMode = FireMode.Single;
    
    // Remove ground check object since we now use collision callbacks
    private bool isGrounded;
    
    // Store movement input and combat mode status
    private Vector3 _moveInput;
    private bool _combatMode = false;
    Animator animator;
    private float idleTimer = 0f;
    private float idleThreshold = 20f;

    // Add a flag to control movement
    private bool canMove = true;
    
    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if(animator == null)
            animator = GetComponent<Animator>();
    }
    
    // Helper method to check if the gun is active
    private bool IsGunActive() {
        GunScript gunScript = GetComponentInChildren<GunScript>();
        return gunScript != null && gunScript.isGunActive;
    }

    // Helper method to check if the rifle is active
    private bool IsRifleActive() {
        RifleScript rifleScript = GetComponentInChildren<RifleScript>();
        return rifleScript != null && rifleScript.isRifleActive;
    }

    // Helper method to check if any weapon is active
    private bool IsAnyWeaponActive() {
        return IsGunActive() || IsRifleActive();
    }

    // Helper method to check if the gun has ammo
    private bool HasAmmo() {
        GunScript gunScript = GetComponentInChildren<GunScript>();
        return gunScript != null && gunScript.currentAmmo > 0;
    }

    // Helper method to check if the rifle has ammo
    private bool HasRifleAmmo() {
        RifleScript rifleScript = GetComponentInChildren<RifleScript>();
        return rifleScript != null && rifleScript.currentAmmo > 0;
    }

    // Helper method to get the current fire mode for the active weapon
    private FireMode GetActiveWeaponFireMode() {
        if (IsGunActive()) {
            GunScript gunScript = GetComponentInChildren<GunScript>();
            return (gunScript._fireMode == GunNamespace.FireMode.Single) ? FireMode.Single : FireMode.Burst;
        } else if (IsRifleActive()) {
            // Rifle is automatic-only, so always return Burst (for animation purposes)
            return FireMode.Burst;
        }
        return FireMode.Single;
    }

    void Update() {
        if (!canMove) {
            // Disable movement input when canMove is false
            _moveInput = Vector3.zero;
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
            animator.SetBool("isMovingWRifle", false);
            return;
        }

        // Capture input
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        _moveInput = new Vector3(horizontalInput, 0, verticalInput);
        
        if (Input.anyKey) {
            idleTimer = 0f; // reset idle timer when any input is detected
        } else {
            idleTimer += Time.deltaTime;
        }
        
        // When the timer exceeds the threshold, set a parameter to trigger Idle transition.
        if (idleTimer >= idleThreshold) {
            animator.SetBool("isIdle", true);
        } else {
            animator.SetBool("isIdle", false);
        }

        // Remove the old fire mode toggle - weapons handle their own fire modes now
        // if (Input.GetKeyDown(KeyCode.F)) {
        //     _fireMode = (_fireMode == FireMode.Single) ? FireMode.Burst : FireMode.Single;
        //     //Debug.Log("Fire mode set to: " + _fireMode);
        // }

        // Set combat mode if right mouse button is held down
        _combatMode = Input.GetMouseButton(1);
        if (Input.GetKey(KeyCode.Mouse1) && IsAnyWeaponActive()) {
            // Set appropriate aiming animation based on active weapon
            if (IsGunActive()) {
                animator.SetBool("isAiming", true);
                animator.SetBool("isAimingRifle", false);
            } else if (IsRifleActive()) {
                animator.SetBool("isAiming", false);
                animator.SetBool("isAimingRifle", true);
            }

            orientation = aimCamera;

            if (Input.GetKeyDown(KeyCode.Q)) {
                if (IsGunActive()) {
                    animator.SetTrigger("isDivingL");
                } else if (IsRifleActive()) {
                    animator.SetTrigger("isDivingRifleL");
                }
            } else if (Input.GetKeyDown(KeyCode.E)) {
                if (IsGunActive()) {
                    animator.SetTrigger("isDivingR");
                } else if (IsRifleActive()) {
                    animator.SetTrigger("isDivingRifleR");
                }
            } else {
                animator.SetBool("isStanding", true);
            }

            // Check if any weapon is active and has ammo before playing firing animations
            bool hasAmmoForActiveWeapon = (IsGunActive() && HasAmmo()) || (IsRifleActive() && HasRifleAmmo());
            
            if (IsAnyWeaponActive()) {
                FireMode currentFireMode = GetActiveWeaponFireMode();
                
                // Depending on the selected fire mode and weapon type...
                if (currentFireMode == FireMode.Single) {
                    // Single shot: fire only on the first frame when mouse button is pressed
                    if (Input.GetKeyDown(KeyCode.Mouse0) && hasAmmoForActiveWeapon) {
                        if (IsGunActive()) {
                            animator.SetBool("isFiring", true);
                            animator.SetBool("isFiringRifle", false);
                            Debug.Log("Gun single fire animation triggered");
                        } else if (IsRifleActive()) {
                            animator.SetBool("isFiring", false);
                            animator.SetBool("isFiringRifle", true);
                            Debug.Log("Rifle single fire animation triggered");
                        }
                    }
                    if (Input.GetKeyUp(KeyCode.Mouse0)) {
                        animator.SetBool("isFiring", false);
                        animator.SetBool("isFiringRifle", false);
                    }
                    // Don't stop single fire animations due to ammo running out - let them complete
                } else { // Burst mode for gun or Automatic mode for rifle
                    // Burst/Auto mode: fire continuously while mouse button is held down
                    if (Input.GetKeyDown(KeyCode.Mouse0) && hasAmmoForActiveWeapon) {
                        if (IsGunActive()) {
                            animator.SetBool("isFiringAuto", true);
                            animator.SetBool("isFiringRifle", false);
                            Debug.Log("Gun burst fire animation triggered");
                        } else if (IsRifleActive()) {
                            animator.SetBool("isFiringAuto", false);
                            animator.SetBool("isFiringRifle", true);
                            Debug.Log("Rifle auto fire animation triggered (using isFiringRifle)");
                        }
                    }
                    if (Input.GetKeyUp(KeyCode.Mouse0) || !hasAmmoForActiveWeapon) {
                        animator.SetBool("isFiringAuto", false);
                        animator.SetBool("isFiringRifle", false);
                    }
                }
                
                // Only show out of ammo message when trying to fire with no ammo
                if (!hasAmmoForActiveWeapon && Input.GetKeyDown(KeyCode.Mouse0)) {
                    Debug.Log("Out of ammo! Press R to reload.");
                }
            } else {
                // Stop all firing animations when no weapon is active
                animator.SetBool("isFiring", false);
                animator.SetBool("isFiringRifle", false);
                animator.SetBool("isFiringAuto", false);
            }
            
            // Handle left/right walking for strafing animations
            if (Input.GetKey(KeyCode.A)) {
                animator.SetBool("isWalkingL", true);
                animator.SetBool("isWalkingR", false);
            } else if (Input.GetKey(KeyCode.D)) {
                animator.SetBool("isWalkingL", false);
                animator.SetBool("isWalkingR", true);
            } else {
                animator.SetBool("isWalkingL", false);
                animator.SetBool("isWalkingR", false);
            }
        } else {
            // Reset all weapon animations when not aiming
            animator.SetBool("isAiming", false);
            animator.SetBool("isAimingRifle", false);
            animator.SetBool("isFiring", false);
            animator.SetBool("isFiringRifle", false);
            animator.SetBool("isFiringAuto", false);
            animator.SetBool("isWalkingL", false);
            animator.SetBool("isWalkingR", false);
            animator.ResetTrigger("isDivingL");
            animator.ResetTrigger("isDivingR");
            animator.ResetTrigger("isDivingRifleL");
            animator.ResetTrigger("isDivingRifleR");
            animator.SetBool("isStanding", false);

            orientation = freelookCamera; // Reset orientation to the main camera
        }
        
        // Handle jump input (only if grounded)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) {
            _rb.AddForce(Vector3.up * _jumpForce);
            animator.SetBool("isJumping", true);
        } else {
            animator.SetBool("isJumping", false);
        }
        
        // Handle walking animations
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
            Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) {
            animator.SetBool("isWalking", true);
            
            // Set rifle movement animation if rifle is active
            if (IsRifleActive()) {
                animator.SetBool("isMovingWRifle", true);
                Debug.Log("Setting isWalking=true while rifle is active and aiming: " + Input.GetKey(KeyCode.Mouse1));
            } else {
                animator.SetBool("isMovingWRifle", false);
            }
        } else {
            animator.SetBool("isWalking", false);
            animator.SetBool("isMovingWRifle", false);
        }

        // Adjust speed if sprinting
        if (Input.GetKey(KeyCode.LeftShift)) {
            _speed = 8f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) {
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
                
                // Set rifle movement animation if rifle is active (running overrides walking)
                if (IsRifleActive()) {
                    animator.SetBool("isMovingWRifle", true);
                    Debug.Log("Setting isRunning=true while rifle is active and aiming: " + Input.GetKey(KeyCode.Mouse1));
                } else {
                    animator.SetBool("isMovingWRifle", false);
                }
                
                if(Input.GetKeyDown(KeyCode.Q)) {
                    if (IsGunActive()) {
                        animator.SetBool("isDivingL", true);
                    } else if (IsRifleActive()) {
                        animator.SetBool("isDivingRifleL", true);
                    }
                } else {
                    animator.SetBool("isDivingL", false);
                    animator.SetBool("isDivingRifleL", false);
                }
                if(Input.GetKeyDown(KeyCode.E)) {
                    if (IsGunActive()) {
                        animator.SetBool("isDivingR", true);
                    } else if (IsRifleActive()) {
                        animator.SetBool("isDivingRifleR", true);
                    }
                } else {
                    animator.SetBool("isDivingR", false);
                    animator.SetBool("isDivingRifleR", false);
                }
                if (Input.GetKey(KeyCode.Space)) {
                    animator.SetBool("isJumping", true);
                }
            }
        } else {
            _speed = 4f;
            animator.SetBool("isRunning", false);
            animator.SetBool("isJumping", false);
        }

        // Ensure jump animation resets on key press (if needed)
        if (Input.GetKeyDown(KeyCode.Space)) {
            animator.SetBool("isJumping", true);
        } else {
            animator.SetBool("isJumping", false);
        }
    }
    
    void FixedUpdate() {
        // Calculate movement direction relative to the camera (ignore vertical component)
        Vector3 moveDir = (orientation.forward * _moveInput.z + orientation.right * _moveInput.x);
        moveDir.y = 0;
        moveDir = moveDir.normalized;

        // If there's no movement input, stop the character's movement
        if (_moveInput.sqrMagnitude < 0.01f) {
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
        } else {
            // Apply horizontal movement while preserving vertical velocity
            Vector3 newVelocity = moveDir * _speed;
            newVelocity.y = _rb.linearVelocity.y; // Preserve vertical momentum
            _rb.linearVelocity = newVelocity;
        }

        // Handle rotation based on mode
        if (_combatMode && IsAnyWeaponActive()) {
            // Rotate in the direction of the aim camera (ignoring vertical component)
            Vector3 targetDirection = orientation.forward;
            targetDirection.y = 0;
            if (targetDirection.sqrMagnitude > 0.001f) {
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
            }
        } else {
            // Basic mode: rotate based on movement input if there's significant input
            if (moveDir.sqrMagnitude > 0.001f) {
                Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed));
            }
        }
    }
    
    // Use collision callbacks to determine if the player is grounded
    private void OnCollisionStay(Collision collision) {
        // Check every contact point to see if any are "ground-like"
        foreach (ContactPoint contact in collision.contacts) {
            // Consider the player grounded if the contact normal is sufficiently upwards
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.5f) {
                isGrounded = true;
                return; // Once a valid contact is found, exit the method.
            }
        }
        // If no valid ground contact was found in this collision, set grounded to false.
        isGrounded = false;
    }
    
    // When the collision ends, we assume the player is no longer grounded.
    private void OnCollisionExit(Collision collision) {
        isGrounded = false;
    }

    // Method to enable or disable movement
    public void SetMovementEnabled(bool enabled) {
        canMove = enabled;
    }
}
