using System;
using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
public static ScoreManager Instance
    {
get; private set;
    }
public int CurrentScore { 
get; private set; 
    }
    [SerializeField] private TextMeshProUGUI scoreText;

    /// <summary>Raised with the new total after every change to the score.</summary>
    public event Action<int> ScoreChanged;

    /// <summary>Raised with the points a critical hit just scored.</summary>
    public event Action<int> CritScored;

    [Header("Audio")]
    [Tooltip("Played whenever score is added.")]
    [SerializeField] private AudioClip scoreClip;
    [SerializeField, Range(0f, 1f)] private float scoreVolume = 1f;

private AudioSource audioSource;

private void Awake()
    {
Instance = this;
UpdateScoreUI();

audioSource = GetComponent<AudioSource>();
if (audioSource == null)
    {
audioSource = gameObject.AddComponent<AudioSource>();
    }
audioSource.playOnAwake = false;
audioSource.spatialBlend = 0f;
    }

public void AddScore(int amount)
    {
bool crit = false;
if (UpgradeManager.Instance != null)
amount = UpgradeManager.Instance.ModifyPoints(amount, out crit);

CurrentScore += amount;
Debug.Log(crit ? $"CRIT! +{amount} Score: {CurrentScore}" : $"Score: {CurrentScore}");
UpdateScoreUI();
PlayScoreSFX();
ScoreChanged?.Invoke(CurrentScore);
if (crit)
CritScored?.Invoke(amount);
    }

private void PlayScoreSFX()
    {
if (scoreClip == null || audioSource == null)
return;

audioSource.PlayOneShot(scoreClip, scoreVolume);
    }

private void UpdateScoreUI()
    {
if (scoreText != null)
        {
scoreText.text = "Score: " + CurrentScore;
        }
    }
}