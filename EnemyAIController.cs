using UnityEngine;
using UnityEngine.AI;

public class EnemyAIController : MonoBehaviour
{
  public NavMeshAgent agent;
  public Transform player;
  public LayerMask whatIsGround, whatIsPlayer;

  // Patrolling
  public Vector3 walkPoint;
  bool walkPointSet;
  public float walkPointRange;

  // Attacking
  public float timeBetweenAttacks;
  bool alreadyAttacked;
  public float attackAccuracy = 0.7f; // 70% chance to hit (0.0 = always miss, 1.0 = always hit)
  public float attackDamage = 25f;
  public LayerMask attackLayers = -1; // What the raycast can hit
  
  [Header("Attack Effects")]
  public AudioSource attackSFX; // Optional sound effect when attacking
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

    // State machine logic - priority order matters!
    // If alerted, always try to chase/attack player regardless of sight range
    if (playerInAttackRange && (playerInSightRange || isAlerted)) 
    {
        AttackPlayer();
    }
    else if ((playerInSightRange || isAlerted) && !playerInAttackRange) 
    {
        ChasePlayer();
    }
    else if (!playerInSightRange && !playerInAttackRange && !isAlerted) 
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
    if (!walkPointSet) SearchWalkPoint();

    if (walkPointSet)
    {
      agent.SetDestination(walkPoint);

      Vector3 distanceToWalkPoint = transform.position - walkPoint;

      // Walkpoint reached
      if (distanceToWalkPoint.magnitude < 1f)
        walkPointSet = false;
    }
  }

  private void ChasePlayer()
  {
    if (player != null && agent.isOnNavMesh && agent.enabled)
    {
        agent.SetDestination(player.position);
    }
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
        casingEffect.Play();
    }

    // Calculate attack direction with accuracy variation
    Vector3 baseDirection = (player.position - transform.position).normalized;
    
    // Add inaccuracy based on attackAccuracy
    float hitRoll = Random.Range(0f, 1f);
    Vector3 attackDirection;
    
    if (hitRoll <= attackAccuracy)
    {
        // Accurate shot - small random deviation
        Vector3 deviation = new Vector3(
            Random.Range(-0.1f, 0.1f),
            Random.Range(-0.1f, 0.1f),
            Random.Range(-0.1f, 0.1f)
        );
        attackDirection = (baseDirection + deviation).normalized;
        Debug.Log("Enemy attack: ACCURATE SHOT!");
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
}