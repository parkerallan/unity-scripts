using UnityEngine;

public class PlayerPersistence : MonoBehaviour
{
    private static PlayerPersistence instance;

    void Awake()
    {
        // Check if an instance already exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Mark this GameObject to not be destroyed
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instances
        }
    }
}
