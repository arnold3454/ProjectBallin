using UnityEngine;

/// <summary>
/// The dealer's chip rain on the HUD. Lives on the ChipRain object under the Canvas, which
/// stretches over the whole screen and sits above the HUD but below the pause, upgrade, shop and
/// end-of-game panels. Its children are the chips themselves, all switched off in the scene and
/// assigned to <see cref="chips"/> in the Inspector; nothing is created at runtime. The Club
/// phase (on the Boss prefab, so it can't hold a scene reference) finds this through
/// <see cref="Instance"/> and asks for one chip at a time.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ChipRain : MonoBehaviour
{
    public static ChipRain Instance { get; private set; }

    [Tooltip("The PokerChip objects under this one, switched off. Each drop switches one on; there can never be more on screen than are listed here.")]
    [SerializeField] private PokerChip[] chips;

    private RectTransform rect;

    private void Awake()
    {
        Instance = this;
        rect = (RectTransform)transform;

        if (chips == null || chips.Length == 0)
            Debug.LogWarning("ChipRain has no chips assigned, so the Club phase has nothing to drop.", this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>How many chips are on screen right now.</summary>
    public int FallingCount
    {
        get
        {
            int count = 0;
            if (chips == null)
                return count;

            foreach (PokerChip chip in chips)
            {
                if (chip != null && chip.IsFalling)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Drops one chip from just above the top of the screen at a random spot that isn't on top of
    /// a chip still near the top. Sizes are in canvas units (pixels at the reference resolution).
    /// Returns false when every chip is already falling or no free spot was found.
    /// </summary>
    /// <param name="radius">Chip radius.</param>
    /// <param name="fallSpeed">Units per second down the screen.</param>
    /// <param name="chipColor">Tint of the chip face.</param>
    /// <param name="sideMargin">Keep chip centres this far inside the left and right screen edges.</param>
    /// <param name="vanishHeight">Chips vanish once their centre is this far above the bottom of the screen. 0 lets them fall right off the bottom.</param>
    /// <param name="attempts">Random spots to try before giving up.</param>
    /// <param name="maxSpin">Fastest a chip may turn as it falls, in degrees per second.</param>
    public bool TryDrop(float radius, float fallSpeed, Color chipColor, float sideMargin, float vanishHeight, int attempts, float maxSpin)
    {
        PokerChip chip = FindFreeChip();
        if (chip == null)
            return false;

        Rect area = rect.rect;
        float halfWidth = Mathf.Max(0f, area.width * 0.5f - sideMargin);
        float startY = radius;
        float vanishY = vanishHeight > 0f ? -(area.height - vanishHeight) : -(area.height + radius);

        for (int i = 0; i < Mathf.Max(1, attempts); i++)
        {
            Vector2 start = new Vector2(Random.Range(-halfWidth, halfWidth), startY);
            if (Overlaps(start, radius))
                continue;

            chip.Drop(start, radius, fallSpeed, vanishY, chipColor, Random.Range(-maxSpin, maxSpin));
            return true;
        }

        return false;
    }

    /// <summary>Takes every chip off the screen at once. Called when the fight ends.</summary>
    public void Clear()
    {
        if (chips == null)
            return;

        foreach (PokerChip chip in chips)
        {
            if (chip != null && chip.IsFalling)
                chip.Hide();
        }
    }

    private PokerChip FindFreeChip()
    {
        if (chips == null)
            return null;

        foreach (PokerChip chip in chips)
        {
            if (chip != null && !chip.IsFalling)
                return chip;
        }
        return null;
    }

    private bool Overlaps(Vector2 point, float radius)
    {
        foreach (PokerChip other in chips)
        {
            if (other == null || !other.IsFalling)
                continue;

            float minDistance = radius + other.Radius;
            if ((other.Position - point).sqrMagnitude < minDistance * minDistance)
                return true;
        }
        return false;
    }
}
