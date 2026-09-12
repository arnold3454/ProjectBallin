using UnityEngine;
using UnityEngine.Events;

public class BallHitboxTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnce;

    [Header("Events")]
    [SerializeField] private UnityEvent onBallHit;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
            return;

        if (!other.CompareTag("Ball"))
            return;

        hasTriggered = true;
        onBallHit?.Invoke();
    }
}