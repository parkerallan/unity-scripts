using UnityEngine;
using UnityEngine.AI;

public class Companion : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform player;
    public float followDistance = 3f;
    public float stopDistance = 2f;
    public float runDistance = 8f;
    
    [Header("Animation")]
    public Animator animator;
    public string idleParam = "isIdle";
    public string walkParam = "isWalking";
    public string runParam = "isRunning";
    
    [Header("Auto-Find Player")]
    public bool autoFindPlayer = true;
    
    private NavMeshAgent agent;
    private bool isFollowing = false;
    private Vector3 lastPlayerPosition;
    private float playerStillTime = 0f;
    private float stillThreshold = 0.5f; // Time before considering player idle
    
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }
        
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (autoFindPlayer && player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }
    
    public void FollowPlayer()
    {
        isFollowing = true;
    }
    
    public void StopFollowing()
    {
        isFollowing = false;
        agent.ResetPath();
        SetIdle();
    }
    
    private void Update()
    {
        // Auto-find player if reference is lost (scene transitions, etc.)
        if (autoFindPlayer && player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                lastPlayerPosition = player.position;
                Debug.Log("Companion: Re-found player after scene transition");
            }
        }
        
        if (!isFollowing || player == null || agent == null)
            return;
            
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerIsMoving = IsPlayerMoving();
        
        // Determine what the companion should do
        if (distanceToPlayer > followDistance)
        {
            // Follow the player
            agent.SetDestination(player.position);
            
            // Set animation based on distance
            if (distanceToPlayer > runDistance)
            {
                SetRunning();
                agent.speed = 12f; // Increased from 8f for faster following
            }
            else
            {
                SetWalking();
                agent.speed = 6f; // Increased from 4f for faster following
            }
        }
        else if (distanceToPlayer <= stopDistance)
        {
            // Stop following, we're close enough
            agent.ResetPath();
            
            // If player is idle, companion should be idle too
            if (!playerIsMoving)
            {
                SetIdle();
            }
            else
            {
                SetWalking();
            }
        }
        
        // Update last known player position
        lastPlayerPosition = player.position;
    }
    
    private bool IsPlayerMoving()
    {
        if (player == null)
            return false;
            
        float playerMovement = Vector3.Distance(player.position, lastPlayerPosition);
        
        if (playerMovement < 0.1f)
        {
            playerStillTime += Time.deltaTime;
        }
        else
        {
            playerStillTime = 0f;
        }
        
        return playerStillTime < stillThreshold;
    }
    
    private void SetIdle()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", true);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", false);
    }
    
    private void SetWalking()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", false);
        animator.SetBool("isWalking", true);
        animator.SetBool("isRunning", false);
    }
    
    private void SetRunning()
    {
        if (animator == null) return;
        animator.SetBool("isIdle", false);
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", true);
    }
}
