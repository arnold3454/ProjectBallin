using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Hands the player a choice of upgrades every time the XP bar fills. XP per level follows the
/// economy document (peg score + 1.05 x level^1.15). Filling the bar doesn't interrupt play: the
/// UpgradeReadyPanel under the Canvas stays up until the player presses the choose key or clicks
/// it, and only then does gameplay pause for the choice. Also holds the resulting stat bonuses for
/// the rest of the game to read (score, ball, flipper and nudge tuning).
/// Lives on the scene's GameManager. Assign the popup prefabs and the UpgradeReadyPanel in the Inspector.
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
        NudgeStamina,
        NudgeStrength,
        SplitAmount,
    }

    // Per-pick amounts. The card text is written from these so it can't drift out of sync.
    private const float BallWeightPerPick = 0.25f;
    private const float BallSpeedPerPick = 0.15f;
    private const float ScoreMultiplierPerPick = 0.20f;
    private const float FlipperSpeedPerPick = 0.20f;
    private const int FlatPointsPerPick = 1;
    private const float CritChancePerPick = 0.05f;
    private const float FlatMultiplierPerPick = 0.10f;
    private const int NudgeStaminaPerPick = 1;
    private const float NudgeStrengthPerPick = 0.20f;
    private const float NudgeCostPerPick = 0.10f;
    private const int SplitsPerPick = 1;

    // Crits (Items & Upgrades document): a crit scores 2x. Past 100% chance the player "overcrits":
    // every 5% over 100% adds 0.1x to the crit multiplier.
    private const float BaseCritMultiplier = 2f;
    private const float OverCritStep = 0.05f;
    private const float OverCritBonusPerStep = 0.1f;

    /// <summary>Splits the Split ball starts with per ball. Split Amount adds one per pick.</summary>
    public const int BaseSplitsPerBall = 1;

    /// <summary>The most splits per ball the Split Amount upgrade can reach.</summary>
    public const int MaxSplitsPerBall = 10;

    [Header("XP Curve (economy document)")]
    [Tooltip("Points a standard peg scores (see Bouncer). The economy document's XP formula starts from this: XP for the next level = peg score + coefficient x level^exponent.")]
    [SerializeField, Min(0)] private int standardPegScore = 1;
    [Tooltip("The 1.05 in the economy document's formula.")]
    [SerializeField, Min(0f)] private float levelCoefficient = 1.05f;
    [Tooltip("The 1.15 in the economy document's formula. Level is the player's current level, 1 at the start of a run.")]
    [SerializeField, Min(0f)] private float levelExponent = 1.15f;
    [Tooltip("Shop prices multiply by this for every level the player has gained.")]
    [SerializeField, Min(1f)] private float priceGrowthPerLevel = 1.25f;
    [SerializeField, Min(1)] private int choicesPerOffer = 3;

    [Header("Upgrade UI")]
    [SerializeField] private UpgradePopup popupPrefab;
    [SerializeField] private CritPopup critPopupPrefab;

    [Header("Upgrade Ready Notification")]
    [Tooltip("The UpgradeReadyPanel under the Canvas. Shown from when the bar fills until the player opens the choice.")]
    [SerializeField] private GameObject readyPanel;
    [SerializeField] private TextMeshProUGUI readyText;
    [Tooltip("Pressing this key opens the upgrade choice. The panel's button calls OpenPendingOffer as well.")]
    [SerializeField] private Key chooseKey = Key.U;

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

    /// <summary>True from when the choice appears until gameplay has resumed.</summary>
    public bool IsChoosing { get; private set; }

    /// <summary>Upgrades earned but not yet chosen. The ready notification is up while this is above 0.</summary>
    public int PendingOffers => pendingOffers;

    /// <summary>Points earned toward the next upgrade. This is what the XP bar shows.</summary>
    public int CurrentXp { get; private set; }

    /// <summary>Points needed to fill the current XP bar and earn an upgrade. Grows each level.</summary>
    public int XpPerUpgrade => xpForCurrentBar;

    /// <summary>How many bars have been filled so far. The player's level is one more than this.</summary>
    public int UpgradeLevel => upgradeLevel;

    /// <summary>Raised with (current XP, XP per upgrade) whenever XP changes, including when the bar fills and resets.</summary>
    public event System.Action<int, int> XpChanged;

    /// <summary>Upgrades the player has taken so far this run. Enemy scaling reads this.</summary>
    public int TotalPicks { get; private set; }

    /// <summary>Points the next upgrade unlocks at.</summary>
    public int NextThreshold => nextThreshold;

    /// <summary>Raised with the new total after every upgrade pick.</summary>
    public event System.Action<int> UpgradePicked;

    public float BallWeightMultiplier => 1f + BallWeightPerPick * Stacks(UpgradeId.BallWeight);
    public float BallSpeedMultiplier => 1f + BallSpeedPerPick * Stacks(UpgradeId.BallSpeed);
    public float FlipperSpeedMultiplier => 1f + FlipperSpeedPerPick * Stacks(UpgradeId.FlipperSpeed);
    public float ScoreMultiplier => 1f + ScoreMultiplierPerPick * Stacks(UpgradeId.ScoreMultiplier);
    public float FlatMultiplier => Mathf.Pow(1f + FlatMultiplierPerPick, Stacks(UpgradeId.FlatMultiplier));
    public int FlatPointBonus => FlatPointsPerPick * Stacks(UpgradeId.FlatPoints);

    /// <summary>Crit chance from picks alone. Past 100% the extra feeds the overcrit multiplier.</summary>
    public float RawCritChance => CritChancePerPick * Stacks(UpgradeId.CritChance);

    /// <summary>Chance a score crits, capped at 100%.</summary>
    public float CritChance => Mathf.Min(1f, RawCritChance);

    /// <summary>Points multiplier on a crit: 2x, plus 0.1x for every 5% of crit chance past 100%.</summary>
    public float CritMultiplier => BaseCritMultiplier + OverCritBonusPerStep * OverCritSteps(RawCritChance);

    /// <summary>Extra nudges before the table tilts, on top of the NudgeController's base.</summary>
    public int NudgeStaminaBonus => NudgeStaminaPerPick * Stacks(UpgradeId.NudgeStamina);

    /// <summary>Multiplier on the force of each nudge.</summary>
    public float NudgeStrengthMultiplier => 1f + NudgeStrengthPerPick * Stacks(UpgradeId.NudgeStrength);

    /// <summary>Multiplier on the tilt each nudge builds. Stronger nudges cost more stamina.</summary>
    public float NudgeCostMultiplier => 1f + NudgeCostPerPick * Stacks(UpgradeId.NudgeStrength);

    /// <summary>How many times the Split ball can split each ball, upgrades included.</summary>
    public int SplitsPerBall => Mathf.Min(MaxSplitsPerBall, BaseSplitsPerBall + SplitsPerPick * Stacks(UpgradeId.SplitAmount));

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

        if (readyPanel == null)
            Debug.LogWarning("UpgradeManager has no UpgradeReadyPanel assigned (under the Canvas), so earned upgrades can only be opened with the choose key.", this);
        else
            readyPanel.SetActive(false);

        Instance = this;
        xpForCurrentBar = XpRequiredFor(0);
        nextThreshold = xpForCurrentBar;
        allUpgrades = BuildUpgradeList();

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
        bool gameOver = GameTimer.Instance != null && GameTimer.Instance.IsGameOver;

        if (IsChoosing)
        {
            if (gameOver)
            {
                // The game over panel takes over, and it owns the time scale.
                popup.Hide();
                IsChoosing = false;
                pendingOffers = 0;
                ShowReadyPanel(false);
                return;
            }

            HandleKeys();
            return;
        }

        if (pendingOffers <= 0)
            return;

        if (gameOver)
        {
            pendingOffers = 0;
            ShowReadyPanel(false);
            return;
        }

        if (Keyboard.current != null && chooseKey != Key.None && Keyboard.current[chooseKey].wasPressedThisFrame)
            OpenPendingOffer();
    }

    /// <summary>
    /// Opens the upgrade choice for the offers earned so far. The UpgradeReadyPanel's button calls
    /// this (On Click, wired in the Inspector) and so does the choose key. Does nothing while nothing
    /// is waiting or the game is already paused for something else (pause menu, shop, ball select).
    /// </summary>
    public void OpenPendingOffer()
    {
        if (IsChoosing || pendingOffers <= 0)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        // Wait out the pause menu rather than stacking two pauses.
        if (Time.timeScale <= 0f)
            return;

        IsChoosing = true;
        Time.timeScale = 0f;
        ShowReadyPanel(false);
        ShowOffer();
    }

    private void OnScoreChanged(int score)
    {
        // Level off everything ever earned, so spending score in the shop doesn't drain the XP bar.
        if (ScoreManager.Instance != null)
            score = ScoreManager.Instance.TotalEarned;

        while (score >= nextThreshold)
        {
            pendingOffers++;
            upgradeLevel++;
            xpForCurrentBar = XpRequiredFor(upgradeLevel);
            nextThreshold += xpForCurrentBar;
        }

        // Filling the bar is what earned the offer above; whatever spilled over starts the next bar.
        CurrentXp = Mathf.Max(0, score - (nextThreshold - xpForCurrentBar));
        XpChanged?.Invoke(CurrentXp, xpForCurrentBar);

        if (pendingOffers > 0 && !IsChoosing)
            ShowReadyPanel(true);
    }

    /// <summary>
    /// XP needed to fill the bar after the given number of bars have been filled. From the economy
    /// document: peg score + 1.05 x level^1.15, where level is the player's level (1 at the start).
    /// </summary>
    private int XpRequiredFor(int barsFilled)
    {
        int playerLevel = barsFilled + 1;
        float xp = standardPegScore + levelCoefficient * Mathf.Pow(playerLevel, levelExponent);
        return Mathf.Max(1, Mathf.RoundToInt(xp));
    }

    /// <summary>
    /// Grows an amount with the player's level: amount * growth^level. Shop prices use this so
    /// they keep pace with levelling.
    /// </summary>
    public int ScaleWithLevel(int amount)
    {
        return Mathf.RoundToInt(amount * Mathf.Pow(priceGrowthPerLevel, upgradeLevel));
    }

    /// <summary>Every upgrade that can still be picked: not maxed out, and for the ball in play.</summary>
    public List<UpgradeDefinition> GetAvailableUpgrades()
    {
        List<UpgradeDefinition> available = new List<UpgradeDefinition>();
        if (allUpgrades == null)
            return available;

        foreach (UpgradeDefinition upgrade in allUpgrades)
        {
            if (IsOffered(upgrade))
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
            ShowReadyPanel(false);
            Time.timeScale = 1f;
            return;
        }

        int[] owned = new int[currentChoices.Count];
        for (int i = 0; i < owned.Length; i++)
            owned[i] = StacksOf(currentChoices[i]);

        resolvingPick = false;
        popup.Show(currentChoices, owned, Pick);
    }

    private void ShowReadyPanel(bool visible)
    {
        if (readyPanel == null)
            return;

        if (visible && readyText != null)
        {
            string key = chooseKey != Key.None ? $"[{chooseKey}] or " : "";
            readyText.text = pendingOffers > 1
                ? $"{pendingOffers} UPGRADES READY!\nPress {key}click here to choose"
                : $"UPGRADE READY!\nPress {key}click here to choose";
        }

        readyPanel.SetActive(visible);
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
            // More bars were filled in the meantime: go straight on to the next choice.
            ShowOffer();
            yield break;
        }

        IsChoosing = false;
        pendingOffers = 0;
        ShowReadyPanel(false);
        if (GameTimer.Instance == null || !GameTimer.Instance.IsGameOver)
            Time.timeScale = 1f;
    }

    /// <summary>Pushes new stats onto things that already exist in the scene.</summary>
    private void RefreshWorld()
    {
        if (BallSettingsManager.Instance != null)
            BallSettingsManager.Instance.RefreshWeight();

        if (NudgeController.Instance != null)
            NudgeController.Instance.RefreshUpgrades();

        foreach (FlipperControls flipper in FindObjectsByType<FlipperControls>(FindObjectsInactive.Include))
            flipper.RefreshUpgrades();
    }

    private List<UpgradeDefinition> RollChoices()
    {
        List<UpgradeDefinition> pool = GetAvailableUpgrades();

        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int count = Mathf.Min(choicesPerOffer, pool.Count);
        return pool.GetRange(0, count);
    }

    /// <summary>Not maxed out, and either a general upgrade or one for the ball type in play.</summary>
    private bool IsOffered(UpgradeDefinition upgrade)
    {
        if (upgrade.IsMaxed(StacksOf(upgrade)))
            return false;

        if (!upgrade.RequiredBallType.HasValue)
            return true;

        BallTypeInfo current = BallTypeManager.Instance != null ? BallTypeManager.Instance.Current : null;
        return current != null && current.Type == upgrade.RequiredBallType.Value;
    }

    private static int OverCritSteps(float rawChance)
    {
        return rawChance <= 1f ? 0 : Mathf.FloorToInt((rawChance - 1f) / OverCritStep + 0.001f);
    }

    private int Stacks(UpgradeId id) => stacks[(int)id];

    /// <summary>How many times the upgrade has been taken so far.</summary>
    public int StacksOf(UpgradeDefinition upgrade) => idByUpgrade.TryGetValue(upgrade, out UpgradeId id) ? stacks[(int)id] : 0;

    private readonly Dictionary<UpgradeDefinition, UpgradeId> idByUpgrade = new Dictionary<UpgradeDefinition, UpgradeId>();

    private UpgradeDefinition Define(UpgradeId id, string name, string description, System.Func<int, string> total, int maxStacks = 0, BallType? requiredBall = null)
    {
        UpgradeDefinition definition = new UpgradeDefinition(name, description, total, manager => manager.stacks[(int)id]++, maxStacks, requiredBall);
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
                $"+{Percent(CritChancePerPick)} crit chance. A crit scores {BaseCritMultiplier:0}x points. " +
                $"Past 100% you overcrit: every {Percent(OverCritStep)} over adds +{OverCritBonusPerStep:0.0}x to crits",
                CritTotal),

            Define(UpgradeId.FlatMultiplier, "Flat Multiplier",
                $"Final points x{1f + FlatMultiplierPerPick:0.00} (+{Percent(FlatMultiplierPerPick)}, each pick multiplies the last)",
                n => "+" + Percent(Mathf.Pow(1f + FlatMultiplierPerPick, n) - 1f)),

            Define(UpgradeId.NudgeStamina, "Nudge Stamina",
                $"+{NudgeStaminaPerPick} nudge before the table tilts and locks the flippers",
                n => "+" + (NudgeStaminaPerPick * n) + (NudgeStaminaPerPick * n == 1 ? " nudge" : " nudges")),

            Define(UpgradeId.NudgeStrength, "Nudge Strength",
                $"Nudges shove the ball {Percent(NudgeStrengthPerPick)} harder, but each one builds {Percent(NudgeCostPerPick)} more tilt",
                n => "+" + Percent(NudgeStrengthPerPick * n) + " force, +" + Percent(NudgeCostPerPick * n) + " tilt"),

            Define(UpgradeId.SplitAmount, "Split Amount",
                $"Split ball: +{SplitsPerPick} split per ball (max {MaxSplitsPerBall})",
                n => Mathf.Min(MaxSplitsPerBall, BaseSplitsPerBall + SplitsPerPick * n) + " per ball",
                maxStacks: (MaxSplitsPerBall - BaseSplitsPerBall) / SplitsPerPick,
                requiredBall: BallType.Split),
        };
    }

    private static string CritTotal(int stacks)
    {
        float raw = CritChancePerPick * stacks;
        float multiplier = BaseCritMultiplier + OverCritBonusPerStep * OverCritSteps(raw);
        return raw > 1f
            ? $"100% chance, {multiplier:0.0}x crits"
            : $"{Percent(raw)} chance, {multiplier:0.0}x crits";
    }

    private static string Percent(float fraction) => (fraction * 100f).ToString("0.#") + "%";
}
