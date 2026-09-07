using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;    
    public Transform spawnPoint;     

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        // Spawns the ball at the spawn point without adding forward force
        Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}