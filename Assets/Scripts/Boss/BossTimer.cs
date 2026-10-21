using System;
using TMPro;
using UnityEngine;

/// <summary>
/// The boss fight's own countdown, separate from the main <see cref="GameTimer"/>. It is always
/// shorter than the main clock: the duration is capped to a fraction of whatever the main timer
/// has left, so the fight never outlives the round. It draws on its own label, which by default
/// is a copy of the main timer's label placed directly below it. Lives on the GameManager next
/// to <see cref="BossManager"/>, which starts and stops it.
/// </summary>
public class BossTimer : MonoBehaviour
{
    [Header("Duration")]
    [Tooltip("Seconds the player gets to beat the boss, before the main clock cap is applied.")]
    [SerializeField, Min(1f)] private float duration = 45f;
    [Tooltip("The boss timer never runs longer than this fraction of the main timer's remaining time, so it always ends before the round does.")]
    [SerializeField, Range(0.1f, 1f)] private float maxFractionOfMainClock = 0.75f;

    [Header("UI")]
    [Tooltip("Label that shows the countdown. Leave empty to copy the main timer's label at runtime and place it below it.")]
    [SerializeField] private TextMeshProUGUI timerText;
    [Tooltip("Offset from the main timer's label when the boss label is created at runtime.")]
    [SerializeField] private Vector2 offsetFromMainTimer = new Vector2(0f, -80f);
    [Tooltip("Font size relative to the main timer's label when the boss label is created at runtime.")]
    [SerializeField, Range(0.1f, 2f)] private float fontScale = 0.65f;
    [Tooltip("Text shown in front of the time, e.g. \"BOSS \".")]
    [SerializeField] private string prefix = "";
    [Tooltip("Hide the label while no boss fight is running.")]
    [SerializeField] private bool hideWhenIdle = true;

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
        TextMeshProUGUI text = GetOrCreateLabel();
        if (text == null)
            return;

        if (hideWhenIdle && text.gameObject.activeSelf != IsRunning)
            text.gameObject.SetActive(IsRunning);

        if (!IsRunning && hideWhenIdle)
            return;

        int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
        text.text = $"{prefix}{minutes:00}:{seconds:00}";
        text.color = TimeRemaining <= warningThreshold ? warningColor : normalColor;
    }

    /// <summary>
    /// Returns the assigned label, or builds one by copying the main timer's label so the boss
    /// clock matches its font and sits just below it.
    /// </summary>
    private TextMeshProUGUI GetOrCreateLabel()
    {
        if (timerText != null)
            return timerText;

        GameTimer main = GameTimer.Instance;
        TextMeshProUGUI source = main != null ? main.TimerText : null;
        if (source == null)
            return null;

        timerText = Instantiate(source, source.transform.parent);
        timerText.name = "BossTimer";
        timerText.fontSize = source.fontSize * fontScale;
        timerText.text = string.Empty;

        RectTransform rect = timerText.rectTransform;
        RectTransform sourceRect = source.rectTransform;
        rect.anchoredPosition = sourceRect.anchoredPosition + offsetFromMainTimer;
        rect.SetSiblingIndex(sourceRect.GetSiblingIndex() + 1);

        return timerText;
    }
}
