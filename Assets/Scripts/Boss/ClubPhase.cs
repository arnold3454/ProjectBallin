using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Club phase: chip rain. The dealer keeps dropping big chips over the table. They are purely
/// visual: they float above the playfield, hide the ball and everything else beneath them while
/// they slide down, and never collide with anything. Each chip vanishes once it reaches the line
/// just above the flippers. <see cref="intensity"/> sets how heavy the rain is: how fast chips
/// arrive, how many can be out at once and how big they are. Chips still falling when the phase
/// ends finish their fall.
/// </summary>
public class ClubPhase : BossPhase
{
    [Header("Chip Rain")]
    [Tooltip("Chip to drop (Prefabs/PokerChip). It should be one unit across at scale 1; the phase scales it to size.")]
    [SerializeField] private PokerChip chipPrefab;
    [Tooltip("How heavy the rain is, 0 to 1. 0 is a light sprinkle for an early mini phase; 1 blankets most of the playfield.")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0.5f;

    [Header("Intensity range (0 -> 1)")]
    [Tooltip("Seconds between chips at intensity 0.")]
    [SerializeField, Min(0.1f)] private float spawnIntervalLight = 1.3f;
    [Tooltip("Seconds between chips at intensity 1.")]
    [SerializeField, Min(0.1f)] private float spawnIntervalHeavy = 0.25f;
    [Tooltip("Most chips on the table at once at intensity 0.")]
    [SerializeField, Min(1)] private int maxChipsLight = 5;
    [Tooltip("Most chips on the table at once at intensity 1.")]
    [SerializeField, Min(1)] private int maxChipsHeavy = 30;
    [Tooltip("Chip radius in world units at intensity 0. The usable board is about 35 units wide.")]
    [SerializeField, Min(0.2f)] private float chipRadiusLight = 2.5f;
    [Tooltip("Chip radius in world units at intensity 1. The usable board is about 35 units wide.")]
    [SerializeField, Min(0.2f)] private float chipRadiusHeavy = 4.5f;

    [Header("Chips")]
    [Tooltip("How far above the table surface the chips float. Keep it above the top of the ball (about 1 unit) so chips pass over the ball and hide it instead of sitting on the table.")]
    [SerializeField, Min(0f)] private float hoverHeight = 2f;
    [Tooltip("How fast chips slide down the table, in units per second.")]
    [SerializeField, Min(0.1f)] private float driftSpeed = 7.5f;
    [Tooltip("Chips disappear once they reach this far above the bottom of the usable board: the line just above the flippers.")]
    [SerializeField] private float despawnDepthFromBottom = 12f;
    [Tooltip("Chips drop in this far below the top of the usable board.")]
    [SerializeField] private float spawnDepthFromTop = 4f;
    [Tooltip("Keep chips this far from the side walls when they drop in.")]
    [SerializeField] private float sideMargin = 2.5f;
    [Tooltip("How many random spots along the drop line to try before giving up on a chip that would land on top of another one.")]
    [SerializeField, Min(1)] private int placementAttempts = 8;
    [SerializeField] private Color[] chipColors =
    {
        new Color(0.85f, 0.12f, 0.12f),
        new Color(0.15f, 0.3f, 0.85f),
        new Color(0.1f, 0.6f, 0.25f),
        new Color(0.1f, 0.1f, 0.12f),
        new Color(0.9f, 0.9f, 0.85f),
    };

    /// <summary>How heavy the rain is, 0 to 1. Can be changed while the phase runs.</summary>
    public float Intensity
    {
        get => intensity;
        set => intensity = Mathf.Clamp01(value);
    }

    private readonly List<PokerChip> chips = new List<PokerChip>();
    private float spawnTimer;

    private float SpawnInterval => Mathf.Lerp(spawnIntervalLight, spawnIntervalHeavy, intensity);
    private int MaxChips => Mathf.RoundToInt(Mathf.Lerp(maxChipsLight, maxChipsHeavy, intensity));
    private float ChipRadius => Mathf.Lerp(chipRadiusLight, chipRadiusHeavy, intensity);

    protected override void OnBegin()
    {
        chips.RemoveAll(c => c == null);
        spawnTimer = 0.5f;
    }

    protected override void OnEnd()
    {
        // Chips already on the table finish their fall.
    }

    public override void Cleanup()
    {
        foreach (PokerChip chip in chips)
        {
            if (chip != null)
                Destroy(chip.gameObject);
        }
        chips.Clear();
    }

    private void Update()
    {
        if (!IsActive)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        spawnTimer = SpawnInterval;

        chips.RemoveAll(c => c == null);
        if (chips.Count >= MaxChips)
            return;

        SpawnChip();
    }

    private void SpawnChip()
    {
        if (chipPrefab == null || Manager == null)
        {
            Debug.LogWarning("Club phase needs the PokerChip prefab assigned.", this);
            return;
        }

        float radius = ChipRadius;
        if (!TryFindDropPoint(radius, out Vector3 point))
            return;

        PokerChip chip = Instantiate(chipPrefab, point, Quaternion.identity);

        float despawnZ = Manager.BoardMin.y + despawnDepthFromBottom;
        Color color = chipColors != null && chipColors.Length > 0
            ? chipColors[Random.Range(0, chipColors.Length)]
            : Color.red;

        chip.Initialize(radius, driftSpeed, despawnZ, color);
        chips.Add(chip);
    }

    /// <summary>
    /// Picks a spot along the drop line near the top of the board that doesn't overlap a chip
    /// still hanging around the line. Chips ignore walls and bumpers, so only other chips matter.
    /// </summary>
    private bool TryFindDropPoint(float radius, out Vector3 point)
    {
        Vector2 min = Manager.BoardMin;
        Vector2 max = Manager.BoardMax;
        float z = max.y - spawnDepthFromTop;
        float y = Manager.SurfaceHeight + hoverHeight;
        float left = min.x + sideMargin + radius;
        float right = max.x - sideMargin - radius;

        for (int i = 0; i < placementAttempts; i++)
        {
            point = new Vector3(Random.Range(left, right), y, z);
            if (!OverlapsChip(point, radius))
                return true;
        }

        point = Vector3.zero;
        return false;
    }

    private bool OverlapsChip(Vector3 point, float radius)
    {
        foreach (PokerChip other in chips)
        {
            if (other == null)
                continue;

            Vector3 delta = other.transform.position - point;
            delta.y = 0f;
            float minDistance = radius + other.Radius;
            if (delta.sqrMagnitude < minDistance * minDistance)
                return true;
        }

        return false;
    }
}
