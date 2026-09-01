using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance
    {
        get; private set;
    }
    public int CurrentScore { 
        get; private set; 
    }
    private void Awake()
    {
        Instance = this;
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        Debug.Log($"Score: {CurrentScore}");
    }
 
}
