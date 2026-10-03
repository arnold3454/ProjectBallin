using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating "CRIT! +N" text that pops up, drifts upward and fades. Built in code like the upgrade popup.
/// </summary>
public class CritPopup : MonoBehaviour
{
    private static readonly Color TextColor = new Color(1f, 0.55f, 0.1f, 1f);
    private static readonly Color OutlineColor = new Color(0.25f, 0.05f, 0f, 1f);

    private const float Duration = 0.9f;
    private const float PopTime = 0.12f;
    private const float RiseDistance = 90f;
    private const float SpreadX = 140f;

    private RectTransform canvasRect;

    public void Build()
    {
        GameObject canvasObject = new GameObject("CritCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        // Below the upgrade popup (200), and no raycaster so it never eats clicks.
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        UnityEngine.UI.CanvasScaler scaler = canvasObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasRect = canvasObject.GetComponent<RectTransform>();
    }

    /// <summary>Pops a crit message showing the points that crit scored.</summary>
    public void Spawn(int points)
    {
        if (canvasRect == null)
            return;

        GameObject textObject = new GameObject("CritText", typeof(RectTransform));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(canvasRect, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.72f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(600f, 120f);
        rect.anchoredPosition = new Vector2(Random.Range(-SpreadX, SpreadX), 0f);

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = $"CRIT!  +{points}";
        label.fontSize = 72;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = TextColor;
        label.outlineColor = OutlineColor;
        label.outlineWidth = 0.25f;
        label.raycastTarget = false;

        StartCoroutine(Animate(rect, label));
    }

    private static IEnumerator Animate(RectTransform rect, TextMeshProUGUI label)
    {
        Vector2 start = rect.anchoredPosition;
        float time = 0f;

        while (time < Duration && rect != null)
        {
            // Unscaled so the text still finishes if the upgrade popup pauses the game.
            // Capped so one long frame can't skip the whole animation.
            time += Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Mathf.Clamp01(time / Duration);

            // Quick overshoot pop, then settle.
            float scale = time < PopTime
                ? Mathf.Lerp(0.6f, 1.3f, time / PopTime)
                : Mathf.Lerp(1.3f, 1f, Mathf.Clamp01((time - PopTime) / 0.15f));
            rect.localScale = Vector3.one * scale;

            rect.anchoredPosition = start + Vector2.up * (RiseDistance * t);

            Color color = label.color;
            color.a = t < 0.55f ? 1f : 1f - (t - 0.55f) / 0.45f;
            label.color = color;

            yield return null;
        }

        if (rect != null)
            Destroy(rect.gameObject);
    }
}
