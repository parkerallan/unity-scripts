using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.AI;

public class oneBController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator bossAnimator;

    [Header("Boss AI Settings")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer, whatIsWater;

    [Header("Boss Settings")]
    public string bossName = "One-B";
    public bool isActive = false; // Boss AI is inactive until triggered
    public float activationDelay = 2f; // Delay after activation for dramatic effect

    [Header("Combat Stats")]
    public float timeBetweenAttacks = 1.5f; // Faster than regular enemies
    public float attackAccuracy = 0.85f; // Higher accuracy than regular enemies
    public float attackDamage = 35f; // Boss damage
    public LayerMask attackLayers = -1;

    [Header("Boss Phases")]
    public bool useHealthPhases = true;
    public float phase2HealthThreshold = 0.6f; // 60% health
    public float phase3HealthThreshold = 0.3f; // 30% health
    public float phase2AttackSpeedMultiplier = 1.3f;
    public float phase3AttackSpeedMultiplier = 1.6f;
    public float phase2AccuracyBonus = 0.05f;
    public float phase3AccuracyBonus = 0.1f;

    [Header("Movement")]
    public float sightRange = 25f; // Larger sight range
    public float attackRange = 15f; // Longer attack range
    public float circleStrafingRadius = 10f;
    public float strafingSpeed = 2f;
    public bool enableCircleStrafing = true;
    public float minCircleTime = 2f;
    public float maxCircleTime = 5f;

    [Header("Aggression System")]
    public float maxAggression = 100f;
    public float aggressionDecayRate = 3f;
    public float aggressionOnHit = 20f;
    public float aggressionThresholdForRush = 50f;
    public float rushSpeed = 12f;
    public float rushCooldown = 10f;
    public float rushChance = 0.25f;
    public float rushDuration = 4f;

    [Header("Water Surfing System")]
    public GameObject surfboard; // Surfboard GameObject to enable/disable
    public LayerMask waterLayer; // Layer to detect water contact
    public float waterCheckRadius = 1f; // Radius for water detection
    public float surfingSpeed = 8f; // Speed when surfing on water
    public bool enableWaterSurfing = true;

    [Header("Attack Effects")]
    public AudioSource attackSFX;
    public ParticleSystem muzzleFlash;
    public ParticleSystem casingEffect;

    [Header("Boss Specific Effects")]
    public AudioSource battleMusicTrigger;
    public ParticleSystem intimidationEffect; // Plays when boss activates
    public AudioClip activationSound;
    public AudioClip phaseChangeSound;

    [Header("One-B Specific Animations")]
    public bool useRandomAttackAnimations = true;
    public bool useRandomBlockAnimations = true;

    // Private variables
    private bool alreadyAttacked;
    private float currentAggression = 0f;
    private int currentPhase = 1;
    private Target bossTarget;
    private float originalTimeBetweenAttacks;
    private float originalAttackAccuracy;
    private bool playerInSightRange, playerInAttackRange;
    private bool isRushing = false;
    private float rushStartTime = 0f;
    private float strafingAngle = 0f;
    private float circlingTime = 0f;
    private bool isCircling = false;
    private Vector3 lastPlayerPosition;
    private float playerStationaryTime = 0f;
    private float lastRushTime = 0f;

    // Water surfing state variables
    private bool isOnWater = false;
    private bool isSurfing = false;

    private void Start()
    {
        // Auto-assign components if not manually set
        if (bossAnimator == null)
            bossAnimator = GetComponent<Animator>();

        // Boss AI initialization
        player = GameObject.Find("CharModel1").transform;
        
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
            
        bossTarget = GetComponent<Target>();
        
        // Store original values
        originalTimeBetweenAttacks = timeBetweenAttacks;
        originalAttackAccuracy = attackAccuracy;

        // Start inactive
        if (agent != null)
            agent.enabled = false;
            
        // Initialize tracking variables
        lastPlayerPosition = player != null ? player.position : Vector3.zero;

        // Ensure surfboard starts disabled
        if (surfboard != null)
        {
            surfboard.SetActive(false);
        }
    }

    private void Update()
    {
        // Don't update AI if not active or if dead
        if (!isActive) return;
        
        // Additional safety check - if health is 0 or below, ensure AI is disabled
        if (bossTarget != null && bossTarget.health <= 0)
        {
            isActive = false;
            return;
        }

        UpdateWaterDetection();
        UpdateRangeChecks();
        UpdateAggression();
        UpdatePhases();
        UpdateBehavior();
    }

    private void UpdateWaterDetection()
    {
        if (!enableWaterSurfing) return;

        // Check if boss is in contact with water layer
        bool wasOnWater = isOnWater;
        isOnWater = Physics.CheckSphere(transform.position, waterCheckRadius, waterLayer);

        // Handle transition into water
        if (isOnWater && !wasOnWater)
        {
            EnterWaterMode();
        }
        // Handle transition out of water
        else if (!isOnWater && wasOnWater)
        {
            ExitWaterMode();
        }
    }

    private void EnterWaterMode()
    {
        Debug.Log($"{bossName} entering water surfing mode!");
        isSurfing = true;

        // Enable surfboard
        if (surfboard != null)
        {
            surfboard.SetActive(true);
        }

        // Adjust agent settings for water movement
        if (agent != null && agent.enabled)
        {
            agent.speed = surfingSpeed;
        }

        // Trigger surfing animation state
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isSurfing", true);
        }
    }

    private void ExitWaterMode()
    {
        Debug.Log($"{bossName} exiting water surfing mode!");
        isSurfing = false;

        // Disable surfboard
        if (surfboard != null)
        {
            surfboard.SetActive(false);
        }

        // Reset agent settings for ground movement
        if (agent != null && agent.enabled)
        {
            agent.speed = 3.5f; // Default ground speed
        }

        // Exit surfing animation state
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isSurfing", false);
        }
    }

    private void UpdateRangeChecks()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Use distance-based detection for more reliable detection
        playerInSightRange = distanceToPlayer <= sightRange;
        playerInAttackRange = distanceToPlayer <= attackRange;
        
        // Track if player is moving or stationary
        if (Vector3.Distance(player.position, lastPlayerPosition) < 0.5f)
        {
            playerStationaryTime += Time.deltaTime;
        }
        else
        {
            playerStationaryTime = 0f;
            lastPlayerPosition = player.position;
        }
    }

    private void UpdateAggression()
    {
        // Decay aggression over time
        if (currentAggression > 0)
        {
            currentAggression -= aggressionDecayRate * Time.deltaTime;
            currentAggression = Mathf.Max(0, currentAggression);
        }
        
        // Gradually build aggression over time when player is in sight
        if (playerInSightRange && !playerInAttackRange)
        {
            currentAggression += 3f * Time.deltaTime;
            currentAggression = Mathf.Min(maxAggression, currentAggression);
        }
    }

    private void UpdatePhases()
    {
        if (!useHealthPhases || bossTarget == null) return;

        float healthPercentage = bossTarget.health / bossTarget.maxHealth;
        int newPhase = 1;

        if (healthPercentage <= phase3HealthThreshold)
            newPhase = 3;
        else if (healthPercentage <= phase2HealthThreshold)
            newPhase = 2;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            OnPhaseChange();
        }
    }

    private void OnPhaseChange()
    {
        // Play phase change sound
        if (phaseChangeSound != null && attackSFX != null)
        {
            attackSFX.PlayOneShot(phaseChangeSound);
        }

        // Adjust stats based on phase
        switch (currentPhase)
        {
            case 2:
                timeBetweenAttacks = originalTimeBetweenAttacks / phase2AttackSpeedMultiplier;
                attackAccuracy = Mathf.Min(1f, originalAttackAccuracy + phase2AccuracyBonus);
                break;
            case 3:
                timeBetweenAttacks = originalTimeBetweenAttacks / phase3AttackSpeedMultiplier;
                attackAccuracy = Mathf.Min(1f, originalAttackAccuracy + phase3AccuracyBonus);
                break;
        }

        // Add aggression when phase changes
        AddAggression(35f);
    }

    private void UpdateBehavior()
    {
        if (playerInSightRange)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // Update circling timer
            if (isCircling)
            {
                circlingTime += Time.deltaTime;
            }
            
            // Check if we should randomly decide to rush
            bool canRush = Time.time - lastRushTime >= rushCooldown && 
                          currentAggression >= aggressionThresholdForRush &&
                          !playerInAttackRange &&
                          !isRushing;
            
            // Random rush decision
            if (canRush && Random.Range(0f, 1f) < rushChance * Time.deltaTime)
            {
                isRushing = true;
                rushStartTime = Time.time;
                lastRushTime = Time.time;
                isCircling = false;
                circlingTime = 0f;
            }
            
            // Also trigger rush if circled too long
            if (circlingTime >= maxCircleTime)
            {
                isRushing = true;
                rushStartTime = Time.time;
                lastRushTime = Time.time;
                isCircling = false;
                circlingTime = 0f;
            }
            
            // Check if rush should end based on timer
            if (isRushing && Time.time - rushStartTime >= rushDuration)
            {
                isRushing = false;
                isCircling = true;
                circlingTime = 0f;
            }
            
            // Decision making for behavior
            if (playerInAttackRange && !alreadyAttacked)
            {
                AttackPlayer();
                isCircling = false;
                circlingTime = 0f;
            }
            else if (isRushing)
            {
                RushPlayer();
            }
            else if (enableCircleStrafing && !playerInAttackRange)
            {
                StopRunningAnimation();
                CircleStrafing();
                if (!isCircling)
                {
                    isCircling = true;
                    circlingTime = 0f;
                }
            }
            else
            {
                StopRunningAnimation();
                ChasePlayer();
                isCircling = false;
                circlingTime = 0f;
            }
        }
        else
        {
            PlayIdleAnimation();
            StopWalkAnimation();
            StopRunningAnimation();
            isRushing = false;
            isCircling = false;
            circlingTime = 0f;
        }
    }

    private void CircleStrafing()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Adjust strafing behavior based on surfing state
        float strafingRotationSpeed = 0.8f * Time.deltaTime;
        if (isSurfing)
        {
            strafingRotationSpeed *= 1.3f; // Faster rotation when surfing
        }
        
        strafingAngle += strafingRotationSpeed;
        
        Vector3 offset = new Vector3(
            Mathf.Sin(strafingAngle) * circleStrafingRadius,
            0,
            Mathf.Cos(strafingAngle) * circleStrafingRadius
        );
        
        Vector3 targetPosition = player.position + offset;
        
        // Ensure the target position is accessible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 8f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Use appropriate speed based on state
        float currentSpeed = isSurfing ? surfingSpeed : 5f;
        agent.speed = currentSpeed;
        agent.updateRotation = false;
        agent.SetDestination(targetPosition);

        // Always face the player while strafing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // Play appropriate movement animation
        if (isSurfing)
        {
            PlaySurfingAnimation();
        }
        else
        {
            PlayWalkAnimation();
        }
    }

    private void RushPlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Rush with appropriate speed based on state
        float currentRushSpeed = isSurfing ? surfingSpeed * 1.5f : rushSpeed;
        agent.speed = currentRushSpeed;
        agent.updateRotation = false;
        
        agent.SetDestination(player.position);
        
        // Always face directly at player while rushing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
        
        // Play appropriate rush animation
        if (bossAnimator != null)
        {
            if (isSurfing)
            {
                bossAnimator.SetBool("isSurfing", true);
                // Surfing rush uses same surfing animation but faster
            }
            else
            {
                bossAnimator.SetBool("isRunning", true);
            }
        }
        
        // If rush has ended, transition back to appropriate animation
        if (!isRushing && bossAnimator != null)
        {
            bossAnimator.SetBool("isRunning", false);
            if (isSurfing)
            {
                bossAnimator.SetBool("isSurfing", true);
            }
        }
    }

    private void ChasePlayer()
    {
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        Vector3 chaseTarget = player.position;
        
        // Add slight offset for less predictable movement
        Vector3 randomOffset = new Vector3(
            Random.Range(-1f, 1f),
            0,
            Random.Range(-1f, 1f)
        ) * 0.5f;
        
        chaseTarget += randomOffset;
        
        // Ensure the target position is accessible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(chaseTarget, out hit, 3f, NavMesh.AllAreas))
        {
            chaseTarget = hit.position;
        }
        
        // Use appropriate speed and animation based on state
        if (isSurfing)
        {
            agent.speed = surfingSpeed;
            PlaySurfingAnimation();
        }
        else
        {
            agent.speed = 4.5f;
            PlayWalkAnimation();
        }
        
        agent.updateRotation = false;
        agent.SetDestination(chaseTarget);
        
        // Always face towards player while chasing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
        }
    }

    private void AttackPlayer()
    {
        // Stop moving immediately
        if (agent.isOnNavMesh && agent.enabled)
        {
            agent.SetDestination(transform.position);
        }

        // Stop all movement animations
        StopWalkAnimation();
        StopRunningAnimation();

        // Look at player
        if (player != null)
        {
            transform.LookAt(player);
        }

        // Use different attacks based on state
        if (bossAnimator != null)
        {
            if (isSurfing)
            {
                // Water attacks - use "Throw" animation
                bossAnimator.SetTrigger("Throw");
            }
            else
            {
                // Ground attacks - use random ground attack animations
                if (isRushing)
                {
                    bossAnimator.SetTrigger("Slash2");
                    isRushing = false;
                    bossAnimator.SetBool("isRunning", false);
                }
                else if (useRandomAttackAnimations)
                {
                    int randomAttack = Random.Range(1, 4);
                    bossAnimator.SetTrigger($"Slash{randomAttack}");
                }
                else
                {
                    bossAnimator.SetTrigger("Slash1");
                }
            }
        }
        
        // Perform the actual attack
        PerformRaycastAttack();
        
        // Set attack cooldown
        alreadyAttacked = true;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }

    private void PerformRaycastAttack()
    {
        if (player == null) return;

        // Play attack sound effect
        if (attackSFX != null)
        {
            attackSFX.Play();
        }

        // Play muzzle flash effect (for both water and ground attacks)
        if (muzzleFlash != null)
        {
            muzzleFlash.Play();
        }

        // Play bullet casing ejection effect
        if (casingEffect != null)
        {
            casingEffect.transform.position = transform.position + Vector3.up * 1.5f + transform.right * 0.3f;
            casingEffect.Play();
        }

        // Calculate attack direction with accuracy variation
        Vector3 baseDirection = (player.position - transform.position).normalized;
        
        float hitRoll = Random.Range(0f, 1f);
        Vector3 attackDirection;

        if (hitRoll <= attackAccuracy)
        {
            // Accurate shot with minimal deviation
            Vector3 deviation = new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f)
            );
            attackDirection = (baseDirection + deviation).normalized;
        }
        else
        {
            // Miss shot with more deviation
            Vector3 missDeviation = new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.4f, 0.4f)
            );
            attackDirection = (baseDirection + missDeviation).normalized;
        }

        // Perform raycast attack
        Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
        RaycastHit hit;
        float maxAttackRange = 50f;

        if (Physics.Raycast(attackRay, out hit, maxAttackRange, attackLayers))
        {
            if (hit.collider.CompareTag("Player"))
            {
                var playerTarget = hit.collider.GetComponent<Target>();
                if (playerTarget != null)
                {
                    playerTarget.TakeDamage(attackDamage);
                }
            }
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    // Animation Methods - Different animations for water vs ground
    private void PlaySurfingAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isSurfing", true);
        }
    }

    private void PlayWalkAnimation()
    {
        if (bossAnimator != null)
        {
            if (isSurfing)
            {
                bossAnimator.SetBool("isSurfing", true);
            }
            else
            {
                bossAnimator.SetBool("isWalking", true);
            }
        }
    }

    private void StopWalkAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isWalking", false);
            if (!isSurfing)
            {
                bossAnimator.SetBool("isSurfing", false);
            }
        }
    }

    private void StopRunningAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isRunning", false);
        }
    }

    private void PlayIdleAnimation()
    {
        if (bossAnimator != null)
        {
            // Set idle state based on current mode
            if (isSurfing)
            {
                bossAnimator.SetBool("isSurfing", true);
            }
            else
            {
                bossAnimator.SetBool("isWalking", false);
                bossAnimator.SetBool("isRunning", false);
            }
        }
    }

    public void PlayDeathAnimation()
    {
        // Immediately disable AI to prevent interference with death animation
        isActive = false;
        
        // Stop the NavMeshAgent to prevent movement during death
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        // Disable surfboard if active
        if (surfboard != null)
        {
            surfboard.SetActive(false);
        }
        
        // Stop all movement animations before playing death
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isSurfing", false);
            bossAnimator.SetBool("isWalking", false);
            bossAnimator.SetBool("isRunning", false);
            
            // Reset any attack triggers that might be active
            bossAnimator.ResetTrigger("Slash1");
            bossAnimator.ResetTrigger("Slash2");
            bossAnimator.ResetTrigger("Slash3");
            bossAnimator.ResetTrigger("Throw");
            bossAnimator.ResetTrigger("Block1");
            bossAnimator.ResetTrigger("Block2");
            bossAnimator.ResetTrigger("Block3");
            
            // Now trigger the death animation
            bossAnimator.SetTrigger("Death");
        }
        
        // Cancel any pending invokes that might interfere
        CancelInvoke();
        
        Debug.Log($"Boss {bossName} death animation triggered");
    }

    /// <summary>
    /// Compatibility method for BossAIController (lowercase version)
    /// </summary>
    public void playDeathAnimation()
    {
        PlayDeathAnimation();
    }

    /// <summary>
    /// Call this method to activate the boss AI after dialogue completion
    /// </summary>
    public void ActivateBoss()
    {
        // Play activation sound
        if (activationSound != null && attackSFX != null)
        {
            attackSFX.PlayOneShot(activationSound);
        }

        // Play intimidation effect
        if (intimidationEffect != null)
        {
            intimidationEffect.Play();
        }

        // Start battle music
        if (battleMusicTrigger != null)
        {
            battleMusicTrigger.Play();
        }

        // Activate AI after delay for dramatic effect
        Invoke(nameof(DelayedActivation), activationDelay);
    }

    private void DelayedActivation()
    {
        isActive = true;
        if (agent != null)
        {
            agent.enabled = true;
            
            // Ensure agent has proper speed settings
            if (agent.speed <= 0)
            {
                agent.speed = 3.5f; // Default movement speed
            }
        }
    }

    /// <summary>
    /// Call this method when the boss is hit by the player
    /// </summary>
    public void OnHitByPlayer()
    {
        AddAggression(aggressionOnHit);
        
        // Play hit reaction animation (block animation)
        PlayHitReactionAnimation();
    }
    
    /// <summary>
    /// Play hit reaction animation using block triggers
    /// </summary>
    private void PlayHitReactionAnimation()
    {
        if (bossAnimator != null)
        {
            if (useRandomBlockAnimations)
            {
                int randomBlock = Random.Range(1, 4);
                bossAnimator.SetTrigger($"Block{randomBlock}");
            }
            else
            {
                bossAnimator.SetTrigger("Block1");
            }
        }
    }

    /// <summary>
    /// Add aggression to the boss
    /// </summary>
    public void AddAggression(float amount)
    {
        currentAggression = Mathf.Min(maxAggression, currentAggression + amount);
    }

    /// <summary>
    /// Deactivate the boss AI
    /// </summary>
    public void DeactivateBoss()
    {
        isActive = false;
        if (agent != null)
        {
            agent.enabled = false;
        }
        
        // Disable surfboard if active
        if (surfboard != null)
        {
            surfboard.SetActive(false);
        }
    }

    /// <summary>
    /// Manually set surfing state (for external triggers)
    /// </summary>
    public void SetSurfingState(bool surfing)
    {
        if (surfing && !isSurfing)
        {
            EnterWaterMode();
        }
        else if (!surfing && isSurfing)
        {
            ExitWaterMode();
        }
    }

    /// <summary>
    /// Get current surfing state
    /// </summary>
    public bool IsSurfing()
    {
        return isSurfing;
    }

    /// <summary>
    /// Call this method when the boss dies to properly handle death state
    /// </summary>
    public void OnBossDeath()
    {
        Debug.Log($"Boss {bossName} is dying - triggering death sequence");
        
        // Immediately stop all AI behavior
        isActive = false;
        
        // Stop any ongoing rushes or circles
        isRushing = false;
        isCircling = false;
        
        // Cancel all pending invokes
        CancelInvoke();
        
        // Play the death animation
        PlayDeathAnimation();
    }

    // Gizmos for debugging
    private void OnDrawGizmosSelected()
    {
        // Draw sight range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sightRange);

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw circle strafing radius
        if (enableCircleStrafing)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, circleStrafingRadius);
        }

        // Draw water detection radius
        if (enableWaterSurfing)
        {
            Gizmos.color = isSurfing ? Color.cyan : Color.white;
            Gizmos.DrawWireSphere(transform.position, waterCheckRadius);
        }
    }
}