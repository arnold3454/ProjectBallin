using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The boss fight's own countdown, separate from the main <see cref="GameTimer"/>. It is always
/// shorter than the main clock: the duration is capped to a fraction of whatever the main timer
/// has left, so the fight never outlives the round. It draws on its own progress bar under the
/// Canvas, assigned in the Inspector. Lives on the GameManager next to
/// <see cref="BossManager"/>, which starts and stops it.
/// </summary>
public class BossTimer : MonoBehaviour
{
    [Header("Duration")]
    [Tooltip("Seconds the player gets to beat the boss, before the main clock cap is applied.")]
    [SerializeField, Min(1f)] private float duration = 90f;
    [Tooltip("The boss timer never runs longer than this fraction of the main timer's remaining time, so it always ends before the round does.")]
    [SerializeField, Range(0.1f, 1f)] private float maxFractionOfMainClock = 0.75f;

    [Header("UI")]
    [Tooltip("Filled Image that shows the boss countdown under the Canvas.")]
    [SerializeField] private Image timerBar;
    [Tooltip("Container holding the boss timer background and fill. Hidden until the boss fight starts.")]
    [SerializeField] private GameObject timerContainer;

    [Header("Low Time Warning")]
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private Color normalColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color warningColor = Color.red;

    /// <summary>Raised once when the countdown reaches zero.</summary>
    public event Action TimedOut;

    public bool IsRunning { get; private set; }
    public float TimeRemaining { get; private set; }
    /// <summary>Length of the current countdown after the main clock cap. 0 while idle.</summary>
    public float Duration { get; private set; }
    /// <summary>0 when the countdown is full, 1 when it has run out.</summary>
    public float Urgency => IsRunning && Duration > 0f ? 1f - Mathf.Clamp01(TimeRemaining / Duration) : 0f;

    private void Awake()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (!IsRunning)
            return;

        // Scaled time, so the pause menu (timeScale 0) stops the boss clock along with the main one.
        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            IsRunning = false;
            UpdateUI();
            TimedOut?.Invoke();
            return;
        }

        UpdateUI();
    }

    /// <summary>Starts the countdown from the configured duration, capped below the main clock.</summary>
    public void Begin()
    {
        Duration = duration;

        GameTimer main = GameTimer.Instance;
        if (main != null && main.IsRunning)
            Duration = Mathf.Min(Duration, main.TimeRemaining * maxFractionOfMainClock);

        TimeRemaining = Duration;
        IsRunning = true;
        UpdateUI();
    }

    /// <summary>Stops the countdown without raising <see cref="TimedOut"/>.</summary>
    public void Stop()
    {
        IsRunning = false;
        TimeRemaining = 0f;
        Duration = 0f;
        UpdateUI();
    }

    /// <summary>Adds (or, with a negative value, removes) seconds from the running countdown.</summary>
    public void AddTime(float seconds)
    {
        if (!IsRunning)
            return;

        TimeRemaining = Mathf.Max(0f, TimeRemaining + seconds);
        Duration = Mathf.Max(Duration, TimeRemaining);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerContainer == null)
        {
            if (!warnedNoContainer)
            {
                Debug.LogWarning("BossTimer has no timer container assigned. Assign the boss time container in the Inspector.", this);
                warnedNoContainer = true;
            }
        }
        else if (timerContainer.activeSelf != IsRunning)
        {
            timerContainer.SetActive(IsRunning);
        }

        if (timerBar == null)
        {
            if (!warnedNoBar)
            {
                Debug.LogWarning("BossTimer has no progress bar assigned. Assign its filled Image in the Inspector.", this);
                warnedNoBar = true;
            }
            return;
        }

        if (!IsRunning)
            return;

        timerBar.fillAmount = Duration > 0f
            ? Mathf.Clamp01(TimeRemaining / Duration)
            : 0f;
        timerBar.color = TimeRemaining <= warningThreshold ? warningColor : normalColor;
    }

    private bool warnedNoBar;
    private bool warnedNoContainer;
}
