using UnityEngine;

public class ScoreZone : MonoBehaviour
{
    [SerializeField] private int pointValue;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) 
        return;

        ScoreManager.Instance.AddScore(pointValue);
    }
}
