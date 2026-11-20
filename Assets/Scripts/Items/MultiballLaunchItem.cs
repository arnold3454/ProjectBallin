using UnityEngine;

/// <summary>
/// Multiball: the launcher fires an extra ball into play on its own. If the
/// player's ball is still waiting on the plunger, that ball is fired too and
/// the extra ball follows it up the lane.
/// </summary>
[CreateAssetMenu(fileName = "MultiballLaunchItem", menuName = "ProjectBallin/Items/Multiball (Launch)")]
public class MultiballLaunchItem : ItemDefinition
{
    [SerializeField, Min(1)] private int ballsToLaunch = 1;

    public override string Description => ballsToLaunch == 1
        ? "The launcher fires an extra ball into play."
        : $"The launcher fires {ballsToLaunch} extra balls into play.";

    public override bool Use()
    {
        BallLauncher launcher = FindAnyObjectByType<BallLauncher>();
        if (launcher == null)
            return false;

        int effectiveBalls = Mathf.Max(1, Mathf.RoundToInt(ballsToLaunch * EffectMultiplier));
        launcher.QueueAutoLaunch(effectiveBalls);
        return true;
    }
}
