using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs the boss fight. Starts when the roulette wheel is won: spawns the dealer over the
/// wheel, runs the boss timer (shorter than the main game clock) and switches the boss to a
/// random phase on a fixed interval. The fight ends when the boss dies or the timer runs out.
/// Lives on the GameManager with the other managers.
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Boss")]
    [SerializeField] private Boss bossPrefab;
    [Tooltip("Where the boss spawns if there is no roulette wheel in the scene.")]
    [SerializeField] private Vector3 fallbackSpawnPoint = new Vector3(-0.8f, 1.75f, 20f);

    [Header("Timer")]
    [Tooltip("Seconds the player gets to beat the boss. Capped to the main timer's remaining time.")]
    [SerializeField, Min(1f)] private float bossDuration = 45f;
    [Tooltip("Seconds between random phase switches.")]
    [SerializeField, Min(1f)] private float phaseInterval = 8f;
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    [Header("Board")]
    [Tooltip("World X/Z of the bottom-left of the area phases may use.")]
    [SerializeField] private Vector2 boardMin = new Vector2(-18.4f, -8f);
    [Tooltip("World X/Z of the top-right of the area phases may use.")]
    [SerializeField] private Vector2 boardMax = new Vector2(16.8f, 38f);
    [Tooltip("World Y of the table surface.")]
    [SerializeField] private float surfaceHeight = 1.75f;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI bossText;
    [SerializeField] private string bossName = "DEALER";
    [SerializeField] private string defeatedMessage = "DEALER DEFEATED!";
    [SerializeField] private string escapedMessage = "The dealer got away...";
    [SerializeField, Min(0f)] private float messageDuration = 3f;

    [Header("Debug")]
    [Tooltip("Start the fight as soon as the scene loads, without winning the roulette.")]
    [SerializeField] private bool startFightOnLoad;

    [Header("Events")]
    [SerializeField] private UnityEvent onBossDefeated;
    [SerializeField] private UnityEvent onBossEscaped;

    public static event Action BossFightStarted;
    /// <summary>Raised with true when the boss was defeated, false when it escaped.</summary>
    public static event Action<bool> BossFightEnded;

    public bool IsFightActive { get; private set; }
    public Boss ActiveBoss { get; private set; }
    public float TimeRemaining { get; private set; }
    public float Duration { get; private set; }

    /// <summary>0 at the start of the fight, 1 when the boss timer runs out.</summary>
    public float Urgency => IsFightActive && Duration > 0f ? 1f - Mathf.Clamp01(TimeRemaining / Duration) : 0f;

    public Vector2 BoardMin => boardMin;
    public Vector2 BoardMax => boardMax;
    public float SurfaceHeight => surfaceHeight;

    private float phaseTimer;
    private float messageTimer;

    private void Awake()
    {
        Instance = this;
        SetHud(string.Empty);
    }

    private void OnEnable()
    {
        RouletteManager.BossFightTriggered += StartFight;
    }

    private void OnDisable()
    {
        RouletteManager.BossFightTriggered -= StartFight;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        if (startFightOnLoad)
            StartFight();
    }

    private void Update()
    {
        if (!IsFightActive)
        {
            if (messageTimer > 0f)
            {
                messageTimer -= Time.deltaTime;
                if (messageTimer <= 0f)
                    SetHud(string.Empty);
            }
            return;
        }

        if (ActiveBoss == null)
        {
            EndFight(false);
            return;
        }

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            EndFight(false);
            return;
        }

        phaseTimer -= Time.deltaTime;
        if (phaseTimer <= 0f)
        {
            phaseTimer = phaseInterval;
            ActiveBoss.SwitchToRandomPhase(this);
        }

        UpdateHud();
    }

    [ContextMenu("Start Boss Fight")]
    public void StartFight()
    {
        if (IsFightActive)
            return;

        if (bossPrefab == null)
        {
            Debug.LogError("BossManager needs a boss prefab assigned.", this);
            return;
        }

        Vector3 spawnPoint = fallbackSpawnPoint;
        RouletteWheel wheel = RouletteManager.Instance != null ? RouletteManager.Instance.Wheel : null;
        if (wheel != null)
            spawnPoint = wheel.transform.position;

        Duration = bossDuration;
        if (GameTimer.Instance != null && GameTimer.Instance.IsRunning)
            Duration = Mathf.Min(Duration, GameTimer.Instance.TimeRemaining);
        TimeRemaining = Duration;

        ActiveBoss = Instantiate(bossPrefab, spawnPoint, Quaternion.identity);
        ActiveBoss.Died += OnBossDied;
        ActiveBoss.HealthChanged += OnBossHealthChanged;

        IsFightActive = true;
        messageTimer = 0f;
        phaseTimer = phaseInterval;
        ActiveBoss.SwitchToRandomPhase(this);
        UpdateHud();

        BossFightStarted?.Invoke();
    }

    /// <summary>Ends the fight early as an escape. Nothing is scored.</summary>
    [ContextMenu("Stop Boss Fight")]
    public void StopFight()
    {
        EndFight(false);
    }

    /// <summary>Starts the fight if none is running, otherwise stops it. Used by the test scene's button.</summary>
    public void ToggleFight()
    {
        if (IsFightActive)
            StopFight();
        else
            StartFight();
    }

    private void OnBossDied(Boss boss)
    {
        EndFight(true);
    }

    private void OnBossHealthChanged(int health, int max)
    {
        UpdateHud();
    }

    private void EndFight(bool defeated)
    {
        if (!IsFightActive)
            return;

        IsFightActive = false;

        if (ActiveBoss != null)
        {
            ActiveBoss.Died -= OnBossDied;
            ActiveBoss.HealthChanged -= OnBossHealthChanged;
            ActiveBoss.EndPhases();
            Destroy(ActiveBoss.gameObject);
            ActiveBoss = null;
        }

        SetHud(defeated ? defeatedMessage : escapedMessage, normalColor);
        messageTimer = messageDuration;

        if (defeated)
            onBossDefeated?.Invoke();
        else
            onBossEscaped?.Invoke();

        BossFightEnded?.Invoke(defeated);
    }

    private void UpdateHud()
    {
        if (ActiveBoss == null)
            return;

        int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
        string phase = ActiveBoss.CurrentPhase != null ? ActiveBoss.CurrentPhase.PhaseName : "-";
        SetHud($"{bossName}  HP {ActiveBoss.Health}/{ActiveBoss.MaxHealth}   {phase}   {minutes:00}:{seconds:00}",
            TimeRemaining <= warningThreshold ? warningColor : normalColor);
    }

    private void SetHud(string message)
    {
        SetHud(message, normalColor);
    }

    private void SetHud(string message, Color color)
    {
        if (bossText == null)
            return;

        bossText.text = message;
        bossText.color = color;
    }
}
