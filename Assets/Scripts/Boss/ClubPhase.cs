using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Club phase: chip rain. The dealer keeps dropping large chips onto the table. They are solid,
/// so the ball bounces off them, and they slide down toward the gutter by the bumpers, piling
/// up against walls, bumpers and each other and blocking lanes. Every ball hit on a chip scores
/// and a few hits break it. <see cref="intensity"/> sets how heavy the rain is: how fast chips
/// arrive, how many can be out at once and how big they are. Chips that are on the table when
/// the phase ends stay until they break or the fight ends.
/// </summary>
public class ClubPhase : BossPhase
{
    [Header("Chip Rain")]
    [Tooltip("Chip to drop (Prefabs/PokerChip). It should be one unit across at scale 1; the phase scales it to size.")]
    [SerializeField] private PokerChip chipPrefab;
    [Tooltip("How heavy the rain is, 0 to 1. 0 is a light sprinkle for an early mini phase; 1 covers roughly 40% of the playfield.")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0.5f;

    [Header("Intensity range (0 -> 1)")]
    [SerializeField, Min(0.1f)] private float spawnIntervalLight = 4f;
    [SerializeField, Min(0.1f)] private float spawnIntervalHeavy = 0.7f;
    [SerializeField, Min(1)] private int maxChipsLight = 5;
    [SerializeField, Min(1)] private int maxChipsHeavy = 30;
    [SerializeField, Min(0.2f)] private float chipRadiusLight = 1.4f;
    [SerializeField, Min(0.2f)] private float chipRadiusHeavy = 2.4f;

    [Header("Chips")]
    [Tooltip("How far above the table surface a chip's centre sits when it drops in. Half the prefab's thickness.")]
    [SerializeField, Min(0f)] private float dropHeight = 0.175f;
    [Tooltip("How fast chips slide down the table, in units per second.")]
    [SerializeField, Min(0.1f)] private float driftSpeed = 2.5f;
    [Tooltip("Chips stop this far above the bottom of the usable board, around the bumpers and flipper lane.")]
    [SerializeField] private float restDepthFromBottom = 12f;
    [Tooltip("Chips drop in this far below the top of the usable board.")]
    [SerializeField] private float spawnDepthFromTop = 4f;
    [Tooltip("Keep chips this far from the side walls when they drop in.")]
    [SerializeField] private float sideMargin = 2.5f;
    [Tooltip("Ball hits needed to break a chip.")]
    [SerializeField, Min(1)] private int hitsToBreak = 3;
    [Tooltip("Points for every ball hit on a chip.")]
    [SerializeField] private int hitPoints = 2;
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
        // Chips already on the table stay put until they break or the fight ends.
    }

    public override void Cleanup()
    {
        foreach (PokerChip chip in chips)
        {
            if (chip != null)
            {
                chip.Broke -= OnChipBroke;
                Destroy(chip.gameObject);
            }
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

        float restZ = Manager.BoardMin.y + restDepthFromBottom;
        Color color = chipColors != null && chipColors.Length > 0
            ? chipColors[Random.Range(0, chipColors.Length)]
            : Color.red;

        chip.Initialize(radius, driftSpeed, restZ, hitsToBreak, hitPoints, color);
        chip.Broke += OnChipBroke;
        chips.Add(chip);
    }

    /// <summary>Picks a clear spot along the drop line near the top of the board.</summary>
    private bool TryFindDropPoint(float radius, out Vector3 point)
    {
        Vector2 min = Manager.BoardMin;
        Vector2 max = Manager.BoardMax;
        float z = max.y - spawnDepthFromTop;
        float y = Manager.SurfaceHeight + dropHeight;
        float left = min.x + sideMargin + radius;
        float right = max.x - sideMargin - radius;
        Vector3 halfExtents = new Vector3(radius, 0.25f, radius);

        for (int i = 0; i < placementAttempts; i++)
        {
            point = new Vector3(Random.Range(left, right), y, z);

            bool blocked = false;
            foreach (Collider overlap in Physics.OverlapBox(point, halfExtents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (overlap.CompareTag("Board"))
                    continue;

                blocked = true;
                break;
            }

            if (!blocked)
                return true;
        }

        point = Vector3.zero;
        return false;
    }

    private void OnChipBroke(PokerChip chip)
    {
        chip.Broke -= OnChipBroke;
        chips.Remove(chip);
    }
}
