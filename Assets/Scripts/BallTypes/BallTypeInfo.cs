using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BallType
{
    Default,
    Bouncy,
    Split,
}

/// <summary>
/// Everything that makes one ball type different from the others. The start screen
/// writes its text from these numbers so the descriptions can't drift out of sync.
/// </summary>
public class BallTypeInfo
{
    /// <summary>Key the Split ball uses to duplicate the ball in play.</summary>
    public const Key SplitKey = Key.E;

    public readonly BallType Type;
    public readonly string Name;
    public readonly string Flavor;
    public readonly Color Color;

    /// <summary>Base multiplier on every point scored, before any upgrades.</summary>
    public readonly float PointsMultiplier;
    public readonly float WeightMultiplier;
    public readonly float SpeedMultiplier;

    /// <summary>Physics bounciness (0-1) the ball gets. Negative leaves the prefab's material alone.</summary>
    public readonly float Bounciness;

    private BallTypeInfo(BallType type, string name, string flavor, Color color, float points, float weight = 1f, float speed = 1f, float bounciness = -1f)
    {
        Type = type;
        Name = name;
        Flavor = flavor;
        Color = color;
        PointsMultiplier = points;
        WeightMultiplier = weight;
        SpeedMultiplier = speed;
        Bounciness = bounciness;
    }

    public string PointsText => "Base points: " + PointsMultiplier.ToString("0.##") + "x";

    public static readonly IReadOnlyList<BallTypeInfo> All = new[]
    {
        new BallTypeInfo(BallType.Default, "Default Ball",
            "The standard pinball. Balanced weight, speed and bounce, with no tricks and no penalties.",
            new Color(0.85f, 0.87f, 0.92f),
            points: 1f),

        new BallTypeInfo(BallType.Bouncy, "Bouncy Ball",
            "A ball specializing in speed. This red bouncy ball will allow the player to ricochet it across the board. " +
            "It has high speed, low weight, and increased bounciness.",
            new Color(0.88f, 0.1f, 0.1f),
            points: 0.75f, weight: 0.6f, speed: 1.25f, bounciness: 0.9f),

        new BallTypeInfo(BallType.Split, "Split Ball",
            "A ball specializing in multiplying. This deep blue ball will allow the player to duplicate it after the player " +
            $"activates it (press {SplitKey}). Its weight and speed are the same as regular pinball.",
            new Color(0.05f, 0.14f, 0.68f),
            points: 0.5f),
    };

    public static BallTypeInfo Get(BallType type)
    {
        foreach (BallTypeInfo info in All)
        {
            if (info.Type == type)
                return info;
        }

        return All[0];
    }
}
