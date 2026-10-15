using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// A large button tucked into a top corner of the table. Every ball hit scores points,
/// toggles the cap between its unlit and lit colours, and gives momentary feedback:
/// the cap is pushed in and a light pops on.
/// </summary>
[RequireComponent(typeof(BallHitboxTrigger))]
public class CornerBossTrigger : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField, Min(0)] private int pointValue = 5;
    [Tooltip("Seconds after a hit before the button can score again, so one bounce can't double count.")]
    [SerializeField, Min(0f)] private float hitCooldown = 0.25f;

    [Header("Button Press")]
    [Tooltip("The cap that gets pushed in when the ball hits. Its authored position is the rest pose.")]
    [SerializeField] private Transform buttonCap;
    [SerializeField, Min(0f)] private float pressDepth = 0.35f;
    [SerializeField, Min(0.01f)] private float pressDuration = 0.06f;
    [SerializeField, Min(0.01f)] private float releaseDuration = 0.3f;

    [Header("Toggle Colour")]
    [Tooltip("Renderer whose colour toggles on hit. Defaults to the button cap's renderer.")]
    [SerializeField] private Renderer capRenderer;
    [Tooltip("Colour shown once the button has been hit. Hitting it again switches back to the material's own colour.")]
    [SerializeField] private Color litColor = new Color(1f, 0.9f, 0.25f);
    [Tooltip("Start the round already lit.")]
    [SerializeField] private bool startLit;

    [Header("Hit Light")]
    [Tooltip("Optional light that pops on with the hit and fades out.")]
    [SerializeField] private Light hitLight;
    [SerializeField, Min(0f)] private float lightIntensity = 8f;
    [SerializeField, Min(0.01f)] private float lightFadeDuration = 0.45f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Raised after every scoring hit. Lets future boss logic react to the triggers.</summary>
    public event Action<CornerBossTrigger> Hit;

    /// <summary>True while the button shows its lit colour. Flips on every hit.</summary>
    public bool IsLit { get; private set; }

    private BallHitboxTrigger hitbox;
    private MaterialPropertyBlock colorBlock;
    private Coroutine feedback;
    private Vector3 restPosition;
    private Color baseColor = Color.white;
    private float lastHitTime = float.NegativeInfinity;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();

        if (capRenderer == null && buttonCap != null)
            capRenderer = buttonCap.GetComponentInChildren<Renderer>();

        if (buttonCap != null)
            restPosition = buttonCap.localPosition;

        colorBlock = new MaterialPropertyBlock();
        if (capRenderer != null && capRenderer.sharedMaterial != null)
        {
            Material material = capRenderer.sharedMaterial;
            if (material.HasProperty(BaseColorId))
                baseColor = material.GetColor(BaseColorId);
            else if (material.HasProperty(ColorId))
                baseColor = material.GetColor(ColorId);
        }

        if (hitLight != null)
            hitLight.intensity = 0f;

        SetLit(startLit);
    }

    private void OnEnable()
    {
        if (hitbox == null)
            hitbox = GetComponent<BallHitboxTrigger>();

        hitbox.BallHit += OnBallHit;
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.BallHit -= OnBallHit;

        if (feedback != null)
        {
            StopCoroutine(feedback);
            feedback = null;
        }

        ResetVisuals();
    }

    private void OnBallHit(Collider ball)
    {
        if (Time.time - lastHitTime < hitCooldown)
            return;

        lastHitTime = Time.time;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(pointValue);

        SetLit(!IsLit);

        if (feedback != null)
            StopCoroutine(feedback);
        feedback = StartCoroutine(PlayFeedback());

        Hit?.Invoke(this);
    }

    /// <summary>Switches the cap between its lit colour and the material's own colour.</summary>
    public void SetLit(bool lit)
    {
        IsLit = lit;
        ApplyColor(lit ? litColor : baseColor);
    }

    private IEnumerator PlayFeedback()
    {
        Vector3 pressedPosition = restPosition + Vector3.down * pressDepth;
        float total = Mathf.Max(pressDuration + releaseDuration, lightFadeDuration);
        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;

            if (buttonCap != null)
            {
                float t = elapsed < pressDuration
                    ? Mathf.SmoothStep(0f, 1f, elapsed / pressDuration)
                    : 1f - Mathf.SmoothStep(0f, 1f, (elapsed - pressDuration) / releaseDuration);
                buttonCap.localPosition = Vector3.LerpUnclamped(restPosition, pressedPosition, Mathf.Clamp01(t));
            }

            if (hitLight != null)
                hitLight.intensity = lightIntensity * (1f - Mathf.Clamp01(elapsed / lightFadeDuration));

            yield return null;
        }

        ResetVisuals();
        feedback = null;
    }

    /// <summary>Snaps the momentary feedback back to rest. The lit colour is kept.</summary>
    private void ResetVisuals()
    {
        if (buttonCap != null)
            buttonCap.localPosition = restPosition;

        if (hitLight != null)
            hitLight.intensity = 0f;
    }

    private void ApplyColor(Color color)
    {
        if (capRenderer == null || colorBlock == null)
            return;

        capRenderer.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, color);
        colorBlock.SetColor(ColorId, color);
        capRenderer.SetPropertyBlock(colorBlock);
    }
}
