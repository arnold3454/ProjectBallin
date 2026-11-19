using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD bar that fills as points are earned toward the next upgrade. A full bar is what hands
/// the player an upgrade choice (see UpgradeManager); the bar then empties and starts over.
/// Drives a Slider and/or a filled Image, plus an optional label; references are assigned in the Inspector.
/// </summary>
public class XpBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Display")]
    [Tooltip("{0} = points earned toward the next upgrade, {1} = points needed, {2} = the player's level.")]
    [SerializeField] private string labelFormat = "XP {0} / {1}";
    [Tooltip("How fast the bar animates toward its target. 0 snaps instantly.")]
    [SerializeField, Min(0f)] private float fillSpeed = 6f;

    private float target;
    private bool fillingToTop;
    private bool subscribed;

    private void Awake()
    {
        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            slider.interactable = false;
        }

        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillAmount = 0f;
            fillImage.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        // The manager may come alive after us.
        if (!subscribed)
            TrySubscribe();

        if (!HasProgressTarget)
            return;

        // Unscaled so the bar still moves while the upgrade popup has the game paused.
        float step = fillSpeed <= 0f ? 1f : fillSpeed * Time.unscaledDeltaTime;
        float currentProgress = GetProgress();

        if (fillingToTop)
        {
            // Show the bar actually filling before it resets for the next level.
            currentProgress = Mathf.MoveTowards(currentProgress, 1f, step);
            SetProgress(currentProgress);
            if (currentProgress >= 1f)
            {
                fillingToTop = false;
                SetProgress(0f);
            }
            return;
        }

        SetProgress(Mathf.MoveTowards(currentProgress, target, step));
    }

    private void TrySubscribe()
    {
        if (subscribed || UpgradeManager.Instance == null)
            return;

        UpgradeManager.Instance.XpChanged += OnXpChanged;
        subscribed = true;
        OnXpChanged(UpgradeManager.Instance.CurrentXp, UpgradeManager.Instance.XpPerUpgrade);
        SetProgress(target);
    }

    private void Unsubscribe()
    {
        if (!subscribed)
            return;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.XpChanged -= OnXpChanged;
        subscribed = false;
    }

    private void OnXpChanged(int xp, int xpPerUpgrade)
    {
        float newTarget = xpPerUpgrade > 0 ? Mathf.Clamp01(xp / (float)xpPerUpgrade) : 0f;

        // Dropping back means the bar just filled and an upgrade was earned.
        if (newTarget < target && HasProgressTarget)
            fillingToTop = true;

        target = newTarget;

        if (label != null)
        {
            int level = UpgradeManager.Instance != null ? UpgradeManager.Instance.UpgradeLevel + 1 : 1;
            label.text = string.Format(labelFormat, xp, xpPerUpgrade, level);
        }
    }

    private bool HasProgressTarget => slider != null || fillImage != null;

    private float GetProgress()
    {
        return fillImage != null ? fillImage.fillAmount : slider.value;
    }

    private void SetProgress(float progress)
    {
        if (slider != null)
            slider.value = progress;

        if (fillImage != null)
            fillImage.fillAmount = progress;
    }
}
