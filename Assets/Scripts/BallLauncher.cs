using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;    
    public Transform spawnPoint;     

    [Header("Launcher Settings")]
    public float launchForce = 20000f; 

    void Update()
    {

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
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
}
