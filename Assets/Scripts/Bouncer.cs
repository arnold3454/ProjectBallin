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

        ScoreManager.Instance.AddScore(1);

        Collider bouncerCollider = GetComponent<Collider>();
        Vector3 bouncerCenter = bouncerCollider != null
            ? bouncerCollider.bounds.center
            : transform.position;
        Vector3 awayDirection = Vector3.ProjectOnPlane(
            ballCollider.bounds.center - bouncerCenter,
            Vector3.up);

        if (awayDirection.sqrMagnitude < 0.0001f)
        {
            awayDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (awayDirection.sqrMagnitude < 0.0001f)
                awayDirection = Vector3.right;
        }

        awayDirection.Normalize();
        Vector3 velocity = ballBody.linearVelocity;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float currentSpeed = horizontalVelocity.magnitude;
        float verticalVelocity = Mathf.Min(velocity.y, 0f);

        ballBody.linearVelocity = awayDirection * currentSpeed + Vector3.up * verticalVelocity;
        ballBody.AddForce(awayDirection * accelerationForce * BallTypeManager.SpeedMultiplier, ForceMode.Impulse);
    }
}