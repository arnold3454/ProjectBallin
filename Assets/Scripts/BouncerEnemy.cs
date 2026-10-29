using UnityEngine;

/// <summary>
/// The Bouncer: the basic enemy for the first level. Patrols like any enemy and dies in two
/// hits, but every ball contact forces a table nudge in a random direction, exactly as if the
/// player had hit the left or right shift key, tilt build-up included. Lives on the
/// BouncerEnemy prefab; EnemySpawners with that prefab assigned keep them on the table.
/// </summary>
public class BouncerEnemy : Enemy
{
    [Header("Bouncer")]
    [Tooltip("Force a random nudge on every ball hit.")]
    [SerializeField] private bool nudgeOnHit = true;

    protected override void OnBallHit(Collision collision)
    {
        if (!nudgeOnHit)
            return;

        NudgeController nudge = NudgeController.Instance;
        if (nudge == null)
            nudge = FindFirstObjectByType<NudgeController>();
        if (nudge == null)
        {
            Debug.LogWarning("Bouncer enemy found no NudgeController in the scene, so it can't nudge the table.", this);
            return;
        }

        nudge.NudgeRandom();
    }
}
