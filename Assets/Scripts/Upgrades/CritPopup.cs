using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating "CRIT! +N" text that pops up, drifts upward and fades using serialized UI references.
/// </summary>
public class CritPopup : MonoBehaviour
{
    [SerializeField] private RectTransform canvasRect;
    [SerializeField] private TextMeshProUGUI textPrefab;
    [SerializeField, Min(0.01f)] private float duration = 0.9f;
    [SerializeField, Min(0.01f)] private float popTime = 0.12f;
    [SerializeField, Min(0f)] private float riseDistance = 90f;
    [SerializeField, Min(0f)] private float horizontalSpread = 140f;
    [SerializeField, Min(0f)] private float startScale = 0.6f;
    [SerializeField, Min(0f)] private float peakScale = 1.3f;
    [SerializeField, Min(0.01f)] private float settleTime = 0.15f;
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.55f;

    /// <summary>Pops a crit message showing the points that crit scored.</summary>
    public void Spawn(int points)
    {
        if (canvasRect == null || textPrefab == null)
            return;

        TextMeshProUGUI label = Instantiate(textPrefab, canvasRect, false);
        RectTransform rect = label.rectTransform;
        rect.anchoredPosition += Vector2.right * Random.Range(-horizontalSpread, horizontalSpread);
        label.text = $"CRIT!  +{points}";
        label.gameObject.SetActive(true);

        StartCoroutine(Animate(rect, label, label.color));
    }

    private IEnumerator Animate(RectTransform rect, TextMeshProUGUI label, Color initialColor)
    {
        Vector2 start = rect.anchoredPosition;
        Vector3 baseScale = rect.localScale;
        float time = 0f;

        while (time < duration && rect != null)
        {
            // Unscaled so the text still finishes if the upgrade popup pauses the game.
            // Capped so one long frame can't skip the whole animation.
            time += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Mathf.Clamp01(time / duration);

            // Quick overshoot pop, then settle.
            float scale = time < popTime
                ? Mathf.Lerp(startScale, peakScale, time / popTime)
                : Mathf.Lerp(peakScale, 1f, Mathf.Clamp01((time - popTime) / settleTime));
            rect.localScale = baseScale * scale;

            rect.anchoredPosition = start + Vector2.up * (riseDistance * t);

            Color color = initialColor;
            float fade = Mathf.InverseLerp(fadeStart, 1f, t);
            color.a *= 1f - fade;
            label.color = color;

            yield return null;
        }

        if (rect != null)
            Destroy(rect.gameObject);
    }
}
