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
    public GameObject characterImpactEffect;
    public GameObject environmentImpactEffect;
    public float impactForce = 30f;

    [Header("Weapon Settings")]
    public GameObject gunModel; // Reference to the gun's visual model
    public bool isGunActive = false; // Tracks if the gun is active

    [Header("Camera Priority Settings")]
    [SerializeField] private int aimCameraPriority = 15;
    [SerializeField] private int freeLookCameraPriority = 10;

    [Header("Ammo Settings")]
    public int maxAmmo = 30; // Maximum ammo in the magazine
    public int currentAmmo; // Current ammo in the magazine
    public float reloadTime = 2f; // Time it takes to reload
    public bool isReloading = false;

    public bool isFiring = false;
    public Coroutine burstCoroutine;
    [Header("SFX")]
    public AudioClip gunshotSound;
    public AudioClip reloadSound;

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

    }

    void Update()
    {
        // Handle weapon toggle with the "1" key
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ToggleGun();
        }

        // If the gun is not active, skip all other logic
        if (!isGunActive) return;

        // Reload mechanic
        if (isReloading) return; // Skip shooting logic while reloading
        if (Input.GetKeyDown(KeyCode.R))
        {
            StartCoroutine(Reload());
            return;
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            _fireMode = (_fireMode == GunNamespace.FireMode.Single) ? GunNamespace.FireMode.Burst : GunNamespace.FireMode.Single;
            Debug.Log("GunScript: Fire mode set to " + _fireMode);
        }

        // When right mouse button is pressed down, switch to the aim camera
        if (Input.GetKey(KeyCode.Mouse1))
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

            // Handle Q and E input
            if (Input.GetKeyDown(KeyCode.Q) && !isFiring)
            {
                burstCoroutine = StartCoroutine(BurstFireCoroutine());
            }
            else if (Input.GetKeyDown(KeyCode.E) && !isFiring)
            {
                burstCoroutine = StartCoroutine(BurstFireCoroutine());
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

    void ToggleGun()
    {
        isGunActive = !isGunActive;
        UpdateGunVisibility();
        Debug.Log("GunScript: Gun is now " + (isGunActive ? "active" : "inactive"));
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

        SFXManager.instance.PlaySFXClip(reloadSound, transform, 1f); // Play reload sound
        
        yield return new WaitForSeconds(reloadTime);

        currentAmmo = maxAmmo;
        isReloading = false;
        Debug.Log("Reload complete. Ammo refilled to " + currentAmmo);
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

    void OnDisable()
    {
        if (burstCoroutine != null)
        {
            StopCoroutine(burstCoroutine);
            isFiring = false;
        }
    }
}
