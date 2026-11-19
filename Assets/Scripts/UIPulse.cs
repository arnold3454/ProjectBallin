using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Makes a HUD element hard to miss: while the object is switched on it swells and shrinks a
/// little and, optionally, a graphic on it glows between its own colour and a highlight. Runs on
/// unscaled time, so it keeps going while the game is paused. Put it on the element itself (the
/// UpgradeReadyPanel uses it) and tune it in the Inspector.
/// </summary>
public class UIPulse : MonoBehaviour
{
    [Tooltip("How much bigger the element gets at the top of each pulse. 0.05 is 5%.")]
    [SerializeField, Min(0f)] private float scaleAmount = 0.05f;
    [SerializeField, Min(0.1f)] private float pulsesPerSecond = 1.2f;

    [Header("Glow (optional)")]
    [Tooltip("Graphic that fades toward the glow colour and back with each pulse. Leave empty for no glow.")]
    [SerializeField] private Graphic glowGraphic;
    [SerializeField] private Color glowColor = new Color(0.45f, 0.34f, 0.05f, 0.95f);

    private Vector3 baseScale;
    private Color baseColor;
    private float startTime;
    private bool captured;

    private void OnEnable()
    {
        if (!captured)
        {
            baseScale = transform.localScale;
            if (glowGraphic != null)
                baseColor = glowGraphic.color;
            captured = true;
        }

        // Start every showing from the small end of the pulse.
        startTime = Time.unscaledTime;
    }

    private void OnDisable()
    {
        if (!captured)
            return;

        transform.localScale = baseScale;
        if (glowGraphic != null)
            glowGraphic.color = baseColor;
    }

    private void Update()
    {
        float phase = (Time.unscaledTime - startTime) * pulsesPerSecond * Mathf.PI * 2f;
        float wave = 0.5f - 0.5f * Mathf.Cos(phase);

        transform.localScale = baseScale * (1f + scaleAmount * wave);
        if (glowGraphic != null)
            glowGraphic.color = Color.Lerp(baseColor, glowColor, wave);
    }
}
