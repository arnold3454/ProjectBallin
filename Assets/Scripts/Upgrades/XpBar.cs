using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD bar that fills as points are earned toward the next upgrade. A full bar is what hands
/// the player an upgrade choice (see UpgradeManager); the bar then empties and starts over.
/// Build the bar in the scene hierarchy (a non-interactable Slider with a Fill, plus an optional
/// label) and assign the references in the Inspector.
/// </summary>
public class XpBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI label;

    [Header("Display")]
    [Tooltip("{0} = points earned toward the next upgrade, {1} = points needed.")]
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

        if (slider == null)
            return;

        // Unscaled so the bar still moves while the upgrade popup has the game paused.
        float step = fillSpeed <= 0f ? 1f : fillSpeed * Time.unscaledDeltaTime;

        if (fillingToTop)
        {
            // Show the bar actually filling before it resets for the next level.
            slider.value = Mathf.MoveTowards(slider.value, 1f, step);
            if (slider.value >= 1f)
            {
                fillingToTop = false;
                slider.value = 0f;
            }
            return;
        }

        slider.value = Mathf.MoveTowards(slider.value, target, step);
    }

    private void TrySubscribe()
    {
        if (subscribed || UpgradeManager.Instance == null)
            return;

        UpgradeManager.Instance.XpChanged += OnXpChanged;
        subscribed = true;
        OnXpChanged(UpgradeManager.Instance.CurrentXp, UpgradeManager.Instance.XpPerUpgrade);
        if (slider != null)
            slider.value = target;
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
        if (newTarget < target && slider != null)
            fillingToTop = true;

        target = newTarget;

        if (label != null)
            label.text = string.Format(labelFormat, xp, xpPerUpgrade);
    }
}
