using System.Collections;
using UnityEngine;

/// <summary>Shows a UI group after the configured time without a ball launch.</summary>
public class BallLaunchInactivityPrompt : MonoBehaviour
{
    private const float BallCheckInterval = 0.5f;

    [SerializeField] private BallLauncher launcher;
    [Tooltip("Place the image and text under this CanvasGroup so they appear and hide together.")]
    [SerializeField] private CanvasGroup promptGroup;
    [SerializeField, Min(0f)] private float inactivitySeconds = 10f;
    [Header("Entrance Animation")]
    [SerializeField, Min(0.01f)] private float bounceDuration = 0.25f;
    [SerializeField, Min(0f)] private float startScale = 0.85f;
    [SerializeField, Min(1f)] private float peakScale = 1.08f;

    private Transform promptTransform;
    private Vector3 baseScale;
    private Coroutine bounceRoutine;

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

        promptTransform = promptGroup.transform;
        baseScale = promptTransform.localScale;
        SetPromptVisible(false);

        if (launcher == null)
            Debug.LogError("Inactivity prompt could not find a BallLauncher. Assign one in the Inspector.", this);
    }

    private void OnEnable()
    {
        if (promptGroup == null || launcher == null)
            return;

        launcher.BallLoaded += OnBallLoaded;
        launcher.BallLaunched += OnBallLaunched;

        if (launcher.HasBallLoaded)
            TryStartInactivityTimer();
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(ShowPrompt));
        CancelInvoke(nameof(TryStartInactivityTimer));

        if (launcher != null)
        {
            launcher.BallLoaded -= OnBallLoaded;
            launcher.BallLaunched -= OnBallLaunched;
        }

        if (promptGroup != null)
            SetPromptVisible(false);
    }

    private void OnBallLaunched()
    {
        CancelInvoke(nameof(ShowPrompt));
        CancelInvoke(nameof(TryStartInactivityTimer));
        HidePromptWithBounce();
    }

    private void OnBallLoaded()
    {
        CancelInvoke(nameof(ShowPrompt));
        CancelInvoke(nameof(TryStartInactivityTimer));
        TryStartInactivityTimer();
    }

    private void TryStartInactivityTimer()
    {
        if (!launcher.HasBallLoaded)
            return;

        if (IsBallInPlay())
        {
            Invoke(nameof(TryStartInactivityTimer), BallCheckInterval);
            return;
        }

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
        if (!launcher.HasBallLoaded)
            return;

        if (IsBallInPlay())
        {
            Invoke(nameof(TryStartInactivityTimer), BallCheckInterval);
            return;
        }

        StopBounceAnimation();
        SetPromptVisible(true);
        promptTransform.localScale = baseScale * startScale;
        bounceRoutine = StartCoroutine(BounceIn());
    }

    private bool IsBallInPlay()
    {
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        for (int i = 0; i < balls.Length; i++)
        {
            if (!launcher.IsBallLoaded(balls[i]))
                return true;
        }

        return false;
    }

    private void HidePromptWithBounce()
    {
        StopBounceAnimation();

        if (promptGroup.alpha <= 0f)
        {
            SetPromptVisible(false);
            return;
        }

        promptGroup.interactable = false;
        promptGroup.blocksRaycasts = false;
        bounceRoutine = StartCoroutine(BounceOut());
    }

    private IEnumerator BounceIn()
    {
        float duration = Mathf.Max(0.01f, bounceDuration);
        float elapsed = 0f;
        float peakTime = duration * 0.55f;

        while (elapsed < duration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float scale = elapsed < peakTime
                ? Mathf.Lerp(startScale, peakScale, elapsed / peakTime)
                : Mathf.Lerp(peakScale, 1f, (elapsed - peakTime) / (duration - peakTime));

            promptTransform.localScale = baseScale * scale;
            yield return null;
        }

        promptTransform.localScale = baseScale;
        bounceRoutine = null;
    }

    private IEnumerator BounceOut()
    {
        float duration = Mathf.Max(0.01f, bounceDuration);
        float elapsed = 0f;
        float peakTime = duration * 0.35f;
        float startAlpha = promptGroup.alpha;
        Vector3 startScale = promptTransform.localScale;
        Vector3 peakLocalScale = baseScale * peakScale;

        while (elapsed < duration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (elapsed < peakTime)
            {
                float t = elapsed / peakTime;
                promptTransform.localScale = Vector3.Lerp(startScale, peakLocalScale, t);
            }
            else
            {
                float t = (elapsed - peakTime) / (duration - peakTime);
                promptTransform.localScale = Vector3.Lerp(peakLocalScale, Vector3.zero, t);
                promptGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }

            yield return null;
        }

        promptTransform.localScale = baseScale;
        promptGroup.alpha = 0f;
        bounceRoutine = null;
    }

    private void StopBounceAnimation()
    {
        if (bounceRoutine == null)
            return;

        StopCoroutine(bounceRoutine);
        bounceRoutine = null;
    }

    private void SetPromptVisible(bool visible)
    {
        if (!visible)
        {
            StopBounceAnimation();
            promptTransform.localScale = baseScale;
        }

        promptGroup.alpha = visible ? 1f : 0f;
        promptGroup.interactable = visible;
        promptGroup.blocksRaycasts = visible;
    }
}
