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
        // Attack code here (e.g., shoot, melee, etc.)
        Debug.Log("Attacking Player!");

        alreadyAttacked = true;
        Invoke(nameof(ResetAttack), timeBetweenAttacks);
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