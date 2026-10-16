using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameTimer : MonoBehaviour
{
public static GameTimer Instance
    {
get; private set;
    }

    [Header("Timer Settings")]
    [SerializeField] private float startingTime = 90f;
    [SerializeField] private bool startOnLoad = true;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    [Header("Low Time Warning")]
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.red;

    [Header("Audio")]
    [Tooltip("Played once when the countdown reaches this many seconds remaining.")]
    [SerializeField] private float audioTriggerTime = 30f;
    [SerializeField] private AudioClip timeWarningClip;
    [SerializeField, Range(0f, 1f)] private float timeWarningVolume = 1f;

public float TimeRemaining
    {
get; private set;
    }
public bool IsRunning
    {
get; private set;
    }
public bool IsGameOver
    {
get; private set;
    }

private AudioSource audioSource;
private bool hasPlayedTimeWarning;

private void Awake()
    {
Instance = this;
TimeRemaining = startingTime;

if (gameOverPanel != null)
        {
gameOverPanel.SetActive(false);
        }

audioSource = GetComponent<AudioSource>();
if (audioSource == null)
    {
audioSource = gameObject.AddComponent<AudioSource>();
    }
audioSource.playOnAwake = false;
audioSource.spatialBlend = 0f;
    }

private void Start()
    {
Time.timeScale = 1f;
UpdateTimerUI();

if (startOnLoad)
        {
StartTimer();
        }
    }

private void Update()
    {
if (!IsRunning) return;

// Unscaled would keep running while paused, so use scaled deltaTime
// (the pause menu sets Time.timeScale to 0).
TimeRemaining -= Time.deltaTime;

if (TimeRemaining <= 0f)
        {
TimeRemaining = 0f;
UpdateTimerUI();
TriggerGameOver();
return;
        }

CheckTimeWarningAudio();
UpdateTimerUI();
    }

public void StartTimer()
    {
if (IsGameOver) return;
IsRunning = true;
    }

public void PauseTimer()
    {
IsRunning = false;
    }

public void ResumeTimer()
    {
if (IsGameOver) return;
IsRunning = true;
    }

/// <summary>Add (or subtract, with a negative value) seconds to the clock.</summary>
public void AddTime(float seconds)
    {
if (IsGameOver) return;

TimeRemaining = Mathf.Max(0f, TimeRemaining + seconds);

// Adding time back above the trigger point should let the warning play again later.
if (TimeRemaining > audioTriggerTime)
    {
hasPlayedTimeWarning = false;
    }

UpdateTimerUI();
    }

public void ResetTimer()
    {
IsGameOver = false;
TimeRemaining = startingTime;
hasPlayedTimeWarning = false;

if (gameOverPanel != null)
        {
gameOverPanel.SetActive(false);
        }

UpdateTimerUI();
Time.timeScale = 1f;
    }

private void CheckTimeWarningAudio()
    {
if (hasPlayedTimeWarning) return;
if (TimeRemaining > audioTriggerTime) return;

hasPlayedTimeWarning = true;
PlayTimeWarningSFX();
    }

private void PlayTimeWarningSFX()
    {
if (timeWarningClip == null || audioSource == null)
return;

audioSource.PlayOneShot(timeWarningClip, timeWarningVolume);
    }

private void UpdateTimerUI()
    {
if (timerText == null) return;

int minutes = Mathf.FloorToInt(TimeRemaining / 60f);
int seconds = Mathf.FloorToInt(TimeRemaining % 60f);
timerText.text = $"{minutes:00}:{seconds:00}";
timerText.color = TimeRemaining <= warningThreshold ? warningColor : normalColor;
    }

public void TriggerGameOver()
    {
if (IsGameOver) return;

IsGameOver = true;
IsRunning = false;

if (finalScoreText != null && ScoreManager.Instance != null)
        {
finalScoreText.text = "Final Score: " + ScoreManager.Instance.CurrentScore;
        }

if (gameOverPanel != null)
        {
gameOverPanel.SetActive(true);
        }

// Freeze the table. Do this last so the panel is already visible.
Time.timeScale = 0f;
    }

// Hook these up to the buttons on the game over panel.
public void RestartGame()
    {
Time.timeScale = 1f;
SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

public void QuitGame()
    {
Time.timeScale = 1f;

#if UNITY_EDITOR
UnityEditor.EditorApplication.isPlaying = false;
#else
Application.Quit();
#endif
    }

private void OnDestroy()
    {
if (Instance == this)
        {
Instance = null;
        }
    }
}