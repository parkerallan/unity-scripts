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

    [Header("Movement")]
    public float sightRange = 25f; // Larger sight range
    public float attackRange = 3f; // Close range for kick attacks
    public float walkRange = 8f; // Range where enemy walks instead of runs
    public float circleStrafingRadius = 10f;
    public float strafingSpeed = 2f;
    public bool enableCircleStrafing = true;
    public float minCircleTime = 2f;
    public float maxCircleTime = 5f;

    [Header("Water Combat Settings")]
    public float waterAttackRange = 50f; // Very large range to start attacks very early for animation sync
    public float waterDamageRange = 20f; // Actual damage range - smaller than attack initiation range
    public float waterMinDistance = 10f; // Minimum distance to maintain when in water
    public float waterCircleRadius = 12f; // Circle radius when in water
    public bool disableRushInWater = true; // Prevent rushing when using ranged weapon
    public float waterAttackAccuracy = 0.92f; // Higher accuracy for water attacks to compensate for distance
    public float waterAccurateDeviation = 0.02f; // Smaller deviation for accurate water shots
    public float waterMissDeviation = 0.15f; // Reduced miss deviation for water attacks

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
    public GameObject waterWeapon; // Weapon GameObject to enable/disable when in water
    public GameObject waterProjectile; // Projectile to instantiate when attacking in water
    public Transform projectileSpawnPoint; // Where to spawn projectiles (can be weapon muzzle)
    public ParticleSystem surfingWaterEffect; // Particle effect to play under surfboard while surfing
    public LayerMask waterLayer; // Layer to detect water contact
    public float waterCheckRadius = 1f; // Radius for water detection
    public float surfingSpeed = 8f; // Speed when surfing on water
    public bool enableWaterSurfing = true;

    [Header("Attack Effects")]
    public AudioClip attackSFXClip; // Used for throw attacks
    public AudioClip kickSFXClip; // Separate SFX clip for kick attacks
    public float attackSFXVolume = 1f;

    [Header("Boss Specific Effects")]
    public AudioSource battleMusicTrigger;
    public ParticleSystem intimidationEffect; // Plays when boss activates

    [Header("One-B Specific Animations")]
    public bool useRandomAttackAnimations = true;
    public bool useRandomBlockAnimations = true;

    // Private variables
    private bool alreadyAttacked;
    private bool animationTriggered; // Track if attack animation has been triggered
    private float currentAggression = 0f;
    private Target bossTarget;
    private float originalTimeBetweenAttacks;
    private float originalAttackAccuracy;
    
    // Attack cooldown timers
    private float lastKickTime = 0f;
    private float lastThrowTime = 0f;
    private float kickCooldown = 4f; // Longer cooldown for kick attacks
    private float throwCooldown = 3f; // Longer cooldown for throw attacks
    private bool playerInSightRange, playerInAttackRange, playerInWalkRange;
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
    private bool surfingDirection = true; // true = surfing forward, false = circling back
    private float surfingChangeTime = 0f;
    private float surfingChangeInterval = 8f; // Surf for 8 seconds before turning around
    private float surfingDistance = 40f; // How far to surf past the player

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

        // Ensure water weapon starts disabled
        if (waterWeapon != null)
        {
            waterWeapon.SetActive(false);
        }

        // Ensure surfing water effect starts stopped
        if (surfingWaterEffect != null)
        {
            surfingWaterEffect.Stop();
            surfingWaterEffect.gameObject.SetActive(false);
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

        // Enable surfboard
        if (surfboard != null)
        {
            surfboard.SetActive(true);
        }

        // Enable water weapon
        if (waterWeapon != null)
        {
            waterWeapon.SetActive(true);
            Debug.Log($"{bossName} water weapon enabled!");
        }

        // Start surfing water particle effect
        if (surfingWaterEffect != null)
        {
            surfingWaterEffect.gameObject.SetActive(true);
            surfingWaterEffect.Play();
            Debug.Log($"{bossName} surfing water effect started!");
        }

        // Adjust agent settings for water movement
        if (agent != null && agent.enabled)
        {
            agent.speed = surfingSpeed;
        }

        // Trigger surfing animation state
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isOnBoard", true);
        }
    }

    private void ExitWaterMode()
    {
        Debug.Log($"{bossName} exiting water surfing mode!");

        // Disable surfboard
        if (surfboard != null)
        {
            surfboard.SetActive(false);
        }

        // Disable water weapon
        if (waterWeapon != null)
        {
            waterWeapon.SetActive(false);
            Debug.Log($"{bossName} water weapon disabled!");
        }

        // Stop surfing water particle effect
        if (surfingWaterEffect != null)
        {
            surfingWaterEffect.Stop();
            surfingWaterEffect.gameObject.SetActive(false);
            Debug.Log($"{bossName} surfing water effect stopped!");
        }

        // Reset agent settings for ground movement
        if (agent != null && agent.enabled)
        {
            agent.speed = 3.5f; // Default ground speed
        }

        // Exit surfing animation state
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isOnBoard", false);
        }
    }

    private void UpdateRangeChecks()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Use different ranges based on water state
        float currentAttackRange = isOnWater ? waterAttackRange : attackRange;
        float currentWalkRange = isOnWater ? waterMinDistance : walkRange;
        
        // Use distance-based detection for more reliable detection
        playerInSightRange = distanceToPlayer <= sightRange;
        playerInAttackRange = distanceToPlayer <= currentAttackRange;
        playerInWalkRange = distanceToPlayer <= currentWalkRange;
        
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
            
            // Check if we should randomly decide to rush (disabled in water if using ranged weapon)
            bool canRush = Time.time - lastRushTime >= rushCooldown && 
                          currentAggression >= aggressionThresholdForRush &&
                          !playerInAttackRange &&
                          !isRushing &&
                          !(isOnWater && disableRushInWater); // No rushing in water with ranged weapon
            
            // Random rush decision
            if (canRush && Random.Range(0f, 1f) < rushChance * Time.deltaTime)
            {
                isRushing = true;
                rushStartTime = Time.time;
                lastRushTime = Time.time;
                isCircling = false;
                circlingTime = 0f;
            }
            
            // Also trigger rush if circled too long (but not in water with ranged weapon)
            if (circlingTime >= maxCircleTime && !(isOnWater && disableRushInWater))
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
            
            // Decision making for behavior - different logic for water vs land
            if (isOnWater)
            {
                // Water behavior: continuous surfing runs with attacks during passes
                StopRunningAnimation();
                SurfingMovement();
                if (!isCircling)
                {
                    isCircling = true;
                    circlingTime = 0f;
                }
            }
            else
            {
                // Land behavior: simple chase and attack, no strafing
                if (playerInAttackRange && !alreadyAttacked)
                {
                    // Only attack if very close (within attack range)
                    AttackPlayer();
                    isCircling = false;
                    circlingTime = 0f;
                }
                else if (isRushing)
                {
                    RushPlayer();
                }
                else
                {
                    // Simple chase behavior - no strafing on land
                    StopRunningAnimation();
                    ChasePlayer();
                    isCircling = false;
                    circlingTime = 0f;
                }
            }
        }
        else
        {
            // Out of sight range - different behavior for water vs land
            if (isOnWater)
            {
                // Continue surfing even when player is out of sight range
                StopRunningAnimation();
                SurfingMovement();
                if (!isCircling)
                {
                    isCircling = true;
                    circlingTime = 0f;
                }
            }
            else
            {
                // On land: go idle when out of range
                PlayIdleAnimation();
                StopWalkAnimation();
                StopRunningAnimation();
                isRushing = false;
                isCircling = false;
                circlingTime = 0f;
            }
        }
    }

    private void MaintainDistance()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Calculate direction away from player
        Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
        
        // Find a position that maintains the minimum water distance
        Vector3 targetPosition = player.position + directionAwayFromPlayer * waterMinDistance;
        
        // Ensure the target position is accessible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 8f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Move away while maintaining water speed
        agent.speed = surfingSpeed;
        agent.updateRotation = false;
        agent.SetDestination(targetPosition);
        
        // Face the player while backing away
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        
        // Play surfing animation
        PlaySurfingAnimation();
    }

    private void SurfingMovement()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Update surfing direction change timer
        surfingChangeTime += Time.deltaTime;
        if (surfingChangeTime >= surfingChangeInterval)
        {
            surfingDirection = !surfingDirection; // Switch surfing directions
            surfingChangeTime = 0f;
        }

        // Create very long surf runs that go far past the player
        Vector3 perpDirection = Vector3.Cross(Vector3.forward, Vector3.up).normalized;
        
        // Start point is very far to one side of the player
        Vector3 startSide = player.position + (surfingDirection ? -perpDirection : perpDirection) * (surfingDistance * 1.5f);
        // End point is very far to the other side of the player  
        Vector3 endSide = player.position + (surfingDirection ? perpDirection : -perpDirection) * (surfingDistance * 1.5f);
        
        // Always surf toward the end point (making a long run past the player)
        Vector3 targetPosition = endSide;
        
        // Ensure the target position is accessible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 50f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Very high speed for long surf runs - never stop moving
        agent.speed = surfingSpeed * 3f;
        agent.updateRotation = false;
        agent.SetDestination(targetPosition);
        agent.isStopped = false; // Force continuous movement
        
        // Always face the direction of the surf run
        Vector3 lookDirection = (targetPosition - transform.position).normalized;
        lookDirection.y = 0;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 12f);
        }
        
        // Check distance for animation trigger (early) and damage (later)
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Trigger attack animation when farther away
        if (distanceToPlayer <= waterAttackRange && !animationTriggered && !alreadyAttacked && Time.time - lastThrowTime >= throwCooldown)
        {
            // Trigger animation early when in larger range
            if (bossAnimator != null)
            {
                bossAnimator.SetTrigger("Throw");
            }
            animationTriggered = true;
        }
        
        // Apply damage when closer (animation should be playing/finishing)
        if (distanceToPlayer <= waterDamageRange && animationTriggered && !alreadyAttacked)
        {
            // Apply damage after animation has had time to play
            PerformRaycastAttack();
            alreadyAttacked = true;
            animationTriggered = false;
            lastThrowTime = Time.time; // Set throw cooldown
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
        
        // Play surfing animation
        PlaySurfingAnimation();
    }

    private void CircleStrafing()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Use different circle radius based on water state
        float currentCircleRadius = isOnWater ? waterCircleRadius : circleStrafingRadius;

        // Adjust strafing behavior based on surfing state
        float strafingRotationSpeed = 0.8f * Time.deltaTime;
        if (isOnWater)
        {
            strafingRotationSpeed *= 1.3f; // Faster rotation when surfing
        }
        
        strafingAngle += strafingRotationSpeed;
        
        Vector3 offset = new Vector3(
            Mathf.Sin(strafingAngle) * currentCircleRadius,
            0,
            Mathf.Cos(strafingAngle) * currentCircleRadius
        );
        
        Vector3 targetPosition = player.position + offset;
        
        // Ensure the target position is accessible
        NavMeshHit hit;
        if (NavMesh.SamplePosition(targetPosition, out hit, 8f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }
        
        // Use appropriate speed based on state and distance
        float currentSpeed;
        if (isOnWater)
        {
            currentSpeed = surfingSpeed;
        }
        else
        {
            // On land: Always use run speed for faster movement
            currentSpeed = 5.5f; // Consistent running speed
        }
        
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
        if (isOnWater)
        {
            PlaySurfingAnimation();
        }
        else
        {
            // On land: Always run for faster strafing
            PlayRunAnimation();
        }
    }

    private void RushPlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Rush with appropriate speed based on state
        float currentRushSpeed = isOnWater ? surfingSpeed * 1.5f : rushSpeed;
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
            if (isOnWater)
            {
                bossAnimator.SetBool("isOnBoard", true);
                bossAnimator.SetBool("isRunning", false);
                // Surfing rush uses same surfing animation but faster
            }
            else
            {
                bossAnimator.SetBool("isRunning", true);
                bossAnimator.SetBool("isOnBoard", false);
            }
        }
        
        // If rush has ended, transition back to appropriate animation
        if (!isRushing && bossAnimator != null)
        {
            bossAnimator.SetBool("isRunning", false);
            if (isOnWater)
            {
                bossAnimator.SetBool("isOnBoard", true);
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
        if (isOnWater)
        {
            agent.speed = surfingSpeed;
            PlaySurfingAnimation();
        }
        else
        {
            // On land: Always run for faster chase
            agent.speed = 5.5f; // Faster running speed for chase
            PlayRunAnimation();
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
        // Check cooldowns before attacking
        if (isOnWater && Time.time - lastThrowTime < throwCooldown) return;
        if (!isOnWater && Time.time - lastKickTime < kickCooldown) return;
        
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
            if (isOnWater)
            {
                // Water attacks - use "Throw" animation
                bossAnimator.SetTrigger("Throw");
                lastThrowTime = Time.time;
            }
            else
            {
                // Ground attacks - use "Kick" trigger
                bossAnimator.SetTrigger("Kick");
                lastKickTime = Time.time;
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

        // Play appropriate attack sound effect using SFXManager
        if (SFXManager.instance != null)
        {
            if (isOnWater && attackSFXClip != null)
            {
                // Water attacks use the regular attack SFX (throw sound)
                SFXManager.instance.PlaySFXClip(attackSFXClip, transform, attackSFXVolume);
            }
            else if (!isOnWater && kickSFXClip != null)
            {
                // Ground attacks use the kick SFX
                SFXManager.instance.PlaySFXClip(kickSFXClip, transform, attackSFXVolume);
            }
            else if (attackSFXClip != null)
            {
                // Fallback to regular attack SFX if kick SFX is not assigned
                SFXManager.instance.PlaySFXClip(attackSFXClip, transform, attackSFXVolume);
            }
        }

        // Calculate attack direction with accuracy variation
        Vector3 targetPosition = player.position;
        Vector3 baseDirection = (targetPosition - transform.position).normalized;
        Vector3 attackDirection;

        if (isOnWater)
        {
            // Water attacks: almost perfect accuracy, direct targeting
            attackDirection = baseDirection; // No deviation for water attacks
        }
        else
        {
            // Land attacks: use original accuracy system
            float hitRoll = Random.Range(0f, 1f);
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
        }

        // Instantiate water projectile if in water
        if (isOnWater && waterProjectile != null)
        {
            // Determine spawn position
            Vector3 spawnPosition = projectileSpawnPoint != null ? projectileSpawnPoint.position : (transform.position + Vector3.up * 1.5f);
            
            // Better targeting with prediction
            Vector3 predictedTargetPosition = player.position + Vector3.up * 1.0f; // Aim at player's torso height
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                // Predict player movement
                float distanceToPlayer = Vector3.Distance(spawnPosition, player.position);
                float projectileSpeed = 30f;
                float timeToReach = distanceToPlayer / projectileSpeed;
                predictedTargetPosition = player.position + Vector3.up * 1.0f + (playerRb.linearVelocity * timeToReach);
            }
            
            Vector3 improvedDirection = (predictedTargetPosition - spawnPosition).normalized;
            
            // Instantiate projectile
            GameObject projectile = Instantiate(waterProjectile, spawnPosition, Quaternion.LookRotation(improvedDirection));
            
            // Try to add velocity to projectile if it has a Rigidbody
            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();
            if (projectileRb != null)
            {
                float projectileSpeed = 30f; // Adjust speed as needed
                projectileRb.linearVelocity = improvedDirection * projectileSpeed;
                projectileRb.useGravity = false; // Disable gravity for straight shots
            }
            else
            {
                // If no Rigidbody, add one automatically
                projectileRb = projectile.AddComponent<Rigidbody>();
                projectileRb.useGravity = false;
                float projectileSpeed = 30f;
                projectileRb.linearVelocity = improvedDirection * projectileSpeed;
            }
            
            // Destroy projectile after 5 seconds to prevent buildup
            Destroy(projectile, 5f);
        }

        // Perform raycast attack
        Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
        RaycastHit hit;
        float maxAttackRange = 50f;

        // For water attacks, use a simpler layer mask that only targets player
        LayerMask raycastLayers;
        if (isOnWater)
        {
            // Only hit player layer in water to avoid hitting water surface or obstacles
            raycastLayers = LayerMask.GetMask("Default"); // Assuming player is on Default layer
        }
        else
        {
            raycastLayers = attackLayers;
        }

        if (Physics.Raycast(attackRay, out hit, maxAttackRange, raycastLayers))
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
        // If raycast missed in water, try a direct sphere check as backup
        else if (isOnWater)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= waterAttackRange)
            {
                var playerTarget = player.GetComponent<Target>();
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
        animationTriggered = false;
    }

    // Animation Methods - Different animations for water vs ground
    private void PlaySurfingAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isOnBoard", true);
        }
    }

    private void PlayWalkAnimation()
    {
        if (bossAnimator != null)
        {
            if (isOnWater)
            {
                bossAnimator.SetBool("isOnBoard", true);
            }
            else
            {
                bossAnimator.SetBool("isWalking", true);
                bossAnimator.SetBool("isRunning", false); // Ensure running is off when walking
                bossAnimator.SetBool("isOnBoard", false); // Ensure surfing is off on land
            }
        }
    }

    private void PlayRunAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isRunning", true);
            bossAnimator.SetBool("isWalking", false); // Ensure walking is off when running
            bossAnimator.SetBool("isOnBoard", false); // Ensure surfing is off on land
        }
    }

    private void StopWalkAnimation()
    {
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isWalking", false);
            if (!isOnWater)
            {
                bossAnimator.SetBool("isOnBoard", false);
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
            if (isOnWater)
            {
                bossAnimator.SetBool("isOnBoard", true);
                bossAnimator.SetBool("isWalking", false);
                bossAnimator.SetBool("isRunning", false);
            }
            else
            {
                bossAnimator.SetBool("isOnBoard", false);
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

        // Stop surfing particle effect on death
        if (surfingWaterEffect != null)
        {
            surfingWaterEffect.Stop();
            surfingWaterEffect.gameObject.SetActive(false);
        }
        
        // Keep surfboard and water weapon active during death animation
        // They will be disabled when the object is destroyed
        
        // Stop all movement animations before playing death
        if (bossAnimator != null)
        {
            bossAnimator.SetBool("isOnBoard", false);
            bossAnimator.SetBool("isWalking", false);
            bossAnimator.SetBool("isRunning", false);
            
            // Reset any attack triggers that might be active
            bossAnimator.ResetTrigger("Kick");
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
        
        // Disable water weapon if active
        if (waterWeapon != null)
        {
            waterWeapon.SetActive(false);
        }

        // Stop surfing particle effect if active
        if (surfingWaterEffect != null)
        {
            surfingWaterEffect.Stop();
            surfingWaterEffect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Manually set surfing state (for external triggers)
    /// </summary>
    public void SetSurfingState(bool surfing)
    {
        if (surfing && !isOnWater)
        {
            EnterWaterMode();
        }
        else if (!surfing && isOnWater)
        {
            ExitWaterMode();
        }
    }

    /// <summary>
    /// Get current surfing state
    /// </summary>
    public bool IsSurfing()
    {
        return isOnWater;
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
            Gizmos.color = isOnWater ? Color.cyan : Color.white;
            Gizmos.DrawWireSphere(transform.position, waterCheckRadius);
        }
    }
}