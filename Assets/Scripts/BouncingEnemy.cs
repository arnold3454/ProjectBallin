using UnityEngine;

public class BouncingEnemy : Enemy
{
    [Header("Bounce")]
    [SerializeField, Min(0f)] private float bounceForce = 4f;

    private Collider enemyCollider;

    private void OnEnable()
    {
        enemyCollider = GetComponent<Collider>();
    }

    protected override void OnBallHit(Collision collision)
    {
        Rigidbody ballBody = collision.rigidbody;
        if (ballBody == null)
            return;

        Vector3 enemyCenter = enemyCollider != null
            ? enemyCollider.bounds.center
            : transform.position;
        Vector3 awayDirection = Vector3.ProjectOnPlane(
            collision.collider.bounds.center - enemyCenter,
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
        float speedMultiplier = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.BallSpeedMultiplier
            : 1f;
        speedMultiplier *= BallTypeManager.SpeedMultiplier;

        ballBody.linearVelocity =
            awayDirection * horizontalVelocity.magnitude
            + Vector3.up * Mathf.Min(velocity.y, 0f);
        ballBody.AddForce(awayDirection * bounceForce * speedMultiplier, ForceMode.Impulse);
    }
}
