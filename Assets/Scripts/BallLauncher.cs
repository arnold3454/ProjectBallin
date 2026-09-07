using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;    
    public Transform spawnPoint;     

    [Header("Launcher Settings")]
    public float launchForce = 20000f; 
    private float baseLaunchForce;

    private void Awake()
    {
        // Store the initial force set in the Inspector
        baseLaunchForce = launchForce;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LaunchBall();
        }
    }

    void LaunchBall()
    {
        GameObject newBall = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
        
        Rigidbody rb = newBall.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(spawnPoint.forward * launchForce);
        }
    }

    // New method to scale the launch force
    public void ScaleLaunchForce(float multiplier)
    {
        launchForce = baseLaunchForce * multiplier;
    }
}