using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Reflection; // Required for reflection

namespace RifleNamespace {
    public enum FireMode {
        Automatic
    }
}

public class RifleScript : MonoBehaviour
{
    public float damage = 12f;
    public float range = 150f;
    public CinemachineCamera FreeLookCamera;
    public CinemachineCamera AimCamera;
    public RectTransform crosshair;
    public RifleNamespace.FireMode _fireMode = RifleNamespace.FireMode.Automatic;
    public ParticleSystem muzzleFlash;
    public GameObject characterImpactEffect;
    public GameObject environmentImpactEffect;
    public float impactForce = 30f;

    [Header("Weapon Settings")]
    public GameObject rifleModel; // Reference to the rifle's visual model
    public bool isRifleActive = false; // Tracks if the rifle is active
    public GunScript gunScript; // Reference to the gun script for weapon switching

    [Header("Camera Priority Settings")]
    [SerializeField] private int aimCameraPriority = 15;
    [SerializeField] private int freeLookCameraPriority = 10;

    [Header("Ammo Settings")]
    public int maxAmmo = 30; // Maximum ammo in the magazine
    public int currentAmmo; // Current ammo in the magazine
    public float reloadTime = 2.5f; // Time it takes to reload
    public bool isReloading = false;

    [Header("Fire Rate Settings")]
    public float fireRate = 600f; // Rounds per minute for automatic fire
    private float fireDelay; // Calculated delay between shots
    public bool isFiring = false;
    public Coroutine automaticFireCoroutine;
    public Coroutine burstCoroutine;

    // Helper method to check if player is near a dialogue trigger
    private bool IsNearDialogueTrigger() {
        DialogueTrigger[] dialogueTriggers = FindObjectsByType<DialogueTrigger>(FindObjectsSortMode.None);
        foreach (DialogueTrigger trigger in dialogueTriggers) {
            if (trigger.IsPlayerInDialogueRange()) {
                return true;
            }
        }
        return false;
    }

    [Header("SFX")]
    public AudioClip gunshotSound;
    public AudioClip reloadSound;
    public AudioClip emptyClickSound;

    void Start()
    {
        // Set initial camera priorities
        if (FreeLookCamera != null) {
            FreeLookCamera.Priority = freeLookCameraPriority;
        }
        if (AimCamera != null) {
            AimCamera.Priority = freeLookCameraPriority - 1; // Lower priority initially
        }

        // Ensure the rifle model is visible or hidden based on the initial state
        isRifleActive = false;
        UpdateRifleVisibility();

        // Initialize ammo
        currentAmmo = maxAmmo;

        // Calculate fire delay from fire rate (RPM to seconds between shots)
        fireDelay = 60f / fireRate;
    }

    void Update()
    {
        // Weapon switching is now handled by WeaponManager
        
        // If the rifle is not active, skip all other logic
        if (!isRifleActive) return;

        // Reload mechanic
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
            return;
        }
        
        // Check for aim release even during reload
        if (isReloading && Input.GetKeyUp(KeyCode.Mouse1))
        {
            if (AimCamera != null && FreeLookCamera != null)
            {
                FreeLookCamera.Priority = aimCameraPriority;
                AimCamera.Priority = freeLookCameraPriority;
                crosshair.gameObject.SetActive(false);
            }
            
            // Stop automatic fire when exiting aim mode during reload
            if (automaticFireCoroutine != null)
            {
                StopCoroutine(automaticFireCoroutine);
                isFiring = false;
            }
        }
        
        if (isReloading) return; // Skip shooting logic while reloading

        // Remove fire mode switching since rifle is automatic-only
        // if (Input.GetKeyDown(KeyCode.F))
        // {
        //     _fireMode = (_fireMode == RifleNamespace.FireMode.Single) ? RifleNamespace.FireMode.Automatic : RifleNamespace.FireMode.Single;
        //     Debug.Log("RifleScript: Fire mode set to " + _fireMode);
        // }

        // When right mouse button is pressed down, switch to the aim camera
        if (Input.GetKey(KeyCode.Mouse1) && !IsNearDialogueTrigger())
        {
            if (AimCamera != null && FreeLookCamera != null)
            {
                AimCamera.Priority = aimCameraPriority;
                FreeLookCamera.Priority = freeLookCameraPriority;
                crosshair.gameObject.SetActive(true);
            }
            CinemachineRotationComposer rotComposer = AimCamera.GetComponent<CinemachineRotationComposer>();
            // Shoulder flipping logic
            if (Input.GetKeyDown(KeyCode.Mouse2)) // Middle mouse button
            {
                var composition = rotComposer.Composition;
                composition.ScreenPosition.x = -composition.ScreenPosition.x;
                rotComposer.Composition = composition;
            }

            // Handle mouse input for automatic firing only
            if (Input.GetKeyDown(KeyCode.Mouse0) && !isFiring)
            {
                if (currentAmmo <= 0)
                {
                    Debug.Log("Out of ammo! Press R to reload.");
                    if (emptyClickSound != null)
                    {
                        SFXManager.instance.PlaySFXClip(emptyClickSound, transform, 1f);
                    }
                    return;
                }

                // Rifle is automatic-only, so always start automatic fire
                automaticFireCoroutine = StartCoroutine(AutomaticFireCoroutine());
            }

            // Stop automatic fire when mouse button is released
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                if (automaticFireCoroutine != null)
                {
                    StopCoroutine(automaticFireCoroutine);
                    isFiring = false;
                }
            }

