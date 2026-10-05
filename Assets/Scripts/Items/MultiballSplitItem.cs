using UnityEngine;

/// <summary>
/// Multiball: splits the ball in play into two. The new ball spawns next to
/// the original and moves with the opposite momentum.
/// </summary>
[CreateAssetMenu(fileName = "MultiballSplitItem", menuName = "ProjectBallin/Items/Multiball (Split)")]
public class MultiballSplitItem : ItemDefinition
{
    [Tooltip("Space left between the two balls when the new one spawns, as a fraction of the ball's radius.")]
    [SerializeField] private float spawnGap = 0.1f;

    public override bool Use()
    {
        Rigidbody source = FindBallToSplit();
        if (source == null)
            return false;

        Vector3 velocity = source.linearVelocity;
        if (!TryFindSpawnPosition(source, velocity, out Vector3 position))
            return false;

        Rigidbody twin = Instantiate(source, position, source.rotation);
        twin.linearVelocity = -velocity;
        twin.angularVelocity = -source.angularVelocity;
        return true;
    }

    /// <summary>The fastest ball in play. A ball parked on the plunger doesn't count.</summary>
    private static Rigidbody FindBallToSplit()
    {
        Rigidbody fastest = null;

        foreach (GameObject ball in GameObject.FindGameObjectsWithTag("Ball"))
        {
            Rigidbody body = ball.GetComponent<Rigidbody>();
            if (body == null || body.isKinematic)
                continue;

            if (fastest == null || body.linearVelocity.sqrMagnitude > fastest.linearVelocity.sqrMagnitude)
                fastest = body;
        }

        return fastest;
    }

    private bool TryFindSpawnPosition(Rigidbody source, Vector3 velocity, out Vector3 position)
    {
        Collider sourceCollider = source.GetComponent<Collider>();
        float radius = sourceCollider != null ? sourceCollider.bounds.extents.x : 0.5f * source.transform.lossyScale.x;
        float distance = radius * (2f + spawnGap);

        // The new ball moves back the way the original came, so spawn it on that
        // side. If something is in the way, try either side instead. The offsets
        // stay flat on the table.
        Vector3 back = new Vector3(-velocity.x, 0f, -velocity.z);
        back = back.sqrMagnitude > 0.0001f ? back.normalized : Vector3.back;
        Vector3 side = Vector3.Cross(Vector3.up, back);

        foreach (Vector3 direction in new[] { back, side, -side })
        {
            position = source.position + direction * distance;

            // Check a slightly smaller sphere than the ball so the table surface doesn't count.
            if (!Physics.CheckSphere(position, radius * 0.9f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return true;
        }

        position = source.position;
        return false;
    }
}
