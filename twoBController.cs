using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.AI;

public class twoBController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator twoBAnimator;

    [Header("Boss AI Settings")]
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Boss Settings")]
    public string bossName = "Two-B";
    public bool isActive = false; // Boss AI is inactive until triggered
    public float activationDelay = 2f; // Delay after activation for dramatic effect

    [Header("Combat Stats")]
    public float timeBetweenAttacks = 1.8f; // Slightly slower but more powerful
    public float attackAccuracy = 0.8f; // Good accuracy
    public float attackDamage = 35f; // Moderate damage
    public LayerMask attackLayers = -1;

    [Header("Boss Phases")]
    public bool useHealthPhases = true;
    public float phase2HealthThreshold = 0.6f; // 60% health
    public float phase3HealthThreshold = 0.3f; // 30% health
    public float phase2AttackSpeedMultiplier = 1.25f;
    public float phase3AttackSpeedMultiplier = 1.5f;
    public float phase2AccuracyBonus = 0.1f;
    public float phase3AccuracyBonus = 0.15f;

    [Header("Movement")]
    public float sightRange = 22f; // Moderate sight range
    public float attackRange = 12f; // Moderate attack range
    public float circleStrafingRadius = 7f;
    public float strafingSpeed = 2.5f;
    public bool enableCircleStrafing = true;

    [Header("Aggression System")]
    public float maxAggression = 100f;
    public float aggressionDecayRate = 6f; // Moderate decay
    public float aggressionOnHit = 20f; // Moderate aggression gain
    public float aggressionThresholdForRush = 75f; // Takes more to trigger rush
    public float rushSpeed = 7f;

    [Header("Attack Effects")]
    public AudioSource attackSFX;
    public ParticleSystem muzzleFlash;
    public ParticleSystem casingEffect;

    [Header("Boss Specific Effects")]
    public AudioSource battleMusicTrigger;
    public ParticleSystem intimidationEffect; // Plays when boss activates
    public AudioClip activationSound;
    public AudioClip phaseChangeSound;

    [Header("Two-B Specific Animations")]
    public string walkAnimationTrigger = "Walk";
    public string attackAnimationTrigger = "Attack";
    public string rushAnimationTrigger = "Rush";
    public string idleAnimationTrigger = "Idle";

    // Private variables
    private bool alreadyAttacked;
    private float currentAggression = 0f;
    private int currentPhase = 1;
    private Target bossTarget;
    private float originalTimeBetweenAttacks;
    private float originalAttackAccuracy;
    private bool playerInSightRange, playerInAttackRange;
    private bool isRushing = false;
    private float strafingAngle = 0f;

    private void Awake()
    {
        player = GameObject.Find("CharModel1").transform;
        
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
            
        bossTarget = GetComponent<Target>();
        
        // Auto-assign animator if not set
        if (twoBAnimator == null)
            twoBAnimator = GetComponent<Animator>();
        
        // Store original values
        originalTimeBetweenAttacks = timeBetweenAttacks;
        originalAttackAccuracy = attackAccuracy;

        // Start inactive
        if (agent != null)
            agent.enabled = false;
    }

    private void Update()
    {
        if (!isActive) return;

        UpdateRangeChecks();
        UpdateAggression();
        UpdatePhases();
        UpdateBehavior();
    }

    private void UpdateRangeChecks()
    {
        playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
        playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);
    }

    private void UpdateAggression()
    {
        // Decay aggression over time
        if (currentAggression > 0)
        {
            currentAggression -= aggressionDecayRate * Time.deltaTime;
            currentAggression = Mathf.Max(0, currentAggression);
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
        Debug.Log($"{bossName} entered Phase {currentPhase}!");
        
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
        AddAggression(25f);
    }

    private void UpdateBehavior()
    {
        // Determine if boss should rush the player
        isRushing = currentAggression >= aggressionThresholdForRush;

        if (playerInAttackRange && playerInSightRange)
        {
            AttackPlayer();
        }
        else if (playerInSightRange)
        {
            if (isRushing)
            {
                RushPlayer();
            }
            else if (enableCircleStrafing && !playerInAttackRange)
            {
                CircleStrafing();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            // Player not in sight, play idle animation
            PlayIdleAnimation();
        }
    }

    private void CircleStrafing()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Calculate position to circle around player
        strafingAngle += strafingSpeed * Time.deltaTime;
        Vector3 offset = new Vector3(
            Mathf.Sin(strafingAngle) * circleStrafingRadius,
            0,
            Mathf.Cos(strafingAngle) * circleStrafingRadius
        );
        
        Vector3 targetPosition = player.position + offset;
        agent.speed = 3.5f; // Ensure speed is set
        agent.SetDestination(targetPosition);

        // Look at player while strafing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);

        // Play walk animation while circle strafing
        PlayWalkAnimation();
    }

    private void RushPlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        agent.speed = rushSpeed;
        agent.SetDestination(player.position);
        
        Debug.Log($"{bossName} is rushing the player with high aggression!");
        
        // Play rush animation
        PlayRushAnimation();
    }

    private void ChasePlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        // Set normal movement speed
        agent.speed = 3.5f;
        agent.SetDestination(player.position);
        
        // Play walk animation while chasing
        PlayWalkAnimation();
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

        if (!alreadyAttacked)
        {
            // Play attack animation
            PlayAttackAnimation();
            
            PerformRaycastAttack();
            alreadyAttacked = true;
            Invoke(nameof(ResetAttack), timeBetweenAttacks);
        }
    }

    private void PerformRaycastAttack()
    {
        if (player == null) return;

        // Play attack sound effect
        if (attackSFX != null)
        {
            attackSFX.Play();
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
            // Accurate shot with small deviation
            Vector3 deviation = new Vector3(
                Random.Range(-0.07f, 0.07f),
                Random.Range(-0.07f, 0.07f),
                Random.Range(-0.07f, 0.07f)
            );
            attackDirection = (baseDirection + deviation).normalized;
            Debug.Log($"{bossName} attack: SOLID HIT!");
        }
        else
        {
            // Miss - moderate accuracy
            Vector3 missDeviation = new Vector3(
                Random.Range(-0.7f, 0.7f),
                Random.Range(-0.4f, 0.4f),
                Random.Range(-0.7f, 0.7f)
            );
            attackDirection = (baseDirection + missDeviation).normalized;
            Debug.Log($"{bossName} attack: MISSED!");
        }

        // Perform raycast attack
        Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
        RaycastHit hit;
        float maxAttackRange = 45f;

        if (Physics.Raycast(attackRay, out hit, maxAttackRange, attackLayers))
        {
            Debug.Log($"{bossName} raycast hit: {hit.collider.name}");

            if (hit.collider.CompareTag("Player"))
            {
                Debug.Log($"Player hit by {bossName} for {attackDamage} damage!");

                var playerTarget = hit.collider.GetComponent<Target>();
                if (playerTarget != null)
                {
                    playerTarget.TakeDamage(attackDamage);
                    Debug.Log($"{bossName} successfully dealt {attackDamage} damage to player!");
                }
                else
                {
                    Debug.LogWarning("Player hit but no Target component found!");
                }
            }

            Debug.DrawRay(attackRay.origin, attackDirection * hit.distance, Color.red, 1f);
        }
        else
        {
            Debug.DrawRay(attackRay.origin, attackDirection * maxAttackRange, Color.yellow, 1f);
            Debug.Log($"{bossName} shot missed completely!");
        }
    }

    private void ResetAttack()
    {
        alreadyAttacked = false;
    }

    // Animation Methods - Customize these for Two-B specific animations
    private void PlayWalkAnimation()
    {
        if (twoBAnimator != null && !string.IsNullOrEmpty(walkAnimationTrigger))
        {
            twoBAnimator.SetTrigger(walkAnimationTrigger);
        }
    }

    private void PlayAttackAnimation()
    {
        if (twoBAnimator != null && !string.IsNullOrEmpty(attackAnimationTrigger))
        {
            twoBAnimator.SetTrigger(attackAnimationTrigger);
        }
    }

    private void PlayRushAnimation()
    {
        if (twoBAnimator != null && !string.IsNullOrEmpty(rushAnimationTrigger))
        {
            twoBAnimator.SetTrigger(rushAnimationTrigger);
        }
    }

    private void PlayIdleAnimation()
    {
        if (twoBAnimator != null && !string.IsNullOrEmpty(idleAnimationTrigger))
        {
            twoBAnimator.SetTrigger(idleAnimationTrigger);
        }
    }

    public void playDeathAnimation()
    {
        if (twoBAnimator != null)
        {
            twoBAnimator.SetTrigger("Death");
        }
        else
        {
            Debug.LogError("twoBAnimator is not assigned in twoBController.");
        }
    }

    /// <summary>
    /// Call this method to activate the boss AI after dialogue completion
    /// </summary>
    public void ActivateBoss()
    {
        Debug.Log($"{bossName} battle initiated!");
        
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
            
            Debug.Log($"{bossName} AI activated! Agent speed: {agent.speed}, On NavMesh: {agent.isOnNavMesh}");
        }
        
        Debug.Log($"{bossName} is now active and hunting the player!");
    }

    /// <summary>
    /// Call this method when the boss is hit by the player
    /// </summary>
    public void OnHitByPlayer()
    {
        AddAggression(aggressionOnHit);
        Debug.Log($"{bossName} hit by player - aggression increased to {currentAggression}!");
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
        Debug.Log($"{bossName} AI deactivated.");
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