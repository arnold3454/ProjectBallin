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
[CreateAssetMenu(fileName = "BallTypeInfo", menuName = "Ball Types/Ball Type")]
public class BallTypeInfo : ScriptableObject
{
    /// <summary>Key the Split ball uses to duplicate the ball in play.</summary>
    public const Key SplitKey = Key.E;

    [SerializeField] private BallType type;
    [SerializeField] private string displayName;
    [SerializeField, TextArea(2, 5)] private string flavor;
    [SerializeField] private Color color = Color.white;

    /// <summary>Base multiplier on every point scored, before any upgrades.</summary>
    [SerializeField, Min(0f)] private float pointsMultiplier = 1f;
    [SerializeField, Min(0f)] private float weightMultiplier = 1f;
    [SerializeField, Min(0f)] private float speedMultiplier = 1f;

    /// <summary>Physics bounciness (0-1) the ball gets. Negative leaves the prefab's material alone.</summary>
    [SerializeField, Range(-1f, 1f)] private float bounciness = -1f;

    public BallType Type => type;
    public string Name => displayName;
    public string Flavor => flavor;
    public Color Color => color;
    public float PointsMultiplier => pointsMultiplier;
    public float WeightMultiplier => weightMultiplier;
    public float SpeedMultiplier => speedMultiplier;
    public float Bounciness => bounciness;

    public string PointsText => "Base points: " + PointsMultiplier.ToString("0.##") + "x";
}
