using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.AI;

public class oneCController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator bossAnimator;

    [Header("Constraint Settings")]
    public ParentConstraint parentConstraint;

    [Header("Optional Settings")]
    public bool playSwapSoundEffect = true;
    public AudioClip swapSoundClip;

    [Header("Boss AI Settings")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Boss Settings")]
    public string bossName = "One-C";
    public bool isActive = false; // Boss AI is inactive until triggered
    public float activationDelay = 2f; // Delay after activation for dramatic effect

    [Header("Combat Stats")]
    public float timeBetweenAttacks = 1.2f; // Even faster than One-B
    public float attackAccuracy = 0.9f; // Higher accuracy - this is the final boss
    public float attackDamage = 50f; // Higher damage
    public LayerMask attackLayers = -1;

    [Header("Boss Phases")]
    public bool useHealthPhases = true;
    public float phase2HealthThreshold = 0.6f; // 60% health
    public float phase3HealthThreshold = 0.3f; // 30% health
    public float phase2AttackSpeedMultiplier = 1.4f;
    public float phase3AttackSpeedMultiplier = 1.8f;
    public float phase2AccuracyBonus = 0.05f;
    public float phase3AccuracyBonus = 0.1f;

    [Header("Movement")]
    public float sightRange = 30f; // Larger sight range than One-B
    public float attackRange = 12f; // Shorter attack range to encourage more movement
    public float circleStrafingRadius = 15f; // Larger radius for wider circles
    public float strafingSpeed = 2f; // Slower for more controlled movement
    public bool enableCircleStrafing = true;
    public float minCircleTime = 1.5f; // Reduced for more aggressive flying attacks
    public float maxCircleTime = 3f; // Much shorter - attack more frequently when flying

    [Header("Aggression System")]
    public float maxAggression = 100f;
    public float aggressionDecayRate = 2f; // Faster decay so it builds up easier
    public float aggressionOnHit = 25f; // Gets angrier when hit
    public float aggressionThresholdForRush = 40f; // Lower threshold - rush more often
    public float rushSpeed = 18f; // Much faster rush speed
    public float rushCooldown = 8f; // Time between possible rushes
    public float rushChance = 0.3f; // 30% chance to rush when conditions are met
    public float rushDuration = 3f; // How long to rush before transitioning back

    [Header("Attack Effects")]
    public AudioClip slash1SFXClip;
    public AudioClip slash2SFXClip;
    public AudioClip slash3SFXClip;
    public AudioClip blockSFXClip; // Same clip used for all block animations
    public float attackSFXVolume = 1f;
    public ParticleSystem muzzleFlash;
    public ParticleSystem casingEffect;

    [Header("Boss Specific Effects")]
    public AudioSource battleMusicTrigger;
    public ParticleSystem intimidationEffect; // Plays when boss activates
    public AudioClip activationSound;
    public AudioClip phaseChangeSound;

    [Header("One-C Specific Animations")]
    public bool useRandomAttackAnimations = true;
    public bool useRandomBlockAnimations = true;

    [Header("Flying Transformation")]
    public bool enableFlyingTransformation = true;
    public Vector3 flyingPositionOffset = new Vector3(0, 0.5f, 0); // How high to float
    public Vector3 flyingRotationOffset = new Vector3(0, 0, 0); // Additional rotation while flying
    public Vector3 flyingScaleMultiplier = new Vector3(1.1f, 1.1f, 1.1f); // Scale change while flying
    public float transformationSpeed = 3f; // Speed of transformation
    public AnimationCurve transformationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Smoothing curve

    [Header("Flying Aggression")]
    public float flyingAggressionMultiplier = 1.5f; // Extra aggression when flying
    public float flyingAttackRangeBonus = 3f; // Extended attack range when flying
    public float flyingCircleSpeedMultiplier = 1.3f; // Faster circling when flying

    // Private variables
    private bool alreadyAttacked;
    private float currentAggression = 0f;
    private int currentPhase = 1;
    private Target bossTarget;
    private float originalTimeBetweenAttacks;
    private float originalAttackAccuracy;
    private bool playerInSightRange, playerInAttackRange;
    private bool isRushing = false;
    private float rushStartTime = 0f; // Track when rush started
    private float strafingAngle = 0f;
    private float circlingTime = 0f;
    private bool isCircling = false;
    private Vector3 lastPlayerPosition;
    private float playerStationaryTime = 0f;
    private float lastRushTime = 0f; // Track when we last rushed

    // Flying transformation variables
    private bool isCurrentlyFlying = false;
    private Vector3 originalScale;
    private float transformationProgress = 0f;

    private void Start()
    {
        // Auto-assign components if not manually set
        if (bossAnimator == null)
            bossAnimator = GetComponent<Animator>();

        if (parentConstraint == null)
            parentConstraint = GetComponent<ParentConstraint>();

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

        // Store original scale for flying transformation
        originalScale = transform.localScale;
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

        UpdateRangeChecks();
        UpdateAggression();
        UpdatePhases();
        UpdateBehavior();
        UpdateFlyingTransformation();
    }

    private void UpdateRangeChecks()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Use distance-based detection instead of Physics.CheckSphere for more reliable detection
        playerInSightRange = distanceToPlayer <= sightRange;
        
        // Extended attack range when flying for more aggressive behavior
        float currentAttackRange = attackRange;
        if (IsFlying())
        {
            currentAttackRange += flyingAttackRangeBonus;
        }
        
        playerInAttackRange = distanceToPlayer <= currentAttackRange;
        
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
            float aggressionRate = 2f * Time.deltaTime; // Base aggression build rate
            
            // Build aggression faster when flying for more aggressive behavior
            if (IsFlying())
            {
                aggressionRate *= flyingAggressionMultiplier;
            }
            
            currentAggression += aggressionRate;
            currentAggression = Mathf.Min(maxAggression, currentAggression);
        }
    }

    private void UpdatePhases()
    {
        if (!useHealthPhases || bossTarget == null) return;

        float healthPercentage = bossTarget.health / bossTarget.maxHealth; // Use actual max health
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
        // Play phase change sound using SFXManager
        if (phaseChangeSound != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(phaseChangeSound, transform, attackSFXVolume);
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
        AddAggression(40f);
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
                          !playerInAttackRange && // Don't rush if already close
                          !isRushing; // Don't re-trigger while already rushing
            
            // Random rush decision
            if (canRush && Random.Range(0f, 1f) < rushChance * Time.deltaTime)
            {
                isRushing = true;
                rushStartTime = Time.time; // Record when rush started
                lastRushTime = Time.time;
                isCircling = false;
                circlingTime = 0f;
            }
            
            // Also trigger rush if circled too long
            if (circlingTime >= maxCircleTime)
            {
                isRushing = true;
                rushStartTime = Time.time; // Record when rush started
                lastRushTime = Time.time;
                isCircling = false;
                circlingTime = 0f;
            }
            
            // Check if rush should end based on timer
            if (isRushing && Time.time - rushStartTime >= rushDuration)
            {
                isRushing = false;
                // Transition back to circle strafing
                isCircling = true;
                circlingTime = 0f;
            }
            
            // Decision making for behavior
            if (playerInAttackRange && !alreadyAttacked)
            {
                // Attack only when in range and not on cooldown
                AttackPlayer();
                isCircling = false;
                circlingTime = 0f;
            }
            else if (isRushing)
            {
                // Continue rushing until we get close
                RushPlayer();
                // Stop rushing when we get close enough to attack
                if (distanceToPlayer <= attackRange * 1.1f)
                {
                    // Don't stop rushing here - let AttackPlayer() handle it
                    // This ensures we transition smoothly to attack
                }
            }
            else if (enableCircleStrafing && !playerInAttackRange)
            {
                // Circle strafe when not attacking or rushing
                StopRunningAnimation(); // Stop running when not rushing
                CircleStrafing();
                if (!isCircling)
                {
                    isCircling = true;
                    circlingTime = 0f;
                }
            }
            else
            {
                // Default chase behavior
                StopRunningAnimation(); // Stop running when not rushing
                ChasePlayer();
                isCircling = false;
                circlingTime = 0f;
            }
        }
        else
        {
            PlayIdleAnimation();
            StopWalkAnimation(); // Stop walking when idle
            StopRunningAnimation(); // Stop running when idle
            isRushing = false;
            isCircling = false;
            circlingTime = 0f;
        }
    }

    private void CircleStrafing()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Faster circle strafing when flying for more aggressive behavior
        float strafingRotationSpeed = 0.8f * Time.deltaTime;
        if (IsFlying())
        {
            strafingRotationSpeed *= flyingCircleSpeedMultiplier;
        }
        
        // Smooth circle strafing with consistent radius
        strafingAngle += strafingRotationSpeed; // Dynamic rotation speed
        
        // Use consistent radius for smooth movement - no oscillation
        float currentRadius = circleStrafingRadius;
        
        Vector3 offset = new Vector3(
            Mathf.Sin(strafingAngle) * currentRadius,
            0,
            Mathf.Cos(strafingAngle) * currentRadius
        );
        
        Vector3 targetPosition = player.position + offset;
        
        // Ensure the target position is accessible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 8f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Faster movement when flying for more aggressive positioning
        float currentSpeed = 5f;
        if (IsFlying())
        {
            currentSpeed *= flyingCircleSpeedMultiplier;
        }
        
        agent.speed = currentSpeed;
        agent.updateRotation = false; // Prevent NavMeshAgent from controlling rotation
        agent.SetDestination(targetPosition);

        // Always face the player while strafing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Keep rotation on Y-axis only
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }

        // Play walk animation while circle strafing
        PlayWalkAnimation();
    }

    private void RushPlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Rush directly at the player with very high speed
        agent.speed = rushSpeed;
        agent.updateRotation = false; // Prevent NavMeshAgent from controlling rotation
        
        // Set destination directly to player for aggressive rush
        agent.SetDestination(player.position);
        
        // Always face directly at player while rushing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Keep rotation on Y-axis only
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
        
        // Set running animation but NO attack trigger while rushing
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isFlying2", false);
            bossAnimator.SetBool("isRunning", true);
        }
        
        // If rush has ended, transition back to walking animation
        if (!isRushing && bossAnimator != null)
        {
            bossAnimator.SetBool("isRunning", false);
            bossAnimator.SetBool("isFlying2", true);
        }
    }

    private void ChasePlayer()
    {
        if (player == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        // Chase with slight randomization to avoid predictable movement
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
        
        // Set moderate chase speed
        agent.speed = 4.5f;
        agent.updateRotation = false; // Prevent NavMeshAgent from controlling rotation
        agent.SetDestination(chaseTarget);
        
        // Always face towards player while chasing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Keep rotation on Y-axis only
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);
        }
        
        // Play walk animation while chasing
        PlayWalkAnimation();
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

        // Attack immediately every time, no conditions
        string attackTrigger = "";
        if (bossAnimator != null)
        {
            if (isRushing)
            {
                attackTrigger = "Slash2";
                bossAnimator.SetTrigger("Slash2");
                isRushing = false; // End rush state when attacking
                // Transition back to normal movement animations
                bossAnimator.SetBool("isRunning", false);
            }
            else
            {
                // Prefer Slash1 when flying for more aggressive aerial attacks
                if (IsFlying())
                {
                    attackTrigger = "Slash1";
                    bossAnimator.SetTrigger("Slash1"); // Consistent aggressive flying attack
                }
                else if (useRandomAttackAnimations)
                {
                    int randomAttack = Random.Range(1, 4);
                    attackTrigger = $"Slash{randomAttack}";
                    bossAnimator.SetTrigger($"Slash{randomAttack}");
                }
                else
                {
                    attackTrigger = "Slash1";
                    bossAnimator.SetTrigger("Slash1");
                }
            }
        }
        
        // Do damage and play appropriate sound
        PerformRaycastAttack(attackTrigger);
        
        // Set attack cooldown
        alreadyAttacked = true;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }

    private void PerformRaycastAttack(string attackType = "Slash1")
    {
        if (player == null) return;

        // Play appropriate attack sound effect using SFXManager
        if (SFXManager.instance != null)
        {
            AudioClip clipToPlay = null;
            
            switch (attackType)
            {
                case "Slash1":
                    clipToPlay = slash1SFXClip;
                    break;
                case "Slash2":
                    clipToPlay = slash2SFXClip;
                    break;
                case "Slash3":
                    clipToPlay = slash3SFXClip;
                    break;
                default:
                    clipToPlay = slash1SFXClip; // Fallback to Slash1
                    break;
            }
            
            if (clipToPlay != null)
            {
                SFXManager.instance.PlaySFXClip(clipToPlay, transform, attackSFXVolume);
            }
        }

        // Play muzzle flash effect
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
        
        // Boss accuracy is higher, especially in later phases
        float hitRoll = Random.Range(0f, 1f);
        Vector3 attackDirection;

        if (hitRoll <= attackAccuracy)
        {
            // Very accurate shot with minimal deviation
            Vector3 deviation = new Vector3(
                Random.Range(-0.03f, 0.03f), // Even more accurate than One-B
                Random.Range(-0.03f, 0.03f),
                Random.Range(-0.03f, 0.03f)
            );
            attackDirection = (baseDirection + deviation).normalized;
        }
        else
        {
            // Miss - but still very accurate
            Vector3 missDeviation = new Vector3(
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.2f, 0.2f),
                Random.Range(-0.3f, 0.3f)
            );
            attackDirection = (baseDirection + missDeviation).normalized;
        }

        // Perform raycast attack
        Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
        RaycastHit hit;
        float maxAttackRange = 60f; // Longer range than One-B

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

    // Animation Methods - Direct animator calls like death animation
    private void PlayWalkAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isFlying2", true);
        }
        
        // Trigger flying transformation
        if (enableFlyingTransformation)
        {
            SetFlyingState(true);
        }
    }

    private void StopWalkAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isFlying2", false);
        }
        
        // Stop flying transformation
        if (enableFlyingTransformation)
        {
            SetFlyingState(false);
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
        // Idle animations no longer needed since blocks are used for hit reactions
        // Boss will just maintain current animation state when idle
    }

    private void UpdateFlyingTransformation()
    {
        if (!enableFlyingTransformation) return;

        // Determine if we should be flying (flying animation is active AND not rushing)
        bool shouldBeFlyingNow = isCurrentlyFlying && !isRushing;

        // Update transformation progress
        if (shouldBeFlyingNow)
        {
            transformationProgress = Mathf.MoveTowards(transformationProgress, 1f, transformationSpeed * Time.deltaTime);
        }
        else
        {
            transformationProgress = Mathf.MoveTowards(transformationProgress, 0f, transformationSpeed * Time.deltaTime);
        }

        // Apply transformation using the curve for smooth animation
        float curveValue = transformationCurve.Evaluate(transformationProgress);
        
        // Calculate current transformation values
        Vector3 currentPositionOffset = Vector3.Lerp(Vector3.zero, flyingPositionOffset, curveValue);
        Vector3 currentScale = Vector3.Lerp(originalScale, Vector3.Scale(originalScale, flyingScaleMultiplier), curveValue);
        Quaternion currentRotationOffset = Quaternion.Lerp(Quaternion.identity, Quaternion.Euler(flyingRotationOffset), curveValue);

        // Apply transformations WITHOUT interfering with NavMeshAgent movement
        // Only apply the Y-offset for flying, let NavMeshAgent handle X and Z movement
        if (shouldBeFlyingNow && agent != null && agent.enabled && curveValue > 0.01f)
        {
            // Let NavMeshAgent control X and Z position, we only add the flying Y offset
            Vector3 navMeshPosition = transform.position;
            navMeshPosition.y += currentPositionOffset.y;
            transform.position = navMeshPosition;
        }
        
        // Always apply scale transformations
        transform.localScale = currentScale;
        
        // Only apply rotation offset if not rushing (to avoid conflicts with LookAt)
        if (!isRushing && curveValue > 0.01f)
        {
            // Preserve the current rotation from NavMeshAgent/LookAt and add flying rotation
            Quaternion baseRotation = transform.rotation;
            transform.rotation = baseRotation * currentRotationOffset;
        }
    }

    private void SetFlyingState(bool flying)
    {
        if (isCurrentlyFlying != flying)
        {
            isCurrentlyFlying = flying;
            
            // Store original scale when starting any transformation
            if (flying && transformationProgress <= 0.01f)
            {
                originalScale = transform.localScale;
                
                // Trigger the flying lift animation when starting to fly
                if (bossAnimator != null)
                {
                    bossAnimator.SetTrigger("isFlying1");
                }
            }
        }
    }

    public void TriggerSwapWithConstraint()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetTrigger("Swap");
        }

        // Delay constraint activation by 1 second
        Invoke(nameof(ActivateConstraint), 1.0f);

        if (playSwapSoundEffect && swapSoundClip != null)
        {
            SFXManager.instance?.PlaySFXClip(swapSoundClip, transform, 1f);
        }
    }

    private void ActivateConstraint()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = true;
        }
    }

    public void DeactivateParentConstraint()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = false;
        }
    }

    public void ToggleParentConstraint()
    {
        if (parentConstraint != null)
        {
            parentConstraint.constraintActive = !parentConstraint.constraintActive;
        }
    }

    public bool IsConstraintActive()
    {
        return parentConstraint != null && parentConstraint.constraintActive;
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
        
        // Stop all movement animations before playing death
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isFlying2", false);
            bossAnimator.SetBool("isRunning", false);
            
            // Reset any attack triggers that might be active
            bossAnimator.ResetTrigger("Slash1");
            bossAnimator.ResetTrigger("Slash2");
            bossAnimator.ResetTrigger("Slash3");
            bossAnimator.ResetTrigger("Block1");
            bossAnimator.ResetTrigger("Block2");
            bossAnimator.ResetTrigger("Block3");
            
            // Now trigger the death animation
            bossAnimator.SetTrigger("Death");
        }
        else
        {
            Debug.LogWarning("Boss Animator is not assigned. Cannot play death animation.");
        }
        
        // Stop flying transformation on death
        if (enableFlyingTransformation)
        {
            SetFlyingState(false);
        }
        
        // Cancel any pending invokes that might interfere
        CancelInvoke();
        
        Debug.Log($"Boss {bossName} death animation triggered");
    }

    /// <summary>
    /// Call this method to activate the boss AI after dialogue completion
    /// </summary>
    public void ActivateBoss()
    {
        // Play activation sound using SFXManager
        if (activationSound != null && SFXManager.instance != null)
        {
            SFXManager.instance.PlaySFXClip(activationSound, transform, attackSFXVolume);
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
        
        // Disable invincibility when boss becomes active
        if (bossTarget != null)
        {
            bossTarget.DisableInvincibility();
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
                // Randomly choose between Block1, Block2, Block3 when hit
                int randomBlock = Random.Range(1, 4);
                bossAnimator.SetTrigger($"Block{randomBlock}");
            }
            else
            {
                bossAnimator.SetTrigger("Block1");
            }
            
            // Play block sound effect using SFXManager
            if (blockSFXClip != null && SFXManager.instance != null)
            {
                SFXManager.instance.PlaySFXClip(blockSFXClip, transform, attackSFXVolume);
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
        
        // Stop flying transformation when deactivated
        if (enableFlyingTransformation)
        {
            SetFlyingState(false);
        }
    }

    /// <summary>
    /// Manually control the flying transformation state
    /// </summary>
    public void SetManualFlyingState(bool flying)
    {
        if (enableFlyingTransformation)
        {
            SetFlyingState(flying);
        }
    }

    /// <summary>
    /// Get the current flying transformation state
    /// </summary>
    public bool IsFlying()
    {
        return isCurrentlyFlying && transformationProgress > 0.5f;
    }

    /// <summary>
    /// Call this method when the boss dies to properly handle death state
    /// This should be called from the Target component's OnDeathAnimation event
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
    }
}
