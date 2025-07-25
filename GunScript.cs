using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Reflection; // Required for reflection

namespace GunNamespace {
    public enum FireMode {
        Single,
        Burst
    }
}

public class GunScript : MonoBehaviour
{
    public float damage = 10f;
    public float range = 100f;
    public CinemachineCamera FreeLookCamera;
    public CinemachineCamera AimCamera;
    public RectTransform crosshair;
    public GunNamespace.FireMode _fireMode = GunNamespace.FireMode.Single;
    public ParticleSystem muzzleFlash;
    public ParticleSystem bulletCasingDrop;
    public GameObject characterImpactEffect;
    public GameObject environmentImpactEffect;
    public float impactForce = 30f;

    [Header("Weapon Settings")]
    public GameObject gunModel; // Reference to the gun's visual model
    public bool isGunActive = false; // Tracks if the gun is active
    public RifleScript rifleScript; // Reference to the rifle script for weapon switching

    [Header("Camera Priority Settings")]
    [SerializeField] private int aimCameraPriority = 15;
    [SerializeField] private int freeLookCameraPriority = 10;

    [Header("Ammo Settings")]
    public int maxAmmo = 20; // Maximum ammo in the magazine
    public int currentAmmo; // Current ammo in the magazine
    public int reserveAmmo = 100; // Reserve ammo not loaded in weapon
    public float reloadTime = 2f; // Time it takes to reload
    public bool isReloading = false;
    
    [Header("Reload Cooldown")]
    public float reloadCooldown = 4f; // Cooldown time before allowing another reload
    private float lastReloadTime = -999f; // Time when last reload was initiated

    public bool isFiring = false;
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
    public AudioClip fireModeSwitchSound; // Sound effect for fire mode switching

    [Header("UI References")]
    public TMPro.TextMeshProUGUI ammoText; // Reference to UI text for ammo display
    public TMPro.TextMeshProUGUI reserveAmmoText; // Reference to UI text for reserve ammo display
    public TMPro.TextMeshProUGUI fireModeText; // Reference to UI text for fire mode display

    void Start()
    {
        // Set initial camera priorities
        if (FreeLookCamera != null) {
            FreeLookCamera.Priority = freeLookCameraPriority;
        }
        if (AimCamera != null) {
            AimCamera.Priority = freeLookCameraPriority - 1; // Lower priority initially
        }

        // Ensure the gun model is visible or hidden based on the initial state
        isGunActive = false;
        UpdateGunVisibility();

        // Initialize ammo
        currentAmmo = maxAmmo;
        
        // Update UI on start
        UpdateAmmoUI();
        UpdateFireModeUI();

    }

    void Update()
    {
        // Weapon switching is now handled by WeaponManager
        
        // If the gun is not active, skip all other logic
        if (!isGunActive) return;

        // Reload mechanic with cooldown
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Check if enough time has passed since last reload attempt
            if (Time.time - lastReloadTime >= reloadCooldown)
            {
                // Only reload if not at max ammo and have reserve ammo
                if (currentAmmo < maxAmmo && reserveAmmo > 0)
                {
                    lastReloadTime = Time.time;
                    StartCoroutine(Reload());
                }
                else if (currentAmmo >= maxAmmo)
                {
                    Debug.Log("Magazine is already full!");
                }
                else if (reserveAmmo <= 0)
                {
                    Debug.Log("No reserve ammo left!");
                }
            }
            else
            {
                float remainingCooldown = reloadCooldown - (Time.time - lastReloadTime);
                Debug.Log($"Reload on cooldown. Wait {remainingCooldown:F1}s before reloading again.");
            }
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
        }
        
        if (isReloading) return; // Skip shooting logic while reloading

        if (Input.GetKeyDown(KeyCode.F))
        {
            _fireMode = (_fireMode == GunNamespace.FireMode.Single) ? GunNamespace.FireMode.Burst : GunNamespace.FireMode.Single;
            UpdateFireModeUI(); // Update UI when fire mode changes
            
            // Play fire mode switch sound effect
            if (fireModeSwitchSound != null)
            {
                SFXManager.instance.PlaySFXClip(fireModeSwitchSound, transform, 1f);
            }
            
            Debug.Log("GunScript: Fire mode set to " + _fireMode);
        }

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

            // Handle mouse input for firing
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

                if (_fireMode == GunNamespace.FireMode.Burst)
                {
                    burstCoroutine = StartCoroutine(BurstFireCoroutine());
                }
                else
                {
                    Shoot();
                }
            }

            // Handle Q and E input for diving burst
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
        }
    }

    public void ToggleGun()
    {
        isGunActive = !isGunActive;
        
        // If activating gun, deactivate rifle
        if (isGunActive && rifleScript != null && rifleScript.isRifleActive)
        {
            rifleScript.DeactivateRifle();
        }
        
        // If deactivating gun, reset camera and crosshair
        if (!isGunActive)
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
        }
        
        UpdateGunVisibility();
        
        // Set animator trigger for swapping
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetTrigger("isSwapping");
        }
        
        Debug.Log("GunScript: Gun is now " + (isGunActive ? "active" : "inactive"));
    }

    // Method to activate gun without toggling
    public void ActivateGun()
    {
        if (!isGunActive)
        {
            isGunActive = true;
            UpdateGunVisibility();
            
            // Set animator trigger for swapping
            Animator animator = GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("isSwapping");
            }
            
            Debug.Log("GunScript: Gun activated");
        }
    }

    public void DeactivateGun()
    {
        isGunActive = false;
        
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
        
        UpdateGunVisibility();
        Debug.Log("GunScript: Gun deactivated by external script");
    }

    void UpdateGunVisibility()
    {
        if (gunModel != null)
        {
            gunModel.SetActive(isGunActive);
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
        
        // Play bullet casing drop effect
        if (bulletCasingDrop != null)
        {
            bulletCasingDrop.Play();
        }
        
        currentAmmo--; // Decrease ammo count
        UpdateAmmoUI(); // Update UI after shooting
        Debug.Log("Ammo remaining: " + currentAmmo);
        SFXManager.instance.PlaySFXClip(gunshotSound, transform, 1f); // Play gunshot sound

        RaycastHit hit;

        // Ignore trigger colliders during raycasting
        if (Physics.Raycast(AimCamera.transform.position, AimCamera.transform.forward, out hit, range, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Hit: " + hit.transform.name + " at distance " + hit.distance);

            // Check if the hit object has a Target component and apply damage
            Target target = hit.transform.GetComponent<Target>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            
            // Check if we hit an enemy AI and alert them
            EnemyAIController enemyAI = hit.transform.GetComponent<EnemyAIController>();
            if (enemyAI != null)
            {
                enemyAI.OnHitByPlayer();
            }

            // Determine impact effect based on layer
            string layerName = LayerMask.LayerToName(hit.transform.gameObject.layer);
            bool isCharacter = layerName == "Enemy" || layerName == "Player";
            
            if (isCharacter && characterImpactEffect != null)
            {
                GameObject impactObj = Instantiate(characterImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                impactObj.transform.SetParent(hit.transform);
                Destroy(impactObj, 1.0f);
            }
            else if (!isCharacter && environmentImpactEffect != null)
            {
                GameObject impactObj = Instantiate(environmentImpactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactObj, 1.0f);
            }

            if (hit.rigidbody != null)
            {
                hit.rigidbody.AddForce(-hit.normal * impactForce);
            }
        }

        UpdateAmmoUI(); // Update the ammo display UI
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = currentAmmo.ToString();
        }
        
        if (reserveAmmoText != null)
        {
            reserveAmmoText.text = reserveAmmo.ToString();
        }
    }

    private void UpdateFireModeUI()
    {
        if (fireModeText != null)
        {
            fireModeText.text = _fireMode == GunNamespace.FireMode.Single ? "SINGLE" : "BURST";
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
            animator.SetBool("isReloading", true);
        }

        SFXManager.instance.PlaySFXClip(reloadSound, transform, 1f); // Play reload sound
        
        yield return new WaitForSeconds(reloadTime);

        // Calculate how much ammo we need and can take from reserves
        int ammoNeeded = maxAmmo - currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, reserveAmmo);
        
        // Transfer ammo from reserves to magazine
        currentAmmo += ammoToReload;
        reserveAmmo -= ammoToReload;
        
        isReloading = false;
        
        // Update UI after reload
        UpdateAmmoUI();
        
        // Stop reload animation
        if (animator != null)
        {
            animator.SetBool("isReloading", false);
        }
        
        Debug.Log($"Reload complete. Magazine: {currentAmmo}/{maxAmmo}, Reserve: {reserveAmmo}");
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
            yield return new WaitForSeconds(0.05f);
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
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            isFiring = false;
        }
    }
}
