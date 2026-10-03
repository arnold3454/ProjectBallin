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

    [Header("Audio")]
    [Tooltip("Played whenever score is added.")]
    [SerializeField] private AudioClip scoreClip;
    [SerializeField, Range(0f, 1f)] private float scoreVolume = 1f;

private AudioSource audioSource;
private float scoreRemainder;

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
// The ball type's base multiplier. Fractions carry over to the next score so a below-1x
// ball still earns points (0.5x scores a point every second hit).
if (amount > 0)
        {
float value = amount * BallTypeManager.PointsMultiplier + scoreRemainder;
amount = Mathf.FloorToInt(value);
scoreRemainder = value - amount;
        }

CurrentScore += amount;
Debug.Log($"Score: {CurrentScore}");
UpdateScoreUI();
// Don't ding until a whole point lands.
if (amount > 0)
PlayScoreSFX();
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