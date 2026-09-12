using UnityEngine;

[RequireComponent(typeof(BallHitboxTrigger))]
public class Bouncer : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float accelerationForce = 2f;

    private BallHitboxTrigger hitboxTrigger;

    private void Awake()
    {
        hitboxTrigger = GetComponent<BallHitboxTrigger>();
    }

    private void OnEnable()
    {
        if (hitboxTrigger == null)
            hitboxTrigger = GetComponent<BallHitboxTrigger>();

        hitboxTrigger.BallHit += BounceBall;
    }

    private void OnDisable()
    {
        if (hitboxTrigger != null)
            hitboxTrigger.BallHit -= BounceBall;
    }

    private void BounceBall(Collider ballCollider)
    {
        Rigidbody ballBody = ballCollider.attachedRigidbody;
        if (ballBody == null)
            return;

        Collider bouncerCollider = GetComponent<Collider>();
        Vector3 bouncerCenter = bouncerCollider != null
            ? bouncerCollider.bounds.center
            : transform.position;
        Vector3 awayDirection = ballCollider.bounds.center - bouncerCenter;

        if (awayDirection.sqrMagnitude < 0.0001f)
            awayDirection = transform.up;

        awayDirection.Normalize();
        Vector3 velocity = ballBody.linearVelocity;
        float currentSpeed = velocity.magnitude;

        // Point the ball directly away from the side of the bouncer it touched.
        ballBody.linearVelocity = awayDirection * currentSpeed;
        ballBody.AddForce(awayDirection * accelerationForce, ForceMode.Impulse);
    }
}