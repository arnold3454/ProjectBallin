using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hands the player a choice of upgrades every few points. Gameplay pauses while the
/// choice is on screen. Also holds the resulting stat bonuses for the rest of the game
/// to read (score, ball and flipper tuning).
/// Add it to the scene's GameManager and assign the upgrade popup prefab in the Inspector.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    private enum UpgradeId
    {
        BallWeight,
        BallSpeed,
        ScoreMultiplier,
        FlipperSpeed,
        FlatPoints,
        CritChance,
        FlatMultiplier,
    }

    // Per-pick amounts. The card text is written from these so it can't drift out of sync.
    private const float BallWeightPerPick = 0.25f;
    private const float BallSpeedPerPick = 0.15f;
    private const float ScoreMultiplierPerPick = 0.20f;
    private const float FlipperSpeedPerPick = 0.20f;
    private const int FlatPointsPerPick = 1;
    private const float CritChancePerPick = 0.05f;
    private const float FlatMultiplierPerPick = 0.10f;
    private const float CritMultiplier = 3f;

    [Header("Upgrade Offers")]
    [Tooltip("Points needed for the first upgrade.")]
    [SerializeField, Min(1)] private int pointsPerUpgrade = 15;
    [Tooltip("Each filled bar multiplies the XP needed for the next one by this. 1 = every level costs the same.")]
    [SerializeField, Min(1f)] private float xpGrowthPerLevel = 1.25f;
    [SerializeField, Min(1)] private int choicesPerOffer = 3;

    [Header("Upgrade UI")]
    [SerializeField] private UpgradePopup popupPrefab;
    [SerializeField] private CritPopup critPopupPrefab;

    [Header("Audio")]
    [Tooltip("Played when the XP bar fills and the player levels up.")]
    [SerializeField] private AudioClip levelUpSound;
    [SerializeField, Range(0f, 1f)] private float levelUpVolume = 1f;

    private readonly int[] stacks = new int[System.Enum.GetValues(typeof(UpgradeId)).Length];
    private UpgradePopup popup;
    private CritPopup critPopup;
    private List<UpgradeDefinition> allUpgrades;
    private List<UpgradeDefinition> currentChoices;
    private int nextThreshold;
    private int upgradeLevel;
    private int xpForCurrentBar;
    private int pendingOffers;
    private bool resolvingPick;
    private float scoreRemainder;

    private AudioSource audioSource;

    /// <summary>True from when the choice appears until gameplay has resumed.</summary>
    public bool IsChoosing { get; private set; }

    /// <summary>Points earned toward the next upgrade. This is what the XP bar shows.</summary>
    public int CurrentXp { get; private set; }

    /// <summary>Points needed to fill the current XP bar and earn an upgrade. Grows each level.</summary>
    public int XpPerUpgrade => xpForCurrentBar;

    /// <summary>How many bars have been filled so far.</summary>
    public int UpgradeLevel => upgradeLevel;

    /// <summary>Raised with (current XP, XP per upgrade) whenever XP changes, including when the bar fills and resets.</summary>
    public event System.Action<int, int> XpChanged;

    /// <summary>Upgrades the player has taken so far this run. Enemy scaling reads this.</summary>
    public int TotalPicks { get; private set; }

    /// <summary>Points the next upgrade unlocks at.</summary>
    public int NextThreshold => nextThreshold;

    /// <summary>How much the XP needed per level grows each level. The shop scales its prices with this too.</summary>
    public float XpGrowthPerLevel => xpGrowthPerLevel;

    /// <summary>Raised with the new total after every upgrade pick.</summary>
    public event System.Action<int> UpgradePicked;

    public float BallWeightMultiplier => 1f + BallWeightPerPick * Stacks(UpgradeId.BallWeight);
    public float BallSpeedMultiplier => 1f + BallSpeedPerPick * Stacks(UpgradeId.BallSpeed);
    public float FlipperSpeedMultiplier => 1f + FlipperSpeedPerPick * Stacks(UpgradeId.FlipperSpeed);
    public float ScoreMultiplier => 1f + ScoreMultiplierPerPick * Stacks(UpgradeId.ScoreMultiplier);
    public float FlatMultiplier => Mathf.Pow(1f + FlatMultiplierPerPick, Stacks(UpgradeId.FlatMultiplier));
    public int FlatPointBonus => FlatPointsPerPick * Stacks(UpgradeId.FlatPoints);
    public float CritChance => Mathf.Min(1f, CritChancePerPick * Stacks(UpgradeId.CritChance));

    /// <summary>
    /// Damage a ball hit deals to enemies and the boss, as a multiple of the base. Follows the
    /// score multipliers only: the flat bonus point and crits don't make hits harder.
    /// </summary>
    public float DamageMultiplier => ScoreMultiplier * FlatMultiplier;

    /// <summary>Static so hit code needn't null-check the manager. 1 without one in the scene.</summary>
    public static float CurrentDamageMultiplier => Instance != null ? Instance.DamageMultiplier : 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (popupPrefab == null || critPopupPrefab == null)
        {
            Debug.LogError("Assign the upgrade and crit popup prefabs on UpgradeManager.", this);
            enabled = false;
            return;
        }

        Instance = this;
        xpForCurrentBar = XpRequiredFor(0);
        nextThreshold = xpForCurrentBar;
        allUpgrades = BuildUpgradeList();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        popup = Instantiate(popupPrefab, transform);
        popup.gameObject.name = "UpgradePopup";
        popup.Hide();

        critPopup = Instantiate(critPopupPrefab, transform);
        critPopup.gameObject.name = "CritPopup";
    }

    private void Start()
    {
        if (Instance == this && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ScoreChanged += OnScoreChanged;
            ScoreManager.Instance.CritScored += critPopup.Spawn;
            OnScoreChanged(ScoreManager.Instance.CurrentScore);
        }
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ScoreChanged -= OnScoreChanged;
            if (critPopup != null)
                ScoreManager.Instance.CritScored -= critPopup.Spawn;
        }

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (IsChoosing)
        {
            if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            {
                // The game over panel takes over, and it owns the time scale.
                popup.Hide();
                IsChoosing = false;
                pendingOffers = 0;
                return;
            }

            HandleKeys();
            return;
        }

        if (pendingOffers <= 0)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
        {
            pendingOffers = 0;
            return;
        }

        // Wait out the pause menu rather than stacking two pauses.
        if (Time.timeScale <= 0f)
            return;

        IsChoosing = true;
        Time.timeScale = 0f;
        ShowOffer();
    }

    private void OnScoreChanged(int score)
    {
        if (ScoreManager.Instance != null)
            score = ScoreManager.Instance.TotalEarned;

        bool leveledUp = false;

        while (score >= nextThreshold)
        {
            pendingOffers++;
            upgradeLevel++;
            xpForCurrentBar = XpRequiredFor(upgradeLevel);
            nextThreshold += xpForCurrentBar;
            leveledUp = true;
        }

        if (leveledUp)
            PlayLevelUpSound();

        CurrentXp = Mathf.Max(0, score - (nextThreshold - xpForCurrentBar));
        XpChanged?.Invoke(CurrentXp, xpForCurrentBar);
    }

    private void PlayLevelUpSound()
    {
        if (levelUpSound == null || audioSource == null)
            return;

        audioSource.PlayOneShot(levelUpSound, levelUpVolume);
    }

    /// <summary>XP needed to fill the bar at the given level: pointsPerUpgrade * growth^level.</summary>
    private int XpRequiredFor(int level)
    {
        return Mathf.Max(1, Mathf.RoundToInt(pointsPerUpgrade * Mathf.Pow(xpGrowthPerLevel, level)));
    }

    /// <summary>
    /// Grows an amount the same way the XP bar does: amount * growth^level, at the player's
    /// current level. Shop prices use this so they keep pace with levelling.
    /// </summary>
    public int ScaleWithLevel(int amount)
    {
        return Mathf.RoundToInt(amount * Mathf.Pow(xpGrowthPerLevel, upgradeLevel));
    }

    /// <summary>Every upgrade that can still be picked, i.e. isn't maxed out.</summary>
    public List<UpgradeDefinition> GetAvailableUpgrades()
    {
        List<UpgradeDefinition> available = new List<UpgradeDefinition>();
        if (allUpgrades == null)
            return available;

        foreach (UpgradeDefinition upgrade in allUpgrades)
        {
            if (!upgrade.IsMaxed(StacksOf(upgrade)))
                available.Add(upgrade);
        }

        return available;
    }

    /// <summary>
    /// Takes the upgrade outside of a level-up offer, e.g. bought in the shop. Returns false
    /// if it is maxed out or not one of this manager's upgrades.
    /// </summary>
    public bool ApplyUpgrade(UpgradeDefinition upgrade)
    {
        if (upgrade == null || !idByUpgrade.ContainsKey(upgrade) || upgrade.IsMaxed(StacksOf(upgrade)))
            return false;

        upgrade.Apply(this);
        TotalPicks++;
        RefreshWorld();
        UpgradePicked?.Invoke(TotalPicks);
        return true;
    }

    /// <summary>
    /// Applies the player's upgrades to points about to be scored.
    /// Order: +flat points, then multipliers, then the crit roll. Fractions carry over
    /// to the next score so small percentage bonuses still add up.
    /// </summary>
    public int ModifyPoints(int amount, out bool crit)
    {
        crit = false;
        if (amount <= 0)
            return amount;

        float value = (amount + FlatPointBonus) * ScoreMultiplier * FlatMultiplier;

        if (CritChance > 0f && Random.value < CritChance)
        {
            value *= CritMultiplier;
            crit = true;
        }

        value += scoreRemainder;
        int points = Mathf.FloorToInt(value);
        scoreRemainder = value - points;
        return points;
    }

    private void ShowOffer()
    {
        currentChoices = RollChoices();
        if (currentChoices.Count == 0)
        {
            // Everything is maxed out. Nothing to offer, so just carry on.
            pendingOffers = 0;
            IsChoosing = false;
            Time.timeScale = 1f;
            return;
        }

        int[] owned = new int[currentChoices.Count];
        for (int i = 0; i < owned.Length; i++)
            owned[i] = StacksOf(currentChoices[i]);

        resolvingPick = false;
        popup.Show(currentChoices, owned, Pick);
    }

    private void HandleKeys()
    {
        if (Keyboard.current == null || currentChoices == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            Pick(0);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            Pick(1);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            Pick(2);
    }

    private void Pick(int index)
    {
        if (resolvingPick || currentChoices == null || index < 0 || index >= currentChoices.Count)
            return;

        resolvingPick = true;
        ApplyUpgrade(currentChoices[index]);
        pendingOffers--;
        popup.Hide();
        StartCoroutine(FinishPick());
    }

    private IEnumerator FinishPick()
    {
        // Defer a frame so the click or key that picked the upgrade can't also trigger gameplay input.
        yield return null;

        if (pendingOffers > 0 && (GameTimer.Instance == null || !GameTimer.Instance.IsGameOver))
        {
            ShowOffer();
            yield break;
        }

        IsChoosing = false;
        pendingOffers = 0;
        if (GameTimer.Instance == null || !GameTimer.Instance.IsGameOver)
            Time.timeScale = 1f;
    }

    /// <summary>Pushes new stats onto things that already exist in the scene.</summary>
    private void RefreshWorld()
    {
        if (BallSettingsManager.Instance != null)
            BallSettingsManager.Instance.RefreshWeight();

        foreach (FlipperControls flipper in FindObjectsByType<FlipperControls>(FindObjectsInactive.Include))
            flipper.RefreshUpgrades();
    }

    private List<UpgradeDefinition> RollChoices()
    {
        List<UpgradeDefinition> pool = new List<UpgradeDefinition>();
        foreach (UpgradeDefinition upgrade in allUpgrades)
        {
            if (!upgrade.IsMaxed(StacksOf(upgrade)))
                pool.Add(upgrade);
        }

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(choicesPerOffer, pool.Count);
        return pool.GetRange(0, count);
    }

    private int Stacks(UpgradeId id) => stacks[(int)id];

    /// <summary>How many times the upgrade has been taken so far.</summary>
    public int StacksOf(UpgradeDefinition upgrade) => idByUpgrade.TryGetValue(upgrade, out UpgradeId id) ? stacks[(int)id] : 0;

    private readonly Dictionary<UpgradeDefinition, UpgradeId> idByUpgrade = new Dictionary<UpgradeDefinition, UpgradeId>();

    private UpgradeDefinition Define(UpgradeId id, string name, string description, System.Func<int, string> total, int maxStacks = 0)
    {
        UpgradeDefinition definition = new UpgradeDefinition(name, description, total, manager => manager.stacks[(int)id]++, maxStacks);
        idByUpgrade[definition] = id;
        return definition;
    }

    private List<UpgradeDefinition> BuildUpgradeList()
    {
        return new List<UpgradeDefinition>
        {
            Define(UpgradeId.BallWeight, "Heavy Ball",
                $"Ball weight +{Percent(BallWeightPerPick)} (mass)",
                n => "+" + Percent(BallWeightPerPick * n)),

            Define(UpgradeId.BallSpeed, "Fast Ball",
                $"Ball speed +{Percent(BallSpeedPerPick)} (plunger launch speed and bouncer kick)",
                n => "+" + Percent(BallSpeedPerPick * n)),

            Define(UpgradeId.ScoreMultiplier, "Score Multiplier",
                $"All points +{Percent(ScoreMultiplierPerPick)} (picks add together: x1.2, x1.4, x1.6...)",
                n => "+" + Percent(ScoreMultiplierPerPick * n)),

            Define(UpgradeId.FlipperSpeed, "Fast Flippers",
                $"Flipper swing speed +{Percent(FlipperSpeedPerPick)}",
                n => "+" + Percent(FlipperSpeedPerPick * n)),

            Define(UpgradeId.FlatPoints, "Bonus Points",
                $"+{FlatPointsPerPick} point on every score (added before multipliers)",
                n => "+" + (FlatPointsPerPick * n)),

            Define(UpgradeId.CritChance, "Critical Hits",
                $"+{Percent(CritChancePerPick)} crit chance. A crit scores {CritMultiplier:0}x points (max 20 picks)",
                n => Percent(CritChancePerPick * n) + " chance",
                maxStacks: 20),

            Define(UpgradeId.FlatMultiplier, "Flat Multiplier",
                $"Final points x{1f + FlatMultiplierPerPick:0.00} (+{Percent(FlatMultiplierPerPick)}, each pick multiplies the last)",
                n => "+" + Percent(Mathf.Pow(1f + FlatMultiplierPerPick, n) - 1f)),
        };
    }

    private static string Percent(float fraction) => (fraction * 100f).ToString("0.#") + "%";
}