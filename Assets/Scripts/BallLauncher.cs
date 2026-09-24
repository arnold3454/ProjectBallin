using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Plunger-style ball launcher. A ball is loaded into the launch lane and held
/// there until the player charges the plunger (hold space) and releases it.
/// </summary>
public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;
    [Tooltip("Optional. Only used when 'Use Spawn Point' is ticked - otherwise the load position is taken from the tip of this launcher's collider.")]
    public Transform spawnPoint;
    [Tooltip("Tick to load balls at the Spawn transform instead of the launcher tip.")]
    [SerializeField] private bool useSpawnPoint;
    [Tooltip("Optional. The piece that visually slides back while charging. Defaults to this object.")]
    [SerializeField] private Transform plungerVisual;

    [Header("Launch")]
    [Tooltip("World-space direction the ball is fired in. +Z is up the table.")]
    [SerializeField] private Vector3 launchDirection = Vector3.forward;
    [SerializeField] private float minLaunchSpeed = 18f;
    [SerializeField] private float maxLaunchSpeed = 45f;
    [Tooltip("Seconds of holding space needed to reach full power.")]
    [SerializeField] private float chargeTime = 0.8f;
    [Tooltip("Gap left between the plunger tip and the ball when it is loaded.")]
    [SerializeField] private float loadGap = 0.05f;

    [Header("Plunger Feel")]
    [SerializeField] private float plungerPullDistance = 0.9f;
    [SerializeField] private float plungerReturnSpeed = 25f;

    [Header("Reloading")]
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private bool autoReload = true;
    [Tooltip("Wait for every ball to drain before the next one is loaded.")]
    [SerializeField] private bool waitForDrain = true;
    [SerializeField] private float reloadDelay = 0.5f;
    [Tooltip("Let the player press space to load a ball immediately, even with one still in play.")]
    [SerializeField] private bool allowManualReload = true;

    private Rigidbody loadedBall;
    private Collider launcherCollider;
    private Vector3 plungerRestLocalPosition;
    private float charge;
    private bool isCharging;
    private float reloadTimer;
    private bool ignoreHold;

    /// <summary>0-1 charge of the plunger, for UI or audio hooks.</summary>
    public float ChargeAmount => charge;
    public bool HasBallLoaded => loadedBall != null;

    private void Awake()
    {
        launcherCollider = GetComponent<Collider>();

        if (plungerVisual == null)
            plungerVisual = transform;

        plungerRestLocalPosition = plungerVisual.localPosition;
    }

    private void Start()
    {
        if (loadOnStart)
            LoadBall();
    }

    private void Update()
    {
        // No new balls once the clock has run out.
        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
        {
            isCharging = false;
            charge = 0f;
            UpdatePlungerVisual();
            return;
        }

        HandleInput();
        HandleReload();
        HoldLoadedBall();
        UpdatePlungerVisual();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null)
            return;

        bool pressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        bool held = Keyboard.current.spaceKey.isPressed;
        bool released = Keyboard.current.spaceKey.wasReleasedThisFrame;

        if (!held)
            ignoreHold = false;

        if (loadedBall == null)
        {
            // Nothing in the lane yet - a tap racks the next ball up. That press
            // only loads, so the player still gets to charge before firing.
            if (pressed && allowManualReload)
            {
                LoadBall();
                ignoreHold = true;
            }

            isCharging = false;
            charge = 0f;
            return;
        }

        if (held && !ignoreHold)
        {
            if (!isCharging)
            {
                isCharging = true;
                charge = 0f;
            }

            charge = chargeTime > 0f ? Mathf.Min(1f, charge + Time.deltaTime / chargeTime) : 1f;
        }

        if (isCharging && released)
            Launch();
    }

    private void HandleReload()
    {
        if (loadedBall != null || !autoReload)
        {
            reloadTimer = 0f;
            return;
        }

        if (waitForDrain && GameObject.FindGameObjectsWithTag("Ball").Length > 0)
        {
            reloadTimer = 0f;
            return;
        }

        reloadTimer += Time.deltaTime;
        if (reloadTimer >= reloadDelay)
            LoadBall();
    }

    /// <summary>Spawns a ball in the launch lane and parks it against the plunger.</summary>
    public void LoadBall()
    {
        if (ballPrefab == null || loadedBall != null)
            return;

        float size = BallSettingsManager.Instance != null ? BallSettingsManager.Instance.CurrentSize : 1f;

        GameObject newBall = Instantiate(ballPrefab, GetLoadPosition(size), transform.rotation);
        newBall.transform.localScale = Vector3.one * size;

        Rigidbody body = newBall.GetComponent<Rigidbody>();
        if (body == null)
        {
            Debug.LogError("Ball prefab has no Rigidbody, so it cannot be launched.", this);
            Destroy(newBall);
            autoReload = false;
            return;
        }

        if (BallSettingsManager.Instance != null)
            body.mass = BallSettingsManager.Instance.CurrentWeight;

        // Hold the ball still until the plunger fires it.
        body.isKinematic = true;
        loadedBall = body;

        charge = 0f;
        isCharging = false;
        reloadTimer = 0f;
    }

    /// <summary>Fires the loaded ball up the lane at the charged power.</summary>
    public void Launch()
    {
        isCharging = false;

        if (loadedBall == null)
            return;

        Rigidbody body = loadedBall;
        loadedBall = null;

        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.AddForce(GetLaunchDirection() * Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, charge), ForceMode.VelocityChange);

        charge = 0f;
    }

    private void HoldLoadedBall()
    {
        if (loadedBall == null)
            return;

        // Keep the parked ball glued to the plunger face so it follows the pull-back.
        float size = loadedBall.transform.localScale.x;
        loadedBall.position = GetLoadPosition(size) - GetLaunchDirection() * (plungerPullDistance * charge);
    }

    private void UpdatePlungerVisual()
    {
        if (plungerVisual == null)
            return;

        Vector3 target = plungerRestLocalPosition;

        if (charge > 0f)
        {
            Vector3 worldPull = -GetLaunchDirection() * (plungerPullDistance * charge);
            target += plungerVisual.parent != null
                ? plungerVisual.parent.InverseTransformVector(worldPull)
                : worldPull;
        }

        plungerVisual.localPosition = isCharging
            ? target
            : Vector3.MoveTowards(plungerVisual.localPosition, target, plungerReturnSpeed * Time.deltaTime);
    }

    private Vector3 GetLaunchDirection()
    {
        return launchDirection.sqrMagnitude < 0.0001f ? Vector3.forward : launchDirection.normalized;
    }

    /// <summary>Resting spot for a ball sitting against the plunger face.</summary>
    private Vector3 GetLoadPosition(float ballSize)
    {
        Vector3 direction = GetLaunchDirection();
        float ballRadius = 0.5f * ballSize;

        if (useSpawnPoint && spawnPoint != null)
            return spawnPoint.position;

        Vector3 origin = plungerRestLocalPosition;
        origin = plungerVisual != null && plungerVisual.parent != null
            ? plungerVisual.parent.TransformPoint(origin)
            : origin;

        // Start at the launcher's own tip so the ball sits in the lane no matter
        // how the map is scaled, then step forward by the ball's radius.
        float reach = 0f;
        if (launcherCollider != null)
        {
            Vector3 extents = launcherCollider.bounds.extents;
            reach = Mathf.Abs(direction.x) * extents.x
                  + Mathf.Abs(direction.y) * extents.y
                  + Mathf.Abs(direction.z) * extents.z;
        }

        Vector3 loadPosition = origin + direction * (reach + ballRadius + loadGap);

        // Bigger balls rest higher off the board than the default one does.
        loadPosition.y += ballRadius - 0.5f;

        return loadPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            launcherCollider = GetComponent<Collider>();
            if (plungerVisual == null) plungerVisual = transform;
            plungerRestLocalPosition = plungerVisual.localPosition;
        }

        Vector3 loadPosition = GetLoadPosition(1f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(loadPosition, 0.5f);
        Gizmos.DrawLine(loadPosition, loadPosition + GetLaunchDirection() * 3f);
    }
}
