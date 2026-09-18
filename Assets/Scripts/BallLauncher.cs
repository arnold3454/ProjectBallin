using UnityEngine;
using UnityEngine.InputSystem;

public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;    
    public Transform spawnPoint;     

    void Update()
    {
        // No new balls once the clock has run out.
        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnBall();
        }
    }

    void SpawnBall()
    {
        if (ballPrefab == null || spawnPoint == null) return;

        // Instantiate the ball
        GameObject newBall = Instantiate(ballPrefab, spawnPoint.position, spawnPoint.rotation);

        // Apply current settings from BallSettingsManager if it exists
        if (BallSettingsManager.Instance != null)
        {
            float size = BallSettingsManager.Instance.CurrentSize;
            float weight = BallSettingsManager.Instance.CurrentWeight;

            newBall.transform.localScale = Vector3.one * size;

            Rigidbody rb = newBall.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = weight;
            }
        }
    }
}