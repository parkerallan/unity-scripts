using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class twoBController : MonoBehaviour
{
  [Header("Animation")]
  public Animator enemyAnimator;

  [Header("AI Components")]
  public NavMeshAgent agent;
  public Transform player;
  public LayerMask whatIsGround, whatIsPlayer;

  // Patrolling
  public Vector3 walkPoint;
  bool walkPointSet;
  public float walkPointRange;
  
  [Header("Patrol Behavior")]
  public float patrolWaitTime = 2f;
  public float patrolSpeed = 3f; // Increased from 1.5f for faster walking
  public float runSpeed = 6f; // Running speed when chasing player
  
  [Header("Strafe and Shoot Behavior")]
  public bool enableStrafeAndShoot = true; // Enable shooting while running sideways
  public float strafeDistance = 8f; // How far to the side to strafe
  public float chaseChance = 0.3f; // Chance to chase instead of strafe (0.0 to 1.0)
  public float strafeShootCooldown = 2f; // Cooldown between strafe shoots (reduced)
  public float kneelingYOffset = -0.5f; // How much to lower Y position when kneeling
  public float kneelingTransitionSpeed = 5f; // How fast to transition between standing and kneeling
  private bool isStrafeAndShooting = false;
  private float lastStrafeShootTime = 0f;
  private Vector3 strafeTarget;
  private bool strafingLeft = true; // Direction of current strafe
  private bool isKneeling = false;
  private bool isTransitioningToKneel = false;
  private bool isTransitioningToStand = false;
  private Vector3 originalPosition;
  private Vector3 targetKneelingPosition;
  
  private float patrolWaitTimer = 0f;
  private bool isWaitingAtPatrolPoint = false;
  private float originalAgentSpeed;

  // Burst Fire System
  [Header("Burst Fire Settings")]
  public int burstFireCount = 10; // Number of shots in a burst
  public float timeBetweenBurstShots = 0.1f; // Time between individual shots in burst
  public float burstCooldown = 5f; // Cooldown after burst is complete
  public float attackAccuracy = 0.7f;
  public float attackDamage = 25f;
  public LayerMask attackLayers = -1;
  
  [Header("Attack Effects")]
  public AudioClip attackSFXClip;
  public float attackSFXVolume = 1f;
  public ParticleSystem muzzleFlash;
  public ParticleSystem casingEffect;
  
  [Header("Environmental Impact Effects")]
  public GameObject bulletImpactPrefab; // Prefab for bullet impact on environment
  public float impactEffectDuration = 2f; // How long impact effects last
  
  [Header("Boss Settings")]
  public string bossName = "Two-B";
  public bool isActive = false; // Boss AI is inactive until triggered
  public float activationDelay = 2f; // Delay after activation for dramatic effect
  public GameObject gunObject; // Gun GameObject to enable/disable
  
  [Header("Boss Specific Effects")]
  public AudioSource battleMusicTrigger;
  public ParticleSystem intimidationEffect; // Plays when boss activates
  public AudioClip activationSound;
  
  // States
  [Header("Detection Ranges")]
  public float sightRange = 25f; // Range at which enemy can detect player
  public float attackRange = 8f; // Range at which enemy stops to attack
  public float chaseRange = 35f; // Extended range for persistent chasing
  public bool playerInSightRange, playerInAttackRange, playerInChaseRange;
  
  // Alert system
  private bool isAlerted = false;
  private float alertDuration = 15f; // Increased from 10f for longer chase
  private float alertTimer = 0f;
  
  // Burst fire state
  private bool isFiring = false;
  private bool burstOnCooldown = false;
  
  // Audio cooldown to prevent double SFX
  private float lastAudioPlayTime = 0f;
  private float audioPlayCooldown = 1f; // Minimum time between audio plays
  
  // Boss target reference for invincibility management
  private Target bossTarget;

  private void Awake()
  {
    player = GameObject.Find("CharModel1").transform;
    agent = GetComponent<NavMeshAgent>();
    bossTarget = GetComponent<Target>();
    
    // Store original agent speed for restoration
    if (agent != null)
    {
        originalAgentSpeed = agent.speed;
    }
    
    // Auto-assign animator if not manually set
    if (enemyAnimator == null)
        enemyAnimator = GetComponent<Animator>();

    // Start inactive - disable NavMeshAgent until activated
    if (agent != null)
        agent.enabled = false;

    // Ensure gun starts disabled
    if (gunObject != null)
    {
        gunObject.SetActive(false);
    }
  }

  private void Update()
  {
    // Don't update AI if not active or if dead
    if (!isActive) return;
    
    // Additional safety check - if component is disabled, don't run AI
    if (!this.enabled) return;

    // Handle kneeling position based on Shoot1 animation state
    if (enemyAnimator != null)
    {
        bool isShoot1Active = enemyAnimator.GetBool("Shoot1");
        
        if (isShoot1Active && !isKneeling && !isTransitioningToKneel)
        {
            // Start transitioning to kneeling
            isTransitioningToKneel = true;
            isTransitioningToStand = false;
            originalPosition = transform.position;
            targetKneelingPosition = originalPosition;
            targetKneelingPosition.y += kneelingYOffset;
            
            // DISABLE NavMeshAgent to prevent it from overriding position
            if (agent != null && agent.enabled)
            {
                agent.enabled = false;
            }
            
            ////Debug.Log($"STARTING KNEEL TRANSITION: Target Y position {targetKneelingPosition.y}");
        }
        else if (!isShoot1Active && isKneeling && !isTransitioningToStand)
        {
            // Start transitioning to standing
            isTransitioningToStand = true;
            isTransitioningToKneel = false;
            
            ////Debug.Log($"STARTING STAND TRANSITION: Target Y position {originalPosition.y}");
        }
    }
    
    // Handle smooth transitions
    if (isTransitioningToKneel)
    {
        // Smoothly transition to kneeling position
        Vector3 currentPos = transform.position;
        currentPos = Vector3.Lerp(currentPos, targetKneelingPosition, Time.deltaTime * kneelingTransitionSpeed);
        transform.position = currentPos;
        
        // Check if we've reached the target
        if (Vector3.Distance(currentPos, targetKneelingPosition) < 0.01f)
        {
            transform.position = targetKneelingPosition;
            isTransitioningToKneel = false;
            isKneeling = true;
            ////Debug.Log($"KNEEL COMPLETE: Y position is now {transform.position.y}");
        }
    }
    else if (isTransitioningToStand)
    {
        // Smoothly transition to standing position
        Vector3 currentPos = transform.position;
        currentPos = Vector3.Lerp(currentPos, originalPosition, Time.deltaTime * kneelingTransitionSpeed);
        transform.position = currentPos;
        
        // Check if we've reached the target
        if (Vector3.Distance(currentPos, originalPosition) < 0.01f)
        {
            transform.position = originalPosition;
            isTransitioningToStand = false;
            isKneeling = false;
            
            // RE-ENABLE NavMeshAgent
            if (agent != null && !agent.enabled)
            {
                agent.enabled = true;
            }
            
            ////Debug.Log($"STAND COMPLETE: NavAgent re-enabled, Y position restored to {transform.position.y}");
        }
    }

    // Check for sight and attack range
    playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
    playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
    playerInChaseRange = Physics.CheckSphere(transform.position, chaseRange, whatIsPlayer);

    // Auto-alert when player enters sight range
    if (playerInSightRange && !isAlerted)
    {
        isAlerted = true;
        alertTimer = alertDuration;
        ////Debug.Log($"TwoB spotted player - now alerted for {alertDuration} seconds");
    }

    // Handle continuous player-facing rotation when in combat or kneeling
    if (player != null && (playerInSightRange || playerInChaseRange || isAlerted || isKneeling || isStrafeAndShooting))
    {
        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0; // Keep rotation on Y-axis only
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            
            // Use faster rotation when kneeling or when NavAgent is disabled
            float rotationSpeed = (isKneeling || !agent.enabled) ? 15f : 10f;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // Handle alert timer
    if (isAlerted)
    {
        alertTimer -= Time.deltaTime;
        if (alertTimer <= 0f)
        {
            // Only stop being alerted if player is also outside chase range
            if (!playerInChaseRange)
            {
                isAlerted = false;
                ////Debug.Log("TwoB is no longer alerted - player too far away");
            }
            else
            {
                // Refresh alert timer if player still in chase range
                alertTimer = alertDuration;
                ////Debug.Log("TwoB alert refreshed - player still in chase range");
            }
        }
    }

    // State machine logic
    if (playerInSightRange || playerInChaseRange || isAlerted)
    {
        // ALWAYS try to strafe when player is detected and strafe is enabled
        if (enableStrafeAndShoot)
        {
            bool canStrafeShoot = Time.time - lastStrafeShootTime >= strafeShootCooldown &&
                                  !isFiring && !burstOnCooldown;
            
            if (canStrafeShoot && !isStrafeAndShooting)
            {
                if (agent.enabled)
                    agent.updateRotation = false;
                StrafeAndShoot();
            }
            else if (isStrafeAndShooting)
            {
                // Continue current strafe
                StrafeAndShoot();
            }
            else if (playerInAttackRange && !isStrafeAndShooting)
            {
                // In attack range and can't strafe - attack directly
                if (agent.enabled)
                    agent.updateRotation = false;
                AttackPlayer();
            }
            else if (!isStrafeAndShooting)
            {
                // Not in attack range and can't strafe - chase and shoot while running
                if (agent.enabled)
                    agent.updateRotation = false;
                ChasePlayer();
                
                // Start burst fire while chasing if not already firing AND not in strafe mode
                if (!isFiring && !burstOnCooldown && !isStrafeAndShooting)
                {
                    StartCoroutine(PerformBurstFire());
                }
            }
        }
        else
        {
            // Strafe disabled - use normal attack/chase logic
            if (playerInAttackRange)
            {
                if (agent.enabled)
                    agent.updateRotation = false;
                AttackPlayer();
            }
            else
            {
                if (agent.enabled)
                    agent.updateRotation = false;
                ChasePlayer();
            }
        }
    }
    else
    {
        // Exit strafe and shoot mode when losing sight of player
        if (isStrafeAndShooting)
        {
            ExitStrafeAndShoot();
        }
        
        // Reset agent rotation control when going back to patrol
        if (agent.enabled)
            agent.updateRotation = true;
            
        Patroling();
    }
  }

  private void SearchWalkPoint()
  {
    // Calculate random point in range - ensure walkPointRange is not zero
    if (walkPointRange <= 0f) walkPointRange = 10f; // Default range if not set
    
    float randomZ = Random.Range(-walkPointRange, walkPointRange);
    float randomX = Random.Range(-walkPointRange, walkPointRange);

    walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

    // Always set walkPoint as valid if no ground check needed, or check ground
    if (whatIsGround == 0)
    {
        walkPointSet = true; // No ground layer check needed
        ////Debug.Log($"TwoB found walkpoint at {walkPoint} (no ground check)");
    }
    else if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
    {
        walkPointSet = true;
        ////Debug.Log($"TwoB found valid walkpoint at {walkPoint}");
    }
    else
    {
        ////Debug.Log($"TwoB walkpoint {walkPoint} failed ground check");
    }
  }

  private void Patroling()
  {
    // Ensure agent is enabled and on navmesh
    if (agent == null || !agent.enabled || !agent.isOnNavMesh)
    {
        ////Debug.LogError("TwoB patrol FAILED - Agent enabled: " + (agent?.enabled) + ", OnNavMesh: " + (agent?.isOnNavMesh));
        return;
    }
    
    // Set patrol speed and ensure not stopped
    agent.speed = patrolSpeed;
    agent.isStopped = false;
    
    // If just entering patrol mode, immediately start looking for walkpoint
    if (!walkPointSet && !isWaitingAtPatrolPoint)
    {
        ////Debug.Log("TwoB starting patrol - searching for walkpoint");
        SearchWalkPoint();
    }

    if (!walkPointSet) 
    {
        SearchWalkPoint();
    }

    if (walkPointSet)
    {
      // Check if we're waiting at the patrol point
      if (isWaitingAtPatrolPoint)
      {
          patrolWaitTimer -= Time.deltaTime;
          
          // Stop moving while waiting
          agent.isStopped = true;
          
          // Set idle animation while waiting
          if (enemyAnimator != null)
          {
              enemyAnimator.SetBool("isWalking", false);
              enemyAnimator.SetBool("isRunning", false);
              enemyAnimator.SetBool("Shoot", false);
              enemyAnimator.SetBool("Shoot1", false); // Make sure kneeling shoot is off
          }
          
          if (patrolWaitTimer <= 0f)
          {
              isWaitingAtPatrolPoint = false;
              walkPointSet = false; // Find a new patrol point
              agent.isStopped = false; // Resume movement
              ////Debug.Log("TwoB finished waiting, finding new patrol point");
          }
      }
      else
      {
          agent.isStopped = false;
          agent.SetDestination(walkPoint);
          
          Vector3 distanceToWalkPoint = transform.position - walkPoint;

          // Walkpoint reached - start waiting
          if (distanceToWalkPoint.magnitude < 1f)
          {
              isWaitingAtPatrolPoint = true;
              patrolWaitTimer = patrolWaitTime;
              ////Debug.Log("TwoB reached patrol point, waiting");
          }
          else
          {
              // Set walking animation while moving to patrol point
              if (enemyAnimator != null)
              {
                  enemyAnimator.SetBool("isWalking", true);
                  enemyAnimator.SetBool("isRunning", false);
                  enemyAnimator.SetBool("Shoot", false);
                  enemyAnimator.SetBool("Shoot1", false); // Make sure kneeling shoot is off
              }
          }
      }
    }
    else
    {
        ////Debug.LogWarning("TwoB could not find valid patrol point");
    }
  }

  private void ChasePlayer()
  {
    // If agent is disabled (kneeling), don't try to chase
    if (agent == null || player == null)
    {
        ////Debug.LogError("TwoB chase FAILED - Missing agent or player component");
        return;
    }
    
    // If agent is disabled (during kneeling), skip movement but allow other chase behavior
    if (!agent.enabled || !agent.isOnNavMesh)
    {
        // Don't log error - this is normal during kneeling transitions
        return;
    }
    
    // Set running speed for chasing (faster than patrol)
    agent.speed = runSpeed;
    agent.isStopped = false;
    agent.SetDestination(player.position);
    
    ////Debug.Log($"TwoB CHASING player to {player.position} at speed {agent.speed}");
    
    // Set running animation when chasing
    if (enemyAnimator != null)
    {
        enemyAnimator.SetBool("isWalking", false);
        enemyAnimator.SetBool("isRunning", true);
        enemyAnimator.SetBool("Shoot", false);
        enemyAnimator.SetBool("Shoot1", false); // Make sure kneeling shoot is off
    }
  }

  private void StrafeAndShoot()
  {
    if (player == null) 
    {
        ////Debug.LogError("TwoB StrafeAndShoot FAILED - No player");
        return;
    }
    
    // If agent is disabled (due to kneeling), we can still continue strafing
    if (agent == null)
    {
        ////Debug.LogError("TwoB StrafeAndShoot FAILED - No NavMeshAgent component");
        return;
    }
    
    // If just starting strafe and shoot, pick a side and calculate strafe position
    if (!isStrafeAndShooting)
    {
        isStrafeAndShooting = true;
        lastStrafeShootTime = Time.time;
        
        // Store original position for kneeling restoration
        originalPosition = transform.position;
        
        // Randomly choose left or right
        strafingLeft = Random.Range(0, 2) == 0;
        
        // Calculate strafe position to the side of the current position
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        Vector3 strafeDirection = strafingLeft ? 
            Vector3.Cross(directionToPlayer, Vector3.up).normalized : 
            Vector3.Cross(Vector3.up, directionToPlayer).normalized;
        
        // Strafe from current position
        strafeTarget = transform.position + (strafeDirection * strafeDistance);
        
        // Ensure the strafe target is on the navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(strafeTarget, out hit, 10f, NavMesh.AllAreas))
        {
            strafeTarget = hit.position;
            ////Debug.Log($"TwoB starting strafe - moving {(strafingLeft ? "LEFT" : "RIGHT")} to {strafeTarget}");
        }
        else
        {
            ////Debug.LogWarning("TwoB could not find valid strafe position, using original target");
        }
    }
    
    // Check if we've reached the strafe position
    float distanceToStrafeTarget = Vector3.Distance(transform.position, strafeTarget);
    
    if (distanceToStrafeTarget > 2f && !isKneeling)
    {
        // Still moving to strafe position - only use agent if it's enabled
        if (agent.enabled)
        {
            agent.speed = runSpeed;
            agent.isStopped = false;
            agent.updateRotation = false; // We'll handle rotation manually
            agent.SetDestination(strafeTarget);
        }
        
        // Set running animation while moving to strafe position
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool("isWalking", false);
            enemyAnimator.SetBool("isRunning", true);
            enemyAnimator.SetBool("Shoot", false);
            enemyAnimator.SetBool("Shoot1", false);
        }
    }
    else
    {
        // Reached strafe position - stop and kneel shoot
        if (agent.enabled)
        {
            agent.isStopped = true;
        }
        
        // Set kneeling shoot animation (Y position handled in Update())
        if (enemyAnimator != null)
        {
            enemyAnimator.SetBool("isWalking", false);
            enemyAnimator.SetBool("isRunning", false);
            enemyAnimator.SetBool("Shoot1", true); // This will trigger kneeling in Update()
        }
        
        // Start shooting if not already firing
        if (!isFiring && !burstOnCooldown)
        {
            StartCoroutine(PerformStrafeShoot());
        }
    }
  }

  private void ExitStrafeAndShoot()
  {
    isStrafeAndShooting = false;
    
    // Stop any ongoing firing when exiting strafe
    isFiring = false;
    burstOnCooldown = true; // Force cooldown to prevent immediate re-firing
    
    // Reset agent rotation control
    if (agent.enabled)
        agent.updateRotation = true;
    
    // Reset animations (Y position will be handled automatically in Update())
    if (enemyAnimator != null)
    {
        enemyAnimator.SetBool("Shoot1", false); // This will trigger standing in Update()
        enemyAnimator.SetBool("isRunning", false);
        enemyAnimator.SetBool("Shoot", false);
    }
    
    // Set strafe cooldown timer
    lastStrafeShootTime = Time.time;
    
    ////Debug.Log("TwoB exited strafe and shoot mode - starting cooldown");
    
    // Start the burst cooldown to prevent immediate re-firing
    StartCoroutine(ResetBurstCooldown());
  }
  
  
  /// Reset burst cooldown after delay
  
  private IEnumerator ResetBurstCooldown()
  {
    yield return new WaitForSeconds(burstCooldown);
    burstOnCooldown = false;
  }

  private void AttackPlayer()
  {
    if (agent.isOnNavMesh && agent.enabled)
    {
        agent.SetDestination(transform.position); // Stop moving
    }

    if (player != null)
    {
        transform.LookAt(player);
    }

    // Start burst fire if not already firing and not on cooldown
    // (Animation is handled within the burst fire coroutine)
    if (!isFiring && !burstOnCooldown)
    {
        StartCoroutine(PerformBurstFire());
    }
  }

  
  
  /// Perform burst fire attack - fires multiple shots in succession
  
  private IEnumerator PerformBurstFire()
  {
    // Prevent multiple concurrent burst fires
    if (isFiring)
    {
        yield break;
    }
    
    isFiring = true;
    
    // Don't override strafe shooting animation - only set if not strafing
    if (!isStrafeAndShooting && enemyAnimator != null)
    {
        enemyAnimator.SetBool("isWalking", false);
        
        // If we're chasing (running), keep running AND add shooting (both layers active)
        if (enemyAnimator.GetBool("isRunning"))
        {
            // Keep running active AND enable shooting - both layers
            enemyAnimator.SetBool("isRunning", true);
            enemyAnimator.SetBool("Shoot", true);
            ////Debug.Log("TwoB shooting while running - both Run and Shoot layers active");
        }
        else
        {
            // Standing still - stop running and just shoot
            enemyAnimator.SetBool("isRunning", false);
            enemyAnimator.SetBool("Shoot", true);
            ////Debug.Log("TwoB standing and shooting - only Shoot layer active");
        }
    }
    
    ////Debug.Log($"TwoB starting burst fire - {burstFireCount} shots");
    
    // Play attack sound effect once at the start of the burst WITH COOLDOWN
    if (attackSFXClip != null && SFXManager.instance != null && Time.time - lastAudioPlayTime >= audioPlayCooldown)
    {
        SFXManager.instance.PlaySFXClip(attackSFXClip, transform, attackSFXVolume);
        lastAudioPlayTime = Time.time;
       //Debug.Log("TwoB played burst fire audio");
    }
    
    for (int i = 0; i < burstFireCount; i++)
    {
        // Perform individual shot (without SFX since we play it once per burst)
        PerformSingleRaycastAttack(false);
        
        // Wait between shots (except for the last shot)
        if (i < burstFireCount - 1)
        {
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
    }
    
    // Burst complete, start cooldown
    isFiring = false;
    burstOnCooldown = true;
    
    // Reset shooting animation when burst is complete (only if not strafing)
    if (!isStrafeAndShooting && enemyAnimator != null)
    {
        enemyAnimator.SetBool("Shoot", false);
        
        // If we were chasing, continue running, otherwise idle
        if (enemyAnimator.GetBool("isRunning"))
        {
            ////Debug.Log("TwoB finished shooting - continuing to run");
            // Keep running active, just turn off shooting layer
        }
        else
        {
            // Just turn off shooting - let the idle state be handled naturally
            ////Debug.Log("TwoB finished shooting - returning to idle");
        }
    }
    
    ////Debug.Log($"TwoB burst fire complete, cooling down for {burstCooldown} seconds");
    
    // Wait for cooldown before allowing another burst
    yield return new WaitForSeconds(burstCooldown);
    
    burstOnCooldown = false;
    ////Debug.Log("TwoB burst fire cooldown complete");
  }

  
  /// Perform burst fire while strafing - for strafe and shoot behavior
  
  private IEnumerator PerformStrafeShoot()
  {
    // Prevent multiple concurrent strafe shoots
    if (isFiring)
    {
        yield break;
    }
    
    isFiring = true;
    
    ////Debug.Log($"TwoB starting strafe shoot - {burstFireCount} shots");
    
    // Play attack sound effect once at the start of the burst WITH COOLDOWN
    if (attackSFXClip != null && SFXManager.instance != null && Time.time - lastAudioPlayTime >= audioPlayCooldown)
    {
        SFXManager.instance.PlaySFXClip(attackSFXClip, transform, attackSFXVolume);
        lastAudioPlayTime = Time.time;
       //Debug.Log("TwoB played strafe shoot audio");
    }
    
    for (int i = 0; i < burstFireCount; i++)
    {
        if (!isActive || !isStrafeAndShooting) break;
        
        // Perform individual shot WITHOUT SFX (already played once above)
        PerformSingleRaycastAttack(false);
        
        // Wait between shots (except for the last shot)
        if (i < burstFireCount - 1)
        {
            yield return new WaitForSeconds(timeBetweenBurstShots);
        }
    }
    
    isFiring = false;
    ////Debug.Log("TwoB strafe shoot complete - exiting strafe mode");
    
    // After shooting, exit strafe and shoot mode
    yield return new WaitForSeconds(0.5f); // Small delay before exiting
    ExitStrafeAndShoot();
  }
  
  
  /// Perform a single raycast attack as part of the burst
  
  private void PerformSingleRaycastAttack(bool playSFX = true)
  {
    if (player == null) return;

    // Play attack sound effect using SFXManager (only if specified)
    if (playSFX && attackSFXClip != null && SFXManager.instance != null)
    {
        SFXManager.instance.PlaySFXClip(attackSFXClip, transform, attackSFXVolume);
    }
    
    // Play muzzle flash effect
    if (muzzleFlash != null)
    {
        muzzleFlash.Play();
    }
    
    // Play bullet casing ejection effect
    if (casingEffect != null)
    {
        casingEffect.Play();
    }

    // Calculate attack direction with accuracy variation
    Vector3 baseDirection = (player.position - transform.position).normalized;
    
    // Add inaccuracy based on attackAccuracy
    float hitRoll = Random.Range(0f, 1f);
    Vector3 attackDirection;
    
    if (hitRoll <= attackAccuracy)
    {
        // Accurate shot - aim directly at player with minimal deviation
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float deviationScale = Mathf.Clamp(distanceToPlayer / 50f, 0.1f, 1f);
        
        Vector3 deviation = new Vector3(
            Random.Range(-0.05f, 0.05f) * deviationScale,
            Random.Range(-0.05f, 0.05f) * deviationScale,
            Random.Range(-0.05f, 0.05f) * deviationScale
        );
        attackDirection = (baseDirection + deviation).normalized;
        ////Debug.Log($"TwoB attack: ACCURATE SHOT! Distance: {distanceToPlayer:F1}m");
    }
    else
    {
        // Miss - larger random deviation
        Vector3 missDeviation = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f),
            Random.Range(-1f, 1f)
        );
        attackDirection = (baseDirection + missDeviation).normalized;
        ////Debug.Log("TwoB attack: MISSED!");
    }

    // Perform raycast attack - use all layers for impact detection
    Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
    RaycastHit hit;
    float attackRangeMax = 50f;

    // Raycast against ALL layers for bullet impacts, not just attackLayers
    if (Physics.Raycast(attackRay, out hit, attackRangeMax))
    {
        ////Debug.Log($"TwoB raycast hit: {hit.collider.name} on layer {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        
        // Check if we hit the player (only deal damage if it's in attackLayers)
        if (hit.collider.CompareTag("Player"))
        {
            // Only deal damage if player is in the attackLayers
            if (((1 << hit.collider.gameObject.layer) & attackLayers) != 0)
            {
                ////Debug.Log($"Player hit for {attackDamage} damage!");
                
                // Try to get Target component and deal damage
                var playerTarget = hit.collider.GetComponent<Target>();
                if (playerTarget != null)
                {
                    playerTarget.TakeDamage(attackDamage);
                    ////Debug.Log($"TwoB successfully dealt {attackDamage} damage to player!");
                }
                else
                {
                    ////Debug.LogWarning("Player hit but no Target component found!");
                }
            }
            else
            {
                ////Debug.Log("Player hit but not on attackable layer - no damage dealt");
            }
        }
        else
        {
            // Hit environment - ALWAYS spawn bullet impact effect for non-player hits
            ////Debug.Log($"TwoB bullet hit environment: {hit.collider.name} - spawning impact effect");
            SpawnBulletImpact(hit.point, hit.normal);
        }
        
        // Visual feedback
        Debug.DrawRay(attackRay.origin, attackDirection * hit.distance, Color.red, 2f);
    }
    else
    {
        // Shot went into the void
        Debug.DrawRay(attackRay.origin, attackDirection * attackRangeMax, Color.yellow, 2f);
        ////Debug.Log("TwoB shot missed completely - no collision detected!");
    }
  }
  
  
  /// Spawn bullet impact effect on environmental surfaces
  
  private void SpawnBulletImpact(Vector3 impactPoint, Vector3 impactNormal)
  {
    if (bulletImpactPrefab == null)
    {
        ////Debug.LogWarning("TwoB bulletImpactPrefab is null - cannot spawn impact effect");
        return;
    }
    
    // Spawn the impact effect at the hit point
    GameObject impactEffect = Instantiate(bulletImpactPrefab, impactPoint, Quaternion.LookRotation(impactNormal));
    
    ////Debug.Log($"TwoB spawned bullet impact effect '{impactEffect.name}' at {impactPoint} with normal {impactNormal}");
    
    // Destroy the impact effect after specified duration
    if (impactEffectDuration > 0f)
    {
        Destroy(impactEffect, impactEffectDuration);
        ////Debug.Log($"TwoB impact effect will be destroyed in {impactEffectDuration} seconds");
    }
  }
  
  
  /// Call this method when the enemy is hit by the player to make them alert and chase
  
  public void OnHitByPlayer()
  {
    isAlerted = true;
    alertTimer = alertDuration;
    ////Debug.Log($"TwoB hit by player - now alerted and will chase for {alertDuration} seconds!");
  }
  
  
  /// Call this method to trigger the death animation
  
  public void PlayDeathAnimation()
  {
   //Debug.Log($"Boss {bossName} PlayDeathAnimation() called - starting death sequence");
    
    // Stop all AI behavior but keep component enabled for death sequence
    isActive = false;
    
    // Stop the NavMeshAgent
    if (agent != null && agent.enabled)
    {
        agent.isStopped = true;
        agent.enabled = false;
    }
    
    // Reset firing states
    isFiring = false;
    burstOnCooldown = false;
    
    // Cancel all pending invokes (but not coroutines, as teleport needs them)
    CancelInvoke();
    
    // Disable gun object
    if (gunObject != null)
    {
        gunObject.SetActive(false);
    }
    
    // Stop all movement animations and trigger death
    if (enemyAnimator != null)
    {
       //Debug.Log($"TwoB: Animator found, current state: {enemyAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash}");
        
        // Force animator to be enabled and not in transition
        enemyAnimator.enabled = true;
        
        // Clear all boolean states first
        enemyAnimator.SetBool("isWalking", false);
        enemyAnimator.SetBool("isRunning", false);
        enemyAnimator.SetBool("Shoot", false);
        enemyAnimator.SetBool("Shoot1", false);
        
        // Wait one frame then trigger death
        StartCoroutine(TriggerDeathAfterFrame());
    }
    else
    {
       //Debug.LogError("TwoB: enemyAnimator is null - cannot play death animation!");
    }
    
    // Start the teleport sequence after death animation
    StartCoroutine(DelayedTeleportToTown());
   //Debug.Log($"Boss {bossName}: DelayedTeleportToTown coroutine started");
    
    // Don't destroy here - let Target component handle it with proper timing
    // DON'T call StopAllCoroutines() here - let teleport coroutine complete
  }

  
  /// Compatibility method for BossAIController (lowercase version)
  
  public void playDeathAnimation()
  {
    PlayDeathAnimation();
  }

  
  /// Swap function to enable gun and play swap animation
  
  public void Swap()
  {
    // Enable gun object
    if (gunObject != null)
    {
        gunObject.SetActive(true);
    }

    // Play swap animation trigger
    if (enemyAnimator != null)
    {
        enemyAnimator.SetTrigger("Swap");
    }

    ////Debug.Log($"{bossName} performed weapon swap!");
  }

  
  /// Call this method to activate the boss AI after dialogue completion or trigger
  
  public void ActivateBoss()
  {
    ////Debug.Log($"{bossName} battle initiated!");
    
    // Play activation sound
    if (activationSound != null && battleMusicTrigger != null)
    {
        battleMusicTrigger.clip = activationSound;
        battleMusicTrigger.Play();
    }

    // Play intimidation effect
    if (intimidationEffect != null)
    {
        intimidationEffect.Play();
    }

    // Perform weapon swap (enable gun and play animation)
    Swap();

    // Start battle music (if different from activation sound)
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
            agent.speed = originalAgentSpeed > 0 ? originalAgentSpeed : 5f; // Increased default from 3.5f
        }
        
        ////Debug.Log($"{bossName} AI activated! Agent speed: {agent.speed}, On NavMesh: {agent.isOnNavMesh}");
    }
    
    // Disable invincibility when boss becomes active
    if (bossTarget != null)
    {
        bossTarget.DisableInvincibility();
    }
    
    ////Debug.Log($"{bossName} is now active and patrolling!");
  }

  
  /// Deactivate the boss AI
  
  public void DeactivateBoss()
  {
    isActive = false;
    if (agent != null)
    {
        agent.enabled = false;
    }
    
    // Disable gun object
    if (gunObject != null)
    {
        gunObject.SetActive(false);
    }
    
    ////Debug.Log($"{bossName} AI deactivated.");
  }

  
  /// Call this method when the boss dies to properly handle death state
  
  public void OnBossDeath()
  {
   //Debug.Log($"Boss {bossName} is dying - triggering death sequence");
    
    // Immediately stop all AI behavior but don't disable component yet
    isActive = false;
    
    // Stop any ongoing burst fire
    isFiring = false;
    burstOnCooldown = false;
    
    // Cancel all pending invokes (but not coroutines, as death animation needs them)
    CancelInvoke();
    
    // Start the teleport sequence BEFORE calling PlayDeathAnimation
    StartCoroutine(DelayedTeleportToTown());
    
    // Play the death animation (this will handle the delayed destruction)
   //Debug.Log($"Boss {bossName}: About to call PlayDeathAnimation()");
    PlayDeathAnimation();
   //Debug.Log($"Boss {bossName}: PlayDeathAnimation() called");
  }
  
  /// <summary>
  /// Trigger death animation after clearing all other states
  /// </summary>
  private System.Collections.IEnumerator TriggerDeathAfterFrame()
  {
    // Wait one frame to ensure booleans are cleared
    yield return null;
    
    if (enemyAnimator != null)
    {
       //Debug.Log("TwoB: About to trigger Death animation");
        enemyAnimator.SetTrigger("Death");
       //Debug.Log("TwoB: Death animation triggered!");
        
        // Check if the trigger was actually set
        yield return null;
        var currentState = enemyAnimator.GetCurrentAnimatorStateInfo(0);
       //Debug.Log($"TwoB: After death trigger, current state hash: {currentState.fullPathHash}, normalizedTime: {currentState.normalizedTime}");
    }
    
    // Disable the Target component to prevent immediate destruction
    Target target = GetComponent<Target>();
    if (target != null)
    {
        target.enabled = false;
        // Re-enable it after animation plays and start destruction
        StartCoroutine(ReEnableTargetAfterAnimation());
    }
  }

  /// <summary>
  /// Re-enable Target component after death animation plays to allow destruction
  /// </summary>
  private System.Collections.IEnumerator ReEnableTargetAfterAnimation()
  {
    // Wait for death animation to play (2 seconds should be enough)
    yield return new WaitForSeconds(2f);
    
    Target target = GetComponent<Target>();
    if (target != null)
    {
      target.enabled = true;
     //Debug.Log("TwoB: Target component re-enabled, destruction can proceed");
    }
  }

  /// <summary>
  /// Wait 3 seconds after death animation, then teleport to Town
  /// </summary>
  private System.Collections.IEnumerator DelayedTeleportToTown()
  {
    // Wait 4 seconds for death animation to complete and be visible
    yield return new WaitForSeconds(4f);
    
    // Find SceneEffects component in the scene (should be on environment, not enemy)
    SceneEffects sceneEffects = FindFirstObjectByType<SceneEffects>();
    if (sceneEffects != null)
    {
     //Debug.Log($"Boss {bossName} death complete - teleporting to Town");
      sceneEffects.TeleportToTown();
    }
    else
    {
     //Debug.LogError($"Boss {bossName} death: Could not find SceneEffects to teleport to Town! Make sure SceneEffects is on an environment GameObject, not the enemy.");
      
      // Fallback: Try to use GameManager or ProgrammaticBuildingEntry directly
      var buildingEntry = FindFirstObjectByType<ProgrammaticBuildingEntry>();
      if (buildingEntry != null)
      {
       //Debug.Log($"Boss {bossName} death: Using fallback - direct building entry to Town");
        buildingEntry.EnterBuilding("Town", "FrontOfBus");
      }
      else
      {
       //Debug.LogError($"Boss {bossName} death: No fallback available - player will not be teleported!");
      }
    }
    
    // Now that teleportation is initiated, stop all remaining coroutines
    StopAllCoroutines();
  }

  
  /// Debug visualization of detection ranges in Scene view
  
  private void OnDrawGizmosSelected()
  {
    // Draw attack range in red
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, attackRange);
    
    // Draw sight range in yellow
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, sightRange);
    
    // Draw chase range in cyan
    Gizmos.color = Color.cyan;
    Gizmos.DrawWireSphere(transform.position, chaseRange);
  }
  
  /// <summary>
  /// TEST METHOD - Call this to verify the Unity Event connection works
  /// </summary>
  public void TestDeathConnection()
  {
   //Debug.Log("TEST: twoBController.TestDeathConnection() was called successfully!");
   //Debug.Log("This confirms the Unity Event connection is working properly.");
   //Debug.Log("Now change the OnDeathAnimation event in Target component to call PlayDeathAnimation instead!");
  }
}