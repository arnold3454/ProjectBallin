using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating "CRIT! +N" text that pops up, drifts upward and fades. The CritPopup object under the
/// Canvas holds a handful of CritText labels built in the hierarchy; each crit takes the next one in
/// turn, so nothing is created at runtime. A label still animating is restarted if its turn comes round.
/// </summary>
public class CritPopup : MonoBehaviour
{
    [Tooltip("The CritText labels under this object. More labels means more crits can be on screen at once.")]
    [SerializeField] private TextMeshProUGUI[] labels;
    [SerializeField, Min(0.01f)] private float duration = 0.9f;
    [SerializeField, Min(0.01f)] private float popTime = 0.12f;
    [SerializeField, Min(0f)] private float riseDistance = 90f;
    [SerializeField, Min(0f)] private float horizontalSpread = 140f;
    [SerializeField, Min(0f)] private float startScale = 0.6f;
    [SerializeField, Min(0f)] private float peakScale = 1.3f;
    [SerializeField, Min(0.01f)] private float settleTime = 0.15f;
    [SerializeField, Range(0f, 1f)] private float fadeStart = 0.55f;

    private Vector2[] restPositions;
    private Vector3[] restScales;
    private Color[] restColors;
    private Coroutine[] running;
    private int next;

    private void Awake()
    {
        if (labels == null || labels.Length == 0)
        {
            Debug.LogWarning("CritPopup has no CritText labels assigned (under the Canvas), so crits won't show their floating text.", this);
            return;
        }

        restPositions = new Vector2[labels.Length];
        restScales = new Vector3[labels.Length];
        restColors = new Color[labels.Length];
        running = new Coroutine[labels.Length];

        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null)
                continue;

            restPositions[i] = labels[i].rectTransform.anchoredPosition;
            restScales[i] = labels[i].rectTransform.localScale;
            restColors[i] = labels[i].color;
            labels[i].gameObject.SetActive(false);
        }
    }

    /// <summary>Pops a crit message showing the points that crit scored.</summary>
    public void Spawn(int points)
    {
        if (labels == null || labels.Length == 0)
            return;

        int index = next;
        next = (next + 1) % labels.Length;

        TextMeshProUGUI label = labels[index];
        if (label == null)
            return;

        if (running[index] != null)
            StopCoroutine(running[index]);

        RectTransform rect = label.rectTransform;
        rect.anchoredPosition = restPositions[index] + Vector2.right * Random.Range(-horizontalSpread, horizontalSpread);
        rect.localScale = restScales[index];
        label.color = restColors[index];
        label.text = $"CRIT!  +{points}";
        label.gameObject.SetActive(true);

        running[index] = StartCoroutine(Animate(index, rect, label, restColors[index], restScales[index]));
    }

    private IEnumerator Animate(int index, RectTransform rect, TextMeshProUGUI label, Color initialColor, Vector3 baseScale)
    {
        Vector2 start = rect.anchoredPosition;
        float time = 0f;

        while (time < duration)
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

        label.gameObject.SetActive(false);
        running[index] = null;
    }
}
