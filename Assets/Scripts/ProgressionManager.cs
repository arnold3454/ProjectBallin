using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks the run across rounds and scales the game to keep up with the player. Beating the
/// boss brings up the win screen; Continue rebuilds the board, resets the boss summoning and
/// the clock, and starts the next round. Score, upgrades and items carry over. Enemy health
/// grows with every upgrade taken and every boss beaten, and boss health with every boss
/// beaten. Lives on the GameManager with the other managers.
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Enemy Scaling")]
    [Tooltip("Enemy health grows by this fraction for every upgrade the player has taken, compounding. 0.03 = +3% per upgrade, so ten upgrades make enemies about 34% tougher.")]
    [SerializeField, Range(0f, 0.5f)] private float enemyHealthPerUpgrade = 0.03f;
    [Tooltip("Extra enemy health per boss beaten, as a fraction of base health. 0.3 = +30% per round.")]
    [SerializeField, Min(0f)] private float enemyHealthPerRound = 0.3f;
    [Tooltip("Extra boss health per boss beaten, as a fraction of base health. 0.25 = +25% per round.")]
    [SerializeField, Min(0f)] private float bossHealthPerRound = 0.25f;

    [Header("Win Screen")]
    [Tooltip("Seconds after the boss dies before the win screen appears, so the kill can be seen.")]
    [SerializeField, Min(0f)] private float winScreenDelay = 1.5f;
    [Tooltip("Panel shown when the boss is beaten: the WinPanel under the Canvas.")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI winTitleText;
    [SerializeField] private TextMeshProUGUI winSummaryText;
    [Tooltip("The panel's Continue button. Its On Click calls StartNextRound, wired in the Inspector.")]
    [SerializeField] private Button continueButton;
    [SerializeField] private string winTitle = "DEALER DEFEATED!";

    [Header("Next Round")]
    [Tooltip("Rebuilds the table between rounds. Found in the scene if left empty.")]
    [SerializeField] private BoardGenerator boardGenerator;

    /// <summary>Raised with the number of bosses beaten once the next round has begun.</summary>
    public static event Action<int> RoundStarted;

    /// <summary>Bosses beaten this run. The first round is 0.</summary>
    public int RoundsWon { get; private set; }

    /// <summary>True from the boss's death until the player presses Continue.</summary>
    public bool IsShowingWin { get; private set; }

    /// <summary>Multiplier for regular enemy health. 1 when there is no manager in the scene.</summary>
    public static float EnemyHealthMultiplier => Instance != null ? Instance.GetEnemyHealthMultiplier() : 1f;

    /// <summary>Multiplier for boss health. 1 when there is no manager in the scene.</summary>
    public static float BossHealthMultiplier => Instance != null ? Instance.GetBossHealthMultiplier() : 1f;

    /// <summary>Scales a base health value by <see cref="EnemyHealthMultiplier"/>, never below 1.</summary>
    public static int ScaleEnemyHealth(int baseHealth) => Mathf.Max(1, Mathf.RoundToInt(baseHealth * EnemyHealthMultiplier));

    /// <summary>Scales a base health value by <see cref="BossHealthMultiplier"/>, never below 1.</summary>
    public static int ScaleBossHealth(int baseHealth) => Mathf.Max(1, Mathf.RoundToInt(baseHealth * BossHealthMultiplier));

    private void Awake()
    {
        Instance = this;

        // The Continue button's On Click is wired to StartNextRound in the Inspector.
        if (winPanel != null)
            winPanel.SetActive(false);
    }

    private void OnEnable()
    {
        BossManager.BossFightEnded += OnBossFightEnded;
    }

    private void OnDisable()
    {
        BossManager.BossFightEnded -= OnBossFightEnded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public float GetEnemyHealthMultiplier()
    {
        return GetEnemyHealthMultiplier(RoundsWon);
    }

    public float GetBossHealthMultiplier()
    {
        return 1f + bossHealthPerRound * RoundsWon;
    }

    private float GetEnemyHealthMultiplier(int roundsWon)
    {
        int upgrades = UpgradeManager.Instance != null ? UpgradeManager.Instance.TotalPicks : 0;
        return Mathf.Pow(1f + enemyHealthPerUpgrade, upgrades) * (1f + enemyHealthPerRound * roundsWon);
    }

    private void OnBossFightEnded(bool defeated)
    {
        if (!defeated || IsShowingWin)
            return;

        // The last boss ends the game with its own screen; there is no next round to offer.
        if (BossManager.Instance != null && BossManager.Instance.IsGameWon)
            return;

        StartCoroutine(ShowWinScreen());
    }

    private IEnumerator ShowWinScreen()
    {
        IsShowingWin = true;

        // Hold the clock so the round can't run out while the kill plays out.
        if (GameTimer.Instance != null)
            GameTimer.Instance.PauseTimer();

        if (winScreenDelay > 0f)
            yield return new WaitForSeconds(winScreenDelay);

        // Wait out a pause menu or upgrade choice rather than drawing over it.
        while (Time.timeScale <= 0f && (GameTimer.Instance == null || !GameTimer.Instance.IsGameOver))
            yield return null;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
        {
            // The game over panel owns the screen now.
            IsShowingWin = false;
            yield break;
        }

        if (winPanel == null)
        {
            Debug.LogWarning("ProgressionManager has no win panel assigned (the WinPanel under the Canvas), so the next round starts straight away.", this);
            StartNextRound();
            yield break;
        }

        if (winTitleText != null)
            winTitleText.text = winTitle;
        if (winSummaryText != null)
            winSummaryText.text = BuildSummary();

        winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private string BuildSummary()
    {
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        float nextEnemyHealth = GetEnemyHealthMultiplier(RoundsWon + 1);
        return $"Round {RoundsWon + 1} cleared!   Score: {score}\n" +
               "Your upgrades and items carry over.\n" +
               $"Round {RoundsWon + 2}: a fresh table, enemies with {Percent(nextEnemyHealth - 1f)} more health.";
    }

    /// <summary>
    /// Hides the win screen, rebuilds the table and starts the next round. Score, upgrades and
    /// items are kept; balls, pickups and enemies on the table are cleared.
    /// </summary>
    [ContextMenu("Start Next Round")]
    public void StartNextRound()
    {
        IsShowingWin = false;
        RoundsWon++;

        if (winPanel != null)
            winPanel.SetActive(false);

        // Reset the summoning before the board changes so re-registered triggers start clean.
        if (BossManager.Instance != null)
            BossManager.Instance.ResetSummoning();
        if (RouletteManager.Instance != null)
            RouletteManager.Instance.ResetRound();

        ClearTable();

        if (boardGenerator == null)
            boardGenerator = FindAnyObjectByType<BoardGenerator>();
        if (boardGenerator != null)
            boardGenerator.GenerateRandom();
        else
            Debug.LogWarning("ProgressionManager found no BoardGenerator, so the board was not regenerated.", this);

        if (GameTimer.Instance != null)
        {
            GameTimer.Instance.ResetTimer();
            GameTimer.Instance.StartTimer();
        }

        Debug.Log($"Round {RoundsWon + 1} started. Enemy health x{EnemyHealthMultiplier:0.00}, boss health x{BossHealthMultiplier:0.00}.", this);
        RoundStarted?.Invoke(RoundsWon);
        StartCoroutine(ResumePlay());
    }

    private IEnumerator ResumePlay()
    {
        // Stay paused a frame so the click that pressed Continue can't reach gameplay.
        Time.timeScale = 0f;
        yield return null;

        if (GameTimer.Instance == null || !GameTimer.Instance.IsGameOver)
            Time.timeScale = 1f;
    }

    private static void ClearTable()
    {
        foreach (GameObject ball in GameObject.FindGameObjectsWithTag("Ball"))
            Destroy(ball);

        foreach (ItemPickup pickup in FindObjectsByType<ItemPickup>())
            Destroy(pickup.gameObject);

        // Enemies respawn from their spawners with the new round's health.
        foreach (Enemy enemy in FindObjectsByType<Enemy>())
            Destroy(enemy.gameObject);
    }

    private static string Percent(float fraction) => (fraction * 100f).ToString("0") + "%";
}
