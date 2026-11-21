using UnityEngine;

/// <summary>Shows a UI group after the configured time without a ball launch.</summary>
public class BallLaunchInactivityPrompt : MonoBehaviour
{
    [SerializeField] private BallLauncher launcher;
    [Tooltip("Place the image and text under this CanvasGroup so they appear and hide together.")]
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField, Min(0f)] private float inactivitySeconds = 10f;

    private void Awake()
    {
        if (launcher == null)
            launcher = FindAnyObjectByType<BallLauncher>();

        if (promptGroup == null)
            promptGroup = GetComponent<CanvasGroup>();

        if (promptGroup == null)
        {
            Debug.LogError("Inactivity prompt requires a CanvasGroup on this object or an assigned UI group.", this);
            enabled = false;
            return;
        }

        SetPromptVisible(false);

        if (launcher == null)
            Debug.LogError("Inactivity prompt could not find a BallLauncher. Assign one in the Inspector.", this);
    }

    private void OnEnable()
    {
        if (promptGroup == null || launcher == null)
            return;

        launcher.BallLaunched += OnBallLaunched;
        SchedulePrompt();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ShowPrompt));

        if (launcher != null)
            launcher.BallLaunched -= OnBallLaunched;

        if (promptGroup != null)
            SetPromptVisible(false);
    }

    private void OnBallLaunched()
    {
        SetPromptVisible(false);
        SchedulePrompt();
    }

    private void SchedulePrompt()
    {
        CancelInvoke(nameof(ShowPrompt));

        if (inactivitySeconds <= 0f)
        {
            ShowPrompt();
            return;
        }

        Invoke(nameof(ShowPrompt), inactivitySeconds);
    }

    private void ShowPrompt()
    {
        SetPromptVisible(true);
    }

    private void SetPromptVisible(bool visible)
    {
        promptGroup.alpha = visible ? 1f : 0f;
        promptGroup.interactable = visible;
        promptGroup.blocksRaycasts = visible;
    }
}
