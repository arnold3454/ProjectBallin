using UnityEngine;

/// <summary>Adds seconds to the game clock.</summary>
[CreateAssetMenu(fileName = "TimeExtendItem", menuName = "ProjectBallin/Items/Time Extend")]
public class TimeExtendItem : ItemDefinition
{
    [SerializeField] private float secondsToAdd = 15f;

    public override bool Use()
    {
        if (GameTimer.Instance == null)
            return false;

        GameTimer.Instance.AddTime(secondsToAdd);
        return true;
    }
}
