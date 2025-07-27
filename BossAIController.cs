using UnityEngine;
using UnityEngine.AI;

public class BossAIController : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    [Header("Boss Settings")]
    public string bossName = "Boss";
    public bool isActive = false; // Boss AI is inactive until triggered
    public float activationDelay = 2f; // Delay after activation for dramatic effect

    [Header("Combat Stats")]
    public float timeBetweenAttacks = 1.5f; // Faster than regular enemies
    public float attackAccuracy = 0.85f; // Higher accuracy than regular enemies
    public float attackDamage = 40f; // Higher damage
    public LayerMask attackLayers = -1;

    [Header("Boss Phases")]
    public bool useHealthPhases = true;
    public float phase2HealthThreshold = 0.6f; // 60% health
    public float phase3HealthThreshold = 0.3f; // 30% health
    public float phase2AttackSpeedMultiplier = 1.3f;
    public float phase3AttackSpeedMultiplier = 1.6f;
    public float phase2AccuracyBonus = 0.1f;
    public float phase3AccuracyBonus = 0.15f;

    [Header("Movement")]
    public float sightRange = 25f; // Larger sight range
    public float attackRange = 15f; // Longer attack range
    public float circleStrafingRadius = 8f;
    public float strafingSpeed = 3f;
    public bool enableCircleStrafing = true;

    [Header("Aggression System")]
    public float maxAggression = 100f;
    public float aggressionDecayRate = 5f;
    public float aggressionOnHit = 25f;
    public float aggressionThresholdForRush = 70f;
    public float rushSpeed = 8f;

    [Header("Attack Effects")]
    public AudioSource attackSFX;
    public ParticleSystem muzzleFlash;
    public ParticleSystem casingEffect;

    [Header("Boss Specific Effects")]
    public AudioSource battleMusicTrigger;
    public ParticleSystem intimidationEffect; // Plays when boss activates
    public AudioClip activationSound;
    public AudioClip phaseChangeSound;

    [Header("Animation Controller Reference")]
    public MonoBehaviour animationController; // Reference to oneB, oneC, twoB controllers

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
        agent = GetComponent<NavMeshAgent>();
        bossTarget = GetComponent<Target>();
        
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

        float healthPercentage = bossTarget.health / 100f; // Assuming max health is 100
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
        AddAggression(30f);
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
        agent.SetDestination(targetPosition);

        // Look at player while strafing
        Vector3 lookDirection = (player.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(lookDirection);
    }

    private void RushPlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        agent.speed = rushSpeed;
        agent.SetDestination(player.position);
        
        Debug.Log($"{bossName} is rushing the player with high aggression!");
    }

    private void ChasePlayer()
    {
        if (player == null || !agent.isOnNavMesh || !agent.enabled) return;

        agent.speed = agent.speed; // Reset to normal speed
        agent.SetDestination(player.position);
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
            // Very accurate shot with minimal deviation
            Vector3 deviation = new Vector3(
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f),
                Random.Range(-0.05f, 0.05f)
            );
            attackDirection = (baseDirection + deviation).normalized;
            Debug.Log($"{bossName} attack: ACCURATE SHOT!");
        }
        else
        {
            // Miss - but still more accurate than regular enemies
            Vector3 missDeviation = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.3f, 0.3f),
                Random.Range(-0.5f, 0.5f)
            );
            attackDirection = (baseDirection + missDeviation).normalized;
            Debug.Log($"{bossName} attack: MISSED!");
        }

        // Perform raycast attack
        Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
        RaycastHit hit;
        float maxAttackRange = 50f;

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
    /// Trigger death animation through the assigned animation controller
    /// </summary>
    public void TriggerDeathAnimation()
    {
        if (animationController != null)
        {
            // Try different controller types
            if (animationController is oneBController oneB)
            {
                oneB.playDeathAnimation();
            }
            else if (animationController is oneCController oneC)
            {
                oneC.PlayDeathAnimation();
            }
            else if (animationController is twoBController twoB)
            {
                twoB.playDeathAnimation();
            }
            else
            {
                Debug.LogWarning($"Unknown animation controller type: {animationController.GetType()}");
            }
        }
        else
        {
            Debug.LogWarning("No animation controller assigned to boss!");
        }
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
