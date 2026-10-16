using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// The half red, half black wheel in the middle of the table. Rolling the ball over it
/// asks the RouletteManager to spin; the manager decides the result and this component
/// animates the disc so the winning colour stops under the pointer.
/// </summary>
[RequireComponent(typeof(BallHitboxTrigger))]
public class RouletteWheel : MonoBehaviour
{
    [Header("Disc")]
    [Tooltip("The flat disc that spins. Its authored rotation is the rest pose.")]
    [SerializeField] private Transform disc;
    [Tooltip("Disc angle (degrees around its up axis) that puts the centre of the red half under the pointer.")]
    [SerializeField] private float redUnderPointerAngle = 90f;
    [Tooltip("How far from the centre of a half the disc may stop, so landings look varied.")]
    [SerializeField, Range(0f, 85f)] private float landingSpread = 70f;

    [Header("Spin")]
    [SerializeField, Min(0.1f)] private float spinDuration = 3f;
    [SerializeField, Min(1)] private int minTurns = 3;
    [SerializeField, Min(1)] private int maxTurns = 5;

    [Header("Feedback")]
    [Tooltip("Lamp that shows the current bet colour, and gold once the boss fight is triggered.")]
    [SerializeField] private Renderer betLamp;
    [SerializeField] private Color noBetColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color redBetColor = new Color(1f, 0.1f, 0.1f);
    [SerializeField] private Color blackBetColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.2f);
    [Tooltip("Optional light that glows while spinning and flashes on the result.")]
    [SerializeField] private Light spinLight;
    [SerializeField, Min(0f)] private float spinLightIntensity = 4f;
    [SerializeField, Min(0f)] private float resultFlashIntensity = 12f;
    [SerializeField, Min(0.01f)] private float resultFlashDuration = 0.6f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Raised with the colour that stopped under the pointer once a spin ends.</summary>
    public event Action<BetColor> SpinFinished;

    public bool IsSpinning { get; private set; }
    public bool IsLocked { get; private set; }

    private BallHitboxTrigger hitbox;
    private MaterialPropertyBlock colorBlock;
    private Coroutine spin;
    private Quaternion discRestRotation = Quaternion.identity;
    private float discAngle;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();
        colorBlock = new MaterialPropertyBlock();

        if (disc != null)
            discRestRotation = disc.localRotation;

        if (spinLight != null)
            spinLight.intensity = 0f;

        ShowBet(BetColor.None);
    }

    private void OnEnable()
    {
        if (hitbox == null)
            hitbox = GetComponent<BallHitboxTrigger>();

        hitbox.BallHit += OnBallHit;

        if (RouletteManager.Instance != null)
            RouletteManager.Instance.RegisterWheel(this);
    }

    private void Start()
    {
        // The manager may not have existed yet in OnEnable.
        if (RouletteManager.Instance != null)
            RouletteManager.Instance.RegisterWheel(this);
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.BallHit -= OnBallHit;

        if (RouletteManager.Instance != null)
            RouletteManager.Instance.UnregisterWheel(this);

        if (spin != null)
        {
            StopCoroutine(spin);
            spin = null;
            IsSpinning = false;
        }

        if (spinLight != null)
            spinLight.intensity = 0f;
    }

    /// <summary>Spins the disc so <paramref name="result"/> ends up under the pointer.</summary>
    public void Spin(BetColor result)
    {
        if (IsSpinning || IsLocked || result == BetColor.None)
            return;

        if (spin != null)
            StopCoroutine(spin);

        spin = StartCoroutine(SpinTo(result));
    }

    /// <summary>Shows the bet lamp in the colour of the current bet.</summary>
    public void ShowBet(BetColor bet)
    {
        if (IsLocked)
            return;

        switch (bet)
        {
            case BetColor.Red: ApplyLampColor(redBetColor); break;
            case BetColor.Black: ApplyLampColor(blackBetColor); break;
            default: ApplyLampColor(noBetColor); break;
        }
    }

    /// <summary>Marks the wheel as used up. It no longer spins and its lamp turns gold.</summary>
    public void Lock()
    {
        IsLocked = true;
        ApplyLampColor(lockedColor);
    }

    private void OnBallHit(Collider ball)
    {
        if (RouletteManager.Instance == null)
        {
            Debug.LogWarning($"Roulette wheel '{name}' needs a RouletteManager in the scene.", this);
            return;
        }

        RouletteManager.Instance.WheelHit(this);
    }

    private IEnumerator SpinTo(BetColor result)
    {
        IsSpinning = true;

        float target = result == BetColor.Red ? redUnderPointerAngle : redUnderPointerAngle + 180f;
        target += UnityEngine.Random.Range(-landingSpread, landingSpread);
        int turns = UnityEngine.Random.Range(minTurns, maxTurns + 1);
        float start = discAngle;
        float end = start + 360f * turns + Mathf.Repeat(target - start, 360f);

        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spinDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            SetDiscAngle(Mathf.Lerp(start, end, eased));

            if (spinLight != null)
                spinLight.intensity = spinLightIntensity * (1f - t);

            yield return null;
        }

        SetDiscAngle(end);
        IsSpinning = false;
        spin = null;

        SpinFinished?.Invoke(result);
        StartCoroutine(FlashResult());
    }

    private IEnumerator FlashResult()
    {
        if (spinLight == null)
            yield break;

        float elapsed = 0f;
        while (elapsed < resultFlashDuration)
        {
            elapsed += Time.deltaTime;
            spinLight.intensity = resultFlashIntensity * (1f - Mathf.Clamp01(elapsed / resultFlashDuration));
            yield return null;
        }

        spinLight.intensity = 0f;
    }

    private void SetDiscAngle(float angle)
    {
        discAngle = angle;
        if (disc != null)
            disc.localRotation = Quaternion.AngleAxis(angle, Vector3.up) * discRestRotation;
    }

    private void ApplyLampColor(Color color)
    {
        if (betLamp == null)
            return;

        if (colorBlock == null)
            colorBlock = new MaterialPropertyBlock();

        betLamp.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, color);
        colorBlock.SetColor(ColorId, color);
        betLamp.SetPropertyBlock(colorBlock);
    }
}
