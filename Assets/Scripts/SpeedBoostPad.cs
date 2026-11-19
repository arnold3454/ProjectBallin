using UnityEngine;

[RequireComponent(typeof(BallHitboxTrigger))]
public class SpeedBoostPad : MonoBehaviour
{
    [SerializeField, Min(1f)] private float speedMultiplier = 1.35f;
    [SerializeField, Min(0.01f)] private float maxHorizontalSpeed = 40f;

    private BallHitboxTrigger hitbox;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();
    }

    private void OnEnable()
    {
        if (hitbox == null)
            hitbox = GetComponent<BallHitboxTrigger>();

        hitbox.BallHit += BoostBall;
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.BallHit -= BoostBall;
    }

    private void BoostBall(Collider ballCollider)
    {
        Rigidbody body = ballCollider.attachedRigidbody;
        if (body == null)
            return;

        Vector3 velocity = body.linearVelocity;
        Vector3 horizontalVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        float boostedSpeed = Mathf.Min(
            horizontalVelocity.magnitude * speedMultiplier,
            maxHorizontalSpeed);

        body.linearVelocity =
            horizontalVelocity.normalized * boostedSpeed
            + Vector3.Project(velocity, Vector3.up);
    }
}
