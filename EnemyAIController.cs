using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
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
  public float patrolWaitTime = 2f; // Time to wait at each patrol point
  public float patrolSpeed = 1.5f; // Speed when patrolling (slower than chase speed)
  private float patrolWaitTimer = 0f;
  private bool isWaitingAtPatrolPoint = false;
  private float originalAgentSpeed;

  // Attacking
  public float timeBetweenAttacks;
  bool alreadyAttacked;
  public float attackAccuracy = 0.7f; // 70% chance to hit (0.0 = always miss, 1.0 = always hit)
  public float attackDamage = 25f;
  public LayerMask attackLayers = -1; // What the raycast can hit
  
  [Header("Attack Effects")]
  public AudioClip attackSFXClip; // Audio clip for attack sound
  public float attackSFXVolume = 1f; // Volume for attack sound
  public ParticleSystem muzzleFlash; // Optional particle effect at attack origin
  public ParticleSystem casingEffect; // Optional particle effect for bullet casing ejection

  // States
  public float sightRange, attackRange;
  public bool playerInSightRange, playerInAttackRange;
  
  // Alert system
  private bool isAlerted = false;
  private float alertDuration = 10f; // How long enemy stays alerted after being hit
  private float alertTimer = 0f;

  private void Awake()
  {
    player = GameObject.Find("CharModel1").transform;
    agent = GetComponent<NavMeshAgent>();
    
    // Store original agent speed for restoration
    if (agent != null)
    {
        originalAgentSpeed = agent.speed;
    }
    
    // Auto-assign animator if not manually set
    if (enemyAnimator == null)
        enemyAnimator = GetComponent<Animator>();
  }

  private void Update()
  {
    // Check for sight and attack range
    playerInSightRange = Physics.CheckSphere(transform.position, sightRange, whatIsPlayer);
    playerInAttackRange = Physics.CheckSphere(transform.position, attackRange, whatIsPlayer);

    // Handle alert timer
    if (isAlerted)
    {
        alertTimer -= Time.deltaTime;
        if (alertTimer <= 0f)
        {
            isAlerted = false;
            Debug.Log("Enemy is no longer alerted");
        }
    }

    // State machine logic - simplified to prevent freezing
    // If in attack range, always attack (regardless of sight - they're close enough!)
    if (playerInAttackRange)
    {
        AttackPlayer();
    }
    else if (playerInSightRange || isAlerted)
    {
        ChasePlayer();
    }
    else
    {
        Patroling();
    }
  }

  private void SearchWalkPoint()
  {
    // Calculate random point in range
    float randomZ = Random.Range(-walkPointRange, walkPointRange);
    float randomX = Random.Range(-walkPointRange, walkPointRange);

    walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

    if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
      walkPointSet = true;
  }

  private void Patroling()
  {
    // Set patrol speed (slower than normal)
    if (agent.speed != patrolSpeed)
    {
        agent.speed = patrolSpeed;
    }
    
    if (!walkPointSet) SearchWalkPoint();

    if (walkPointSet)
    {
      // Check if we're waiting at the patrol point
      if (isWaitingAtPatrolPoint)
      {
          patrolWaitTimer -= Time.deltaTime;
          
          // Stop moving while waiting
          if (agent.isOnNavMesh && agent.enabled)
          {
              agent.SetDestination(transform.position);
          }
          
          // Set idle animation while waiting
          if (enemyAnimator != null)
          {
              enemyAnimator.SetBool("isWalking", false);
              enemyAnimator.SetBool("isRunning", false);
              enemyAnimator.SetBool("isShooting", false);
          }
          
          if (patrolWaitTimer <= 0f)
          {
              isWaitingAtPatrolPoint = false;
              walkPointSet = false; // Find a new patrol point
          }
      }
      else
      {
          agent.SetDestination(walkPoint);
          
          Vector3 distanceToWalkPoint = transform.position - walkPoint;

          // Walkpoint reached - start waiting
          if (distanceToWalkPoint.magnitude < 1f)
          {
              isWaitingAtPatrolPoint = true;
              patrolWaitTimer = patrolWaitTime;
          }
          else
          {
              // Set walking animation while moving to patrol point
              if (enemyAnimator != null)
              {
                  enemyAnimator.SetBool("isWalking", true);
                  enemyAnimator.SetBool("isRunning", false);
                  enemyAnimator.SetBool("isShooting", false);
              }
          }
      }
    }
  }

  private void ChasePlayer()
  {
    // Restore original speed for chasing (faster than patrol)
    if (agent.speed != originalAgentSpeed)
    {
        agent.speed = originalAgentSpeed;
    }
    
    if (player != null && agent.isOnNavMesh && agent.enabled)
    {
        agent.SetDestination(player.position);
    }
    
    // Set running animation when chasing
    if (enemyAnimator != null)
    {
        enemyAnimator.SetBool("isWalking", false);
        enemyAnimator.SetBool("isRunning", true);
        enemyAnimator.SetBool("isShooting", false);
    }
  }

  private void AttackPlayer()
  {
    if (agent.isOnNavMesh && agent.enabled)
    {
        agent.SetDestination(transform.position); // Stop moving
    }

    // Only rotate horizontally (Y-axis) to face the player, don't tilt up/down
    if (player != null)
    {
        Vector3 directionToPlayer = (player.position - transform.position);
        directionToPlayer.y = 0; // Remove vertical component to prevent tilting
        
        if (directionToPlayer != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }
    }

    // Check line of sight before attacking
    if (!HasLineOfSight())
    {
        // No clear shot - switch to chasing instead
        ChasePlayer();
        return;
    }

    // Set shooting animation
    if (enemyAnimator != null)
    {
        enemyAnimator.SetBool("isWalking", false);
        enemyAnimator.SetBool("isRunning", false);
        enemyAnimator.SetBool("isShooting", true);
    }

    if (!alreadyAttacked)
    {
        PerformRaycastAttack();

        alreadyAttacked = true;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
    }
  }
  
  private bool HasLineOfSight()
  {
    if (player == null) return false;
    
    Vector3 rayOrigin = transform.position + Vector3.up * 1.5f; // Enemy eye level
    Vector3 directionToPlayer = (player.position + Vector3.up * 1.0f - rayOrigin).normalized; // Player chest level
    float distanceToPlayer = Vector3.Distance(rayOrigin, player.position + Vector3.up * 1.0f);
    
    // Cast ray to check for obstacles
    RaycastHit hit;
    if (Physics.Raycast(rayOrigin, directionToPlayer, out hit, distanceToPlayer, ~whatIsPlayer))
    {
        // Something is blocking the view
        return false;
    }
    
    return true;
  }
  
  private void PerformRaycastAttack()
  {
    if (player == null) return;

    // Play attack sound effect using SFXManager
    if (attackSFXClip != null && SFXManager.instance != null)
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
        // Scale deviation based on distance - closer targets get even more accurate shots
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float deviationScale = Mathf.Clamp(distanceToPlayer / 50f, 0.1f, 1f); // Less deviation at closer range
        
        Vector3 deviation = new Vector3(
            Random.Range(-0.05f, 0.05f) * deviationScale,
            Random.Range(-0.05f, 0.05f) * deviationScale,
            Random.Range(-0.05f, 0.05f) * deviationScale
        );
        attackDirection = (baseDirection + deviation).normalized;
        Debug.Log($"Enemy attack: ACCURATE SHOT! Distance: {distanceToPlayer:F1}m, Deviation Scale: {deviationScale:F2}");
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
        Debug.Log("Enemy attack: MISSED!");
    }

    // Perform raycast attack
    Ray attackRay = new Ray(transform.position + Vector3.up * 1.5f, attackDirection);
    RaycastHit hit;
    float attackRange = 50f; // Maximum attack range

    if (Physics.Raycast(attackRay, out hit, attackRange, attackLayers))
    {
        Debug.Log($"Enemy raycast hit: {hit.collider.name}");
        
        // Check if we hit the player
        if (hit.collider.CompareTag("Player"))
        {
            Debug.Log($"Player hit for {attackDamage} damage!");
            
            // Try to get Target component and deal damage
            var playerTarget = hit.collider.GetComponent<Target>();
            if (playerTarget != null)
            {
                playerTarget.TakeDamage(attackDamage);
                Debug.Log($"Successfully dealt {attackDamage} damage to player!");
            }
            else
            {
                Debug.LogWarning("Player hit but no Target component found!");
            }
        }
        
        // Visual feedback - you can add muzzle flash, bullet trail, etc.
        Debug.DrawRay(attackRay.origin, attackDirection * hit.distance, Color.red, 1f);
    }
    else
    {
        // Shot went into the void
        Debug.DrawRay(attackRay.origin, attackDirection * attackRange, Color.yellow, 1f);
        Debug.Log("Enemy shot missed completely!");
    }
  }
  
  private void ResetAttack()
  {
    alreadyAttacked = false;
  }
  
  /// <summary>
  /// Call this method when the enemy is hit by the player to make them alert and chase
  /// </summary>
  public void OnHitByPlayer()
  {
    isAlerted = true;
    alertTimer = alertDuration;
    Debug.Log("Enemy hit by player - now alerted and will chase!");
  }
  
  /// <summary>
  /// Call this method to trigger the death animation
  /// This can be invoked from Unity events (like from the Target component's OnDeathAnimation event)
  /// </summary>
  public void PlayDeathAnimation()
  {
    // Stop all AI behavior
    this.enabled = false;
    
    // Stop the NavMeshAgent
    if (agent != null && agent.enabled)
    {
        agent.isStopped = true;
        agent.enabled = false;
    }
    
    // Stop all movement animations and trigger death
    if (enemyAnimator != null)
    {
        enemyAnimator.SetBool("isWalking", false);
        enemyAnimator.SetBool("isRunning", false);
        enemyAnimator.SetBool("isShooting", false);
        enemyAnimator.SetTrigger("Death");
    }
    
    Debug.Log("Enemy death animation triggered");
  }
}