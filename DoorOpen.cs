using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    [Header("Door Settings")]
    public float openHeight = 3f;
    public float openSpeed = 2f;
    
    private bool isPlayerInRange = false;
    private bool isDoorOpen = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    
    private void Start()
    {
        closedPosition = transform.position;
        openPosition = closedPosition + Vector3.up * openHeight;
    }
    
    private void Update()
    {
        if (isPlayerInRange && !isDoorOpen)
        {
            // Open door
            transform.position = Vector3.Lerp(transform.position, openPosition, openSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, openPosition) < 0.1f)
            {
                isDoorOpen = true;
            }
        }
        else if (!isPlayerInRange && isDoorOpen)
        {
            // Close door
            transform.position = Vector3.Lerp(transform.position, closedPosition, openSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, closedPosition) < 0.1f)
            {
                isDoorOpen = false;
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
}
