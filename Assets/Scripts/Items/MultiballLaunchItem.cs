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

    public override bool Use()
    {
        BallLauncher launcher = FindAnyObjectByType<BallLauncher>();
        if (launcher == null)
            return false;

        launcher.QueueAutoLaunch(ballsToLaunch);
        return true;
    }
}
