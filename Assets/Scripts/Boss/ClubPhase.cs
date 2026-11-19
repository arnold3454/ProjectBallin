using UnityEngine;

/// <summary>
/// Club phase: chip rain. The dealer keeps dropping big chips down the screen. They are HUD
/// elements, not board objects: each one slides over the table and the rest of the HUD and hides
/// whatever is behind it, without ever touching the ball. The chips themselves are the PokerChip
/// objects under the Canvas's ChipRain; this phase only decides when the next one drops and how
/// big it is. <see cref="intensity"/> sets how heavy the rain is: how fast chips arrive, how many
/// can be out at once and how big they are. Chips still falling when the phase ends finish their
/// fall. All sizes and speeds are in canvas units: pixels at the canvas's 1920 x 1080 reference.
/// </summary>
public class ClubPhase : BossPhase
{
    [Header("Chip Rain")]
    [Tooltip("How heavy the rain is, 0 to 1. 0 is a light sprinkle for an early mini phase; 1 blankets most of the screen.")]
    [SerializeField, Range(0f, 1f)] private float intensity = 0.5f;

    [Header("Intensity range (0 -> 1)")]
    [Tooltip("Seconds between chips at intensity 0.")]
    [SerializeField, Min(0.1f)] private float spawnIntervalLight = 1.3f;
    [Tooltip("Seconds between chips at intensity 1.")]
    [SerializeField, Min(0.1f)] private float spawnIntervalHeavy = 0.25f;
    [Tooltip("Most chips on screen at once at intensity 0.")]
    [SerializeField, Min(1)] private int maxChipsLight = 5;
    [Tooltip("Most chips on screen at once at intensity 1. Never more than there are chips under ChipRain.")]
    [SerializeField, Min(1)] private int maxChipsHeavy = 30;
    [Tooltip("Chip radius at intensity 0, in canvas units. The screen is 1920 wide and the table about 700.")]
    [SerializeField, Min(10f)] private float chipRadiusLight = 130f;
    [Tooltip("Chip radius at intensity 1, in canvas units. The screen is 1920 wide and the table about 700.")]
    [SerializeField, Min(10f)] private float chipRadiusHeavy = 210f;

    [Header("Chips")]
    [Tooltip("How fast chips slide down the screen, in canvas units per second. The screen is 1080 tall.")]
    [SerializeField, Min(1f)] private float fallSpeed = 260f;
    [Tooltip("Chips vanish once their centre is this far above the bottom of the screen. 0 lets them fall right off the bottom; about 330 stops them just above the flippers.")]
    [SerializeField, Min(0f)] private float vanishHeightFromBottom = 0f;
    [Tooltip("Keep chip centres this far inside the left and right edges of the screen.")]
    [SerializeField] private float sideMargin = 60f;
    [Tooltip("Fastest a chip may turn as it falls, in degrees per second.")]
    [SerializeField, Min(0f)] private float maxSpin = 25f;
    [Tooltip("How many random spots along the top of the screen to try before giving up on a chip that would land on top of another one.")]
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

    private float spawnTimer;
    private bool warnedMissingRain;

    private float SpawnInterval => Mathf.Lerp(spawnIntervalLight, spawnIntervalHeavy, intensity);
    private int MaxChips => Mathf.RoundToInt(Mathf.Lerp(maxChipsLight, maxChipsHeavy, intensity));
    private float ChipRadius => Mathf.Lerp(chipRadiusLight, chipRadiusHeavy, intensity);

    protected override void OnBegin()
    {
        spawnTimer = 0.5f;
    }

    protected override void OnEnd()
    {
        // Chips already on screen finish their fall.
    }

    public override void Cleanup()
    {
        if (ChipRain.Instance != null)
            ChipRain.Instance.Clear();
    }

    private void Update()
    {
        if (!IsActive)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        spawnTimer = SpawnInterval;
        DropChip();
    }

    private void DropChip()
    {
        ChipRain rain = ChipRain.Instance;
        if (rain == null)
        {
            if (!warnedMissingRain)
                Debug.LogWarning("Club phase needs a ChipRain object under the Canvas (with its PokerChip children) to drop chips.", this);
            warnedMissingRain = true;
            return;
        }

        if (rain.FallingCount >= MaxChips)
            return;

        Color color = chipColors != null && chipColors.Length > 0
            ? chipColors[Random.Range(0, chipColors.Length)]
            : Color.red;

        rain.TryDrop(ChipRadius, fallSpeed, color, sideMargin, vanishHeightFromBottom, placementAttempts, maxSpin);
    }
}
