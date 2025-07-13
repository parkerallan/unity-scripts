using UnityEngine;
using System.Collections.Generic;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon References")]
    public List<GameObject> weapons = new List<GameObject>(); // List of all weapon GameObjects
    public List<MonoBehaviour> weaponScripts = new List<MonoBehaviour>(); // List of weapon scripts (GunScript, RifleScript, etc.)
    
    [Header("Weapon Management")]
    public int currentWeaponIndex = -1; // -1 means no weapon is active
    public bool allowScrollSwitching = true;
    public bool allowNumberKeySwitching = true;
    
    [Header("Input Settings")]
    public KeyCode[] weaponKeys = { KeyCode.Alpha1, KeyCode.Alpha2 }; // Keys for direct weapon selection
    
    private void Start()
    {
        // Initialize weapon system
        InitializeWeapons();
        
        // Start with no weapon active by default
        DeactivateAllWeapons();
    }
    
    private void Update()
    {
        HandleWeaponSwitching();
    }
    
    void InitializeWeapons()
    {
        // Auto-populate weapon scripts if not manually assigned
        if (weaponScripts.Count == 0)
        {
            // Find all weapon scripts in children
            GunScript gunScript = GetComponentInChildren<GunScript>();
            RifleScript rifleScript = GetComponentInChildren<RifleScript>();
            
            if (gunScript != null) weaponScripts.Add(gunScript);
            if (rifleScript != null) weaponScripts.Add(rifleScript);
        }
        
        // Auto-populate weapon GameObjects if not manually assigned
        if (weapons.Count == 0)
        {
            foreach (var script in weaponScripts)
            {
                weapons.Add(script.gameObject);
            }
        }
        
        Debug.Log($"WeaponManager: Initialized with {weapons.Count} weapons");
    }
    
    void HandleWeaponSwitching()
    {
        // Handle scroll wheel switching
        if (allowScrollSwitching)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                SwitchToNextWeapon();
            }
        }
        
        // Handle number key switching
        if (allowNumberKeySwitching)
        {
            for (int i = 0; i < weaponKeys.Length && i < weapons.Count; i++)
            {
                if (Input.GetKeyDown(weaponKeys[i]))
                {
                    ToggleWeapon(i);
                }
            }
        }
    }
    
    public void SwitchToNextWeapon()
    {
        if (weapons.Count == 0) return;
        
        // Cycle through weapons, with -1 (no weapon) as the last position
        int nextIndex = currentWeaponIndex + 1;
        if (nextIndex >= weapons.Count)
        {
            nextIndex = -1; // No weapon equipped
        }
        
        SwitchToWeapon(nextIndex);
    }
    
    public void SwitchToPreviousWeapon()
    {
        if (weapons.Count == 0) return;
        
        int previousIndex = currentWeaponIndex - 1;
        if (previousIndex < -1) previousIndex = weapons.Count - 1;
        SwitchToWeapon(previousIndex);
    }
    
    public void SwitchToWeapon(int weaponIndex)
    {
        // Allow -1 for no weapon equipped
        if (weaponIndex < -1 || weaponIndex >= weapons.Count) return;
        if (weaponIndex == currentWeaponIndex) return; // Already active
        
        // Deactivate current weapon
        if (currentWeaponIndex >= 0)
        {
            DeactivateWeapon(currentWeaponIndex);
        }
        
        // Activate new weapon (if not -1)
        if (weaponIndex >= 0)
        {
            ActivateWeapon(weaponIndex);
        }
        
        currentWeaponIndex = weaponIndex;
        
        if (weaponIndex == -1)
        {
            Debug.Log("WeaponManager: No weapon equipped");
        }
        else
        {
            Debug.Log($"WeaponManager: Switched to weapon {weaponIndex}");
        }
    }
    
    // Toggle weapon on/off - if weapon is active, deactivate it; if not active, activate it
    public void ToggleWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count) return;
        
        if (currentWeaponIndex == weaponIndex)
        {
            // Weapon is currently active, deactivate it
            SwitchToWeapon(-1); // Switch to no weapon
        }
        else
        {
            // Weapon is not active, activate it
            SwitchToWeapon(weaponIndex);
        }
    }
    
    void ActivateWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count) return;
        
        // Activate the weapon GameObject
        if (weapons[weaponIndex] != null)
        {
            weapons[weaponIndex].SetActive(true);
        }
        
        // Call the appropriate activation method on the weapon script
        var weaponScript = weaponScripts[weaponIndex];
        if (weaponScript is GunScript gunScript)
        {
            gunScript.ActivateGun();
        }
        else if (weaponScript is RifleScript rifleScript)
        {
            rifleScript.ActivateRifle();
        }
        
        // Trigger weapon swap animation
        Animator animator = GetComponent<Animator>();
        if (animator != null)
        {
            if (weaponScript is GunScript)
            {
                animator.SetTrigger("isSwapping");
            }
            else if (weaponScript is RifleScript)
            {
                animator.SetTrigger("isSwappingRifle");
            }
        }
    }
    
    void DeactivateWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count) return;
        
        // Call the appropriate deactivation method on the weapon script
        var weaponScript = weaponScripts[weaponIndex];
        if (weaponScript is GunScript gunScript)
        {
            gunScript.DeactivateGun();
        }
        else if (weaponScript is RifleScript rifleScript)
        {
            rifleScript.DeactivateRifle();
        }
    }
    
    public void DeactivateAllWeapons()
    {
        for (int i = 0; i < weapons.Count; i++)
        {
            DeactivateWeapon(i);
        }
        currentWeaponIndex = -1;
    }
    
    // Utility methods for external scripts
    public bool IsWeaponActive(int weaponIndex)
    {
        return currentWeaponIndex == weaponIndex;
    }
    
    public bool HasWeaponEquipped()
    {
        return currentWeaponIndex >= 0;
    }
    
    public GameObject GetCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            return weapons[currentWeaponIndex];
        }
        return null;
    }
    
    public MonoBehaviour GetCurrentWeaponScript()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weaponScripts.Count)
        {
            return weaponScripts[currentWeaponIndex];
        }
        return null;
    }
    
    public int GetWeaponCount()
    {
        return weapons.Count;
    }
    
    // Method to add new weapons at runtime (for inventory expansion)
    public void AddWeapon(GameObject weaponObject, MonoBehaviour weaponScript, KeyCode keyBinding = KeyCode.None)
    {
        weapons.Add(weaponObject);
        weaponScripts.Add(weaponScript);
        
        if (keyBinding != KeyCode.None)
        {
            // Expand the weapon keys array
            KeyCode[] newKeys = new KeyCode[weaponKeys.Length + 1];
            weaponKeys.CopyTo(newKeys, 0);
            newKeys[weaponKeys.Length] = keyBinding;
            weaponKeys = newKeys;
        }
        
        Debug.Log($"WeaponManager: Added new weapon. Total weapons: {weapons.Count}");
    }
    
    // Method to remove weapons (for inventory management)
    public void RemoveWeapon(int weaponIndex)
    {
        if (weaponIndex < 0 || weaponIndex >= weapons.Count) return;
        
        // If we're removing the currently active weapon, deactivate it first
        if (weaponIndex == currentWeaponIndex)
        {
            DeactivateWeapon(weaponIndex);
            currentWeaponIndex = -1;
        }
        else if (weaponIndex < currentWeaponIndex)
        {
            // Adjust current weapon index if we're removing a weapon before it
            currentWeaponIndex--;
        }
        
        weapons.RemoveAt(weaponIndex);
        weaponScripts.RemoveAt(weaponIndex);
        
        Debug.Log($"WeaponManager: Removed weapon. Total weapons: {weapons.Count}");
    }
}
