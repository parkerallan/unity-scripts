using UnityEngine;

public class UnderwaterPlayerController : MonoBehaviour
{
    [Header("Animator Controllers")]
    public RuntimeAnimatorController normalController;
    public RuntimeAnimatorController underwaterController;

    [Header("Player References")]
    public Animator playerAnimator;
    public Transform playerChest; // Assign the player's chest or root transform in the Inspector

    private BoxCollider boxCollider;
    private bool isSwimming = false;
    private bool isDiving = false;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && !boxCollider.isTrigger)
            boxCollider.isTrigger = true;
    }

    private void Update()
    {
        if (playerAnimator == null || normalController == null || underwaterController == null || playerChest == null || boxCollider == null)
            return;

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
}