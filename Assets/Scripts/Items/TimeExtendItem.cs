using UnityEngine;

/// <summary>Adds seconds to the boss clock during a fight, or the game clock otherwise.</summary>
[CreateAssetMenu(fileName = "TimeExtendItem", menuName = "ProjectBallin/Items/Time Extend")]
public class TimeExtendItem : ItemDefinition
{
    [SerializeField] private float secondsToAdd = 15f;

    public override string Description => $"Adds {secondsToAdd:0} seconds to the active clock.";

    public override bool Use()
    {
        BossManager bossManager = BossManager.Instance;
        if (bossManager != null && bossManager.Timer != null && bossManager.Timer.IsRunning)
        {
            bossManager.Timer.AddTime(secondsToAdd);
            return true;
        }

        if (GameTimer.Instance == null)
            return false;

        GameTimer.Instance.AddTime(secondsToAdd);
        return true;
    }
}
