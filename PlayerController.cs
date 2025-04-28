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

    // Helper method to check if the gun has ammo
    private bool HasAmmo() {
        GunScript gunScript = GetComponentInChildren<GunScript>();
        return gunScript != null && gunScript.currentAmmo > 0;
    }

    void Update() {
        if (!canMove) {
            // Disable movement input when canMove is false
            _moveInput = Vector3.zero;
            animator.SetBool("isWalking", false);
            animator.SetBool("isRunning", false);
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

        if (Input.GetKeyDown(KeyCode.F)) {
            _fireMode = (_fireMode == FireMode.Single) ? FireMode.Burst : FireMode.Single;
            //Debug.Log("Fire mode set to: " + _fireMode);
        }

        // Set combat mode if right mouse button is held down
        _combatMode = Input.GetMouseButton(1);
        if (Input.GetKey(KeyCode.Mouse1) && IsGunActive()) {
            animator.SetBool("isAiming", true);

            orientation = aimCamera;

            if (Input.GetKeyDown(KeyCode.Q)) {
                animator.SetTrigger("isDivingL");
            } else if (Input.GetKeyDown(KeyCode.E)) {
                animator.SetTrigger("isDivingR");
            } else {
                animator.SetBool("isStanding", true);
            }

            // Check if the gun is active and has ammo before playing firing animations
            if (IsGunActive() && HasAmmo()) {
                // Depending on the selected fire mode...
                if (_fireMode == FireMode.Single) {
                    // Single shot: fire only on the first frame when mouse button is pressed
                    if (Input.GetKeyDown(KeyCode.Mouse0)) {
                        animator.SetBool("isFiring", true);
                    }
                    if (Input.GetKeyUp(KeyCode.Mouse0)) {
                        animator.SetBool("isFiring", false);
                    }
                } else if (_fireMode == FireMode.Burst) {
                    // Burst mode: fire continuously while mouse button is held down
                    if (Input.GetKeyDown(KeyCode.Mouse0)) {
                        animator.SetBool("isFiringAuto", true);
                    }
                    if (Input.GetKeyUp(KeyCode.Mouse0)) {
                        animator.SetBool("isFiringAuto", false);
                    }
                }
            } else if (!HasAmmo()) {
                Debug.Log("Out of ammo! Press R to reload.");
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
            animator.SetBool("isAiming", false);
            animator.SetBool("isFiring", false);
            animator.SetBool("isFiringAuto", false);
            animator.SetBool("isWalkingL", false);
            animator.SetBool("isWalkingR", false);
            animator.ResetTrigger("isDivingL");
            animator.ResetTrigger("isDivingR");
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
        } else {
            animator.SetBool("isWalking", false);
        }

        // Adjust speed if sprinting
        if (Input.GetKey(KeyCode.LeftShift)) {
            _speed = 8f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) ||
                Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)) {
                animator.SetBool("isRunning", true);
                animator.SetBool("isWalking", false);
                if(Input.GetKeyDown(KeyCode.Q)) {
                    animator.SetBool("isDivingL", true);
                } else {
                    animator.SetBool("isDivingL", false);
                }
                if(Input.GetKeyDown(KeyCode.E)) {
                    animator.SetBool("isDivingR", true);
                } else {
                    animator.SetBool("isDivingR", false);
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
        if (_combatMode && IsGunActive()) {
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
