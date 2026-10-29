using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class NudgeController : MonoBehaviour
{
    public static NudgeController Instance { get; private set; }

    [Header("Nudge Settings")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeInterval = 0.04f;
    [SerializeField] private float horizontalForce = 3f;

    [Header("Camera Feedback")]
    [SerializeField] private Transform cameraToShake;
    [SerializeField] private float cameraShakeDuration = 0.15f;
    [SerializeField] private float cameraShakeStrength = 0.08f;

    [Header("Tilt Settings")]
    [SerializeField] private float tiltPerNudge = 1f;
    [SerializeField] private float tiltThreshold = 3f;
    [SerializeField] private float tiltDecayPerSecond = 0.5f;
    [SerializeField] private float flipperLockDuration = 3f;
    [SerializeField] private Slider tiltMeter;

    private float currentTilt;
    private bool isTilted;
    private Vector3 cameraRestLocalPosition;
    private Coroutine cameraShakeRoutine;

    void Update()
    {
        if (!isTilted)
        {
            currentTilt = Mathf.Max(0f, currentTilt - tiltDecayPerSecond * Time.deltaTime);
            UpdateTiltMeter();
        }

        if (Keyboard.current == null || BallTypeManager.IsSelecting)
            return;

        if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsChoosing)
            return;

        if (Keyboard.current.leftShiftKey.wasPressedThisFrame)
            Nudge(1f);
        else if (Keyboard.current.rightShiftKey.wasPressedThisFrame)
            Nudge(-1f);
    }

    /// <summary>
    /// Nudges the table exactly as a shift key press does: +1 is left shift (balls pushed right),
    /// -1 is right shift. Builds tilt like a real nudge and locks the flippers past the threshold.
    /// Used by the Bouncer enemy to force a nudge on contact.
    /// </summary>
    public void Nudge(float direction)
    {
        if (isTilted || direction == 0f)
            return;

        direction = Mathf.Sign(direction);
        currentTilt += tiltPerNudge;
        UpdateTiltMeter();
        StartCoroutine(ShakeBalls(direction));
        StartCameraShake();

        if (currentTilt >= tiltThreshold)
            StartCoroutine(TiltFlippers());
    }

    /// <summary>Nudges left or right at random, as if the player had hit one of the shift keys.</summary>
    public void NudgeRandom()
    {
        Nudge(Random.value < 0.5f ? 1f : -1f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Awake()
    {
        Instance = this;

        if (cameraToShake == null && Camera.main != null)
            cameraToShake = Camera.main.transform;

        if (cameraToShake != null)
            cameraRestLocalPosition = cameraToShake.localPosition;

        if (tiltMeter != null)
        {
            tiltMeter.minValue = 0f;
            tiltMeter.maxValue = tiltThreshold;
        }

        UpdateTiltMeter();
    }

    private IEnumerator ShakeBalls(float direction)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            foreach (GameObject ball in GameObject.FindGameObjectsWithTag("Ball"))
            {
                Rigidbody ballBody = ball.GetComponent<Rigidbody>();
                // Shake force taken from shake force manager
                // This will also help us adjust value when we implment items
                if (ballBody != null)
                {
                    float shakeForce = ShakeSettingsManager.Instance != null
                        ? ShakeSettingsManager.Instance.ShakeForce
                        : horizontalForce;
                    float xForce = shakeForce * direction;
                    ballBody.AddForce(Vector3.right * xForce, ForceMode.Impulse);
                }
            }

            yield return new WaitForSeconds(shakeInterval);
            elapsed += shakeInterval;
        }
    }

    private void StartCameraShake()
    {
        if (cameraToShake == null)
            return;

        if (cameraShakeRoutine != null)
        {
            StopCoroutine(cameraShakeRoutine);
            cameraToShake.localPosition = cameraRestLocalPosition;
        }

        cameraShakeRoutine = StartCoroutine(ShakeCamera());
    }

    private IEnumerator ShakeCamera()
    {
        float elapsed = 0f;

        while (elapsed < cameraShakeDuration)
        {
            float strength = cameraShakeStrength;
            Vector2 offset = Random.insideUnitCircle * strength;
            cameraToShake.localPosition = cameraRestLocalPosition + new Vector3(offset.x, offset.y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraToShake.localPosition = cameraRestLocalPosition;
        cameraShakeRoutine = null;
    }

    private IEnumerator TiltFlippers()
    {
        isTilted = true;
        currentTilt = tiltThreshold;
        UpdateTiltMeter();

        foreach (FlipperControls flipper in FindObjectsByType<FlipperControls>(FindObjectsInactive.Include))
            flipper.SetTiltLocked(true);

        yield return new WaitForSeconds(flipperLockDuration);

        foreach (FlipperControls flipper in FindObjectsByType<FlipperControls>(FindObjectsInactive.Include))
            flipper.SetTiltLocked(false);

        currentTilt = 0f;
        isTilted = false;
        UpdateTiltMeter();
    }

    private void UpdateTiltMeter()
    {
        if (tiltMeter != null)
            tiltMeter.SetValueWithoutNotify(Mathf.Clamp(currentTilt, 0f, tiltThreshold));
    }
}
