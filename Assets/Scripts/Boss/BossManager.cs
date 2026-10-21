using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs the boss fight. It starts once both summoning conditions are met, in either order:
/// every corner boss trigger on the table is lit, and the roulette wheel has been won. Then
/// the dealer spawns over the wheel, the boss timer runs (shorter than the main game clock)
/// and the boss switches to a random phase on a fixed interval. The fight ends when the boss
/// dies or the timer runs out. Lives on the GameManager with the other managers.
/// </summary>
public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Summoning")]
    [Tooltip("Require every CornerBossTrigger in the scene to be lit before the fight can start.")]
    [SerializeField] private bool requireCornerTriggers = true;
    [Tooltip("Require the roulette wheel to have been won before the fight can start.")]
    [SerializeField] private bool requireRouletteWin = true;

    [Header("Boss")]
    [SerializeField] private Boss bossPrefab;
    [Tooltip("Where the boss spawns if there is no roulette wheel in the scene.")]
    [SerializeField] private Vector3 fallbackSpawnPoint = new Vector3(-0.8f, 1.75f, 20f);
    [Tooltip("Offset from the roulette wheel (or fallback point) to the boss. +Z is up the table, so the boss stands above the wheel on screen rather than on it.")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 7f);

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
    [SerializeField] private string needCornersMessage = "Light both corner buttons to summon the dealer";
    [SerializeField] private string needRouletteMessage = "Corners lit!  Win the roulette to summon the dealer";

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

    /// <summary>True once the roulette has been won this round.</summary>
    public bool RouletteWon { get; private set; }
    /// <summary>True while every registered corner trigger is lit (or none are required).</summary>
    public bool CornersArmed => !requireCornerTriggers || cornerTriggers.TrueForAll(t => t == null || t.IsLit);

    private readonly List<CornerBossTrigger> cornerTriggers = new List<CornerBossTrigger>();
    private float phaseTimer;
    private float messageTimer;

    private void Awake()
    {
        Instance = this;
        SetHud(string.Empty);
    }

    private void OnEnable()
    {
        RouletteManager.BossFightTriggered += OnRouletteWon;
    }

    private void OnDisable()
    {
        RouletteManager.BossFightTriggered -= OnRouletteWon;
    }

    public void RegisterCornerTrigger(CornerBossTrigger trigger)
    {
        if (trigger == null || cornerTriggers.Contains(trigger))
            return;

        cornerTriggers.Add(trigger);
        trigger.LitChanged += OnCornerTriggerChanged;
        TryStartFight();
    }

    public void UnregisterCornerTrigger(CornerBossTrigger trigger)
    {
        if (!cornerTriggers.Remove(trigger))
            return;

        trigger.LitChanged -= OnCornerTriggerChanged;
    }

    private void OnRouletteWon()
    {
        RouletteWon = true;
        TryStartFight();
    }

    private void OnCornerTriggerChanged(CornerBossTrigger trigger)
    {
        TryStartFight();
    }

    /// <summary>
    /// Clears the summoning progress for a new round: any running fight is stopped, the roulette
    /// win is forgotten and every corner trigger goes dark, so the dealer has to be summoned again.
    /// </summary>
    public void ResetSummoning()
    {
        if (IsFightActive)
            EndFight(false);

        RouletteWon = false;
        foreach (CornerBossTrigger trigger in cornerTriggers)
        {
            if (trigger != null)
                trigger.SetLit(false);
        }

        SetHud(string.Empty);
        messageTimer = 0f;
    }

    /// <summary>Starts the fight if every summoning condition is met; otherwise shows what is still missing.</summary>
    private void TryStartFight()
    {
        if (IsFightActive)
            return;

        bool rouletteReady = !requireRouletteWin || RouletteWon;
        bool cornersReady = CornersArmed;

        if (rouletteReady && cornersReady)
        {
            StartFight();
            return;
        }

        if (rouletteReady && !cornersReady)
            SetHud(needCornersMessage);
        else if (cornersReady && !rouletteReady && requireCornerTriggers && cornerTriggers.Count > 0)
            SetHud(needRouletteMessage);
        else
            SetHud(string.Empty);
        messageTimer = 0f;
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

    /// <summary>Starts the fight immediately, ignoring the summoning conditions. Used by the debug toggle.</summary>
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
        spawnPoint += spawnOffset;

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