            // Handle Q and E input for diving burst fire
            if (Input.GetKeyDown(KeyCode.Q) && !isFiring)
            {
                if (currentAmmo <= 0)
                {
                    Debug.Log("Out of ammo! Press R to reload.");
                    if (emptyClickSound != null)
                    {
                        SFXManager.instance.PlaySFXClip(emptyClickSound, transform, 1f);
                    }
                    return;
                }
                burstCoroutine = StartCoroutine(DivingBurstFireCoroutine());
            }
            else if (Input.GetKeyDown(KeyCode.E) && !isFiring)
            {
                if (currentAmmo <= 0)
                {
                    Debug.Log("Out of ammo! Press R to reload.");
                    if (emptyClickSound != null)
                    {
                        SFXManager.instance.PlaySFXClip(emptyClickSound, transform, 1f);
                    }
                    return;
                }
                burstCoroutine = StartCoroutine(DivingBurstFireCoroutine());
            }
        }

        // When right mouse button is released, revert back to the main camera
        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            if (AimCamera != null && FreeLookCamera != null)
            {
                FreeLookCamera.Priority = aimCameraPriority;
                AimCamera.Priority = freeLookCameraPriority;
                crosshair.gameObject.SetActive(false);
            }

            // Stop automatic fire when exiting aim mode
            if (automaticFireCoroutine != null)
            {
                StopCoroutine(automaticFireCoroutine);
                isFiring = false;
            }
        }
    }

    public void ToggleRifle()
    {
        isRifleActive = !isRifleActive;
        
        // If activating rifle, deactivate gun
        if (isRifleActive && gunScript != null && gunScript.isGunActive)
        {
            gunScript.DeactivateGun();
        }
        
        // If deactivating rifle, reset camera and crosshair
        if (!isRifleActive)
        {
            if (AimCamera != null && FreeLookCamera != null)
            {
                FreeLookCamera.Priority = aimCameraPriority;
                AimCamera.Priority = freeLookCameraPriority;
            }
            if (crosshair != null)
            {
                crosshair.gameObject.SetActive(false);
            }
            
            // Stop any ongoing firing coroutines
            if (automaticFireCoroutine != null)
            {
                StopCoroutine(automaticFireCoroutine);
                isFiring = false;
            }
            if (burstCoroutine != null)
            {
                StopCoroutine(burstCoroutine);
                isFiring = false;
            }
        }
        
        UpdateRifleVisibility();
        
        // Set animator trigger for swapping rifle
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("isSwappingRifle");
        }
        
        Debug.Log("RifleScript: Rifle is now " + (isRifleActive ? "active" : "inactive"));
    }

    // Method to activate rifle without toggling
    public void ActivateRifle()
    {
        if (!isRifleActive)
        {
            isRifleActive = true;
            UpdateRifleVisibility();
            
            // Set animator trigger for swapping rifle
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("isSwappingRifle");
            }
            
            Debug.Log("RifleScript: Rifle activated");
        }
    }

    public void DeactivateRifle()
    {
        isRifleActive = false;
        
        // Reset camera priorities and crosshair when deactivating
        if (AimCamera != null && FreeLookCamera != null)
        {
            FreeLookCamera.Priority = aimCameraPriority;
            AimCamera.Priority = freeLookCameraPriority;
        }
        if (crosshair != null)
        {
            crosshair.gameObject.SetActive(false);
        }
        
        // Stop any ongoing firing coroutines
        if (automaticFireCoroutine != null)
        {
            StopCoroutine(automaticFireCoroutine);
            isFiring = false;
        }
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            isFiring = false;
        }
        
        UpdateRifleVisibility();
        Debug.Log("RifleScript: Rifle deactivated by external script");
    }

    void UpdateRifleVisibility()
    {
        if (rifleModel != null)
        {
            rifleModel.SetActive(isRifleActive);
        }
    }

    void Shoot()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("Out of ammo! Press R to reload.");
            if (emptyClickSound != null)
            {
                SFXManager.instance.PlaySFXClip(emptyClickSound, transform, 1f);
            }
            return;
        }

        muzzleFlash.Play();
        currentAmmo--; // Decrease ammo count
        Debug.Log("Ammo remaining: " + currentAmmo);
        SFXManager.instance.PlaySFXClip(gunshotSound, transform, 1f); // Play gunshot sound

        RaycastHit hit;

        // Ignore trigger colliders during raycasting
        if (Physics.Raycast(AimCamera.transform.position, AimCamera.transform.forward, out hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Hit: " + hit.transform.name + " at distance " + hit.distance);

            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);

                if (characterImpactEffect != null)
                {
                    GameObject impactObj = Instantiate(characterImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    impactObj.transform.SetParent(hit.transform);
                    Destroy(impactObj, 1.0f);
                }
            }
            else
            {
                if (environmentImpactEffect != null)
                {
                    GameObject impactObj = Instantiate(environmentImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(impactObj, 1.0f);
                }
            }

            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * impactForce);
            }
        }
    }

    IEnumerator Reload()
    {
        Debug.Log("Reloading...");
        isReloading = true;

        // Trigger reload animation
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("isReloadingRifle", true);
        }

        SFXManager.instance.PlaySFXClip(reloadSound, transform, 1f); // Play reload sound
        
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        
        // Stop reload animation
        if (animator != null)
        {
            animator.SetBool("isReloadingRifle", false);
        }
        
        Debug.Log("Reload complete. Ammo refilled to " + currentAmmo);
    }

    IEnumerator AutomaticFireCoroutine()
    {
        if (isFiring) {
            yield break;
        }

        isFiring = true;

        while (isFiring && currentAmmo > 0 && Input.GetKey(KeyCode.Mouse0) || Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E))
        {
            if (currentAmmo <= 0)
            {
                Debug.Log("Out of ammo! Press R to reload.");
                break;
            }

            Shoot();
            yield return new WaitForSeconds(fireDelay);
        }

        isFiring = false;
    }

    IEnumerator BurstFireCoroutine()
    {
        if (isFiring) {
            yield break;
        }

        isFiring = true;

        for (int i = 0; i < 3; i++)
        {
            if (currentAmmo <= 0)
            {
                Debug.Log("Out of ammo! Press R to reload.");
                break;
            }

            Shoot();
            yield return new WaitForSeconds(0.1f);
        }

        isFiring = false;
    }

    IEnumerator DivingBurstFireCoroutine()
    {
        if (isFiring) {
            yield break;
        }

        isFiring = true;

        for (int i = 0; i < 10; i++)
        {
            if (currentAmmo <= 0)
            {
                Debug.Log("Out of ammo! Press R to reload.");
                break;
            }

            Shoot();
            yield return new WaitForSeconds(0.05f);
        }

        isFiring = false;
    }

    void OnDisable()
    {
        if (automaticFireCoroutine != null)
        {
            StopCoroutine(automaticFireCoroutine);
            isFiring = false;
        }
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            isFiring = false;
        }
    }
}
