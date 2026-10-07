using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Plunger-style ball launcher. A ball is loaded into the launch lane and held
/// there until the player charges the plunger (hold space) and releases it.
/// It can also fire balls on its own for items (see QueueAutoLaunch).
/// </summary>
public class BallLauncher : MonoBehaviour
{
    [Header("References")]
    public GameObject ballPrefab;
    [Tooltip("Optional. Only used when 'Use Spawn Point' is ticked; otherwise the load position is taken from the tip of this launcher's collider.")]
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

    [Header("Audio")]
    [Tooltip("Played once when the player begins charging the plunger (pull-back).")]
    [SerializeField] private AudioClip pullBackClip;
    [Tooltip("Played when the plunger fires and the ball is launched.")]
    [SerializeField] private AudioClip releaseClip;
    [SerializeField, Range(0f, 1f)] private float pullBackVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float releaseVolume = 1f;

    [Header("Auto Launch")]
    [Tooltip("Plunger charge (0-1) used when the launcher fires a ball on its own, such as for the Launch Ball item.")]
    [SerializeField, Range(0f, 1f)] private float autoLaunchCharge = 0.85f;
    [Tooltip("Pause between automatic shots. The wait starts once the previous ball has left the lane.")]
    [SerializeField] private float autoLaunchDelay = 0.5f;
    [Tooltip("How far up the lane, measured from the plunger, to check for balls before an automatic shot so it doesn't hit them. Drawn in yellow when the launcher is selected.")]
    [SerializeField] private float autoLaunchLaneLength = 35f;

    private AudioSource audioSource;
    private Rigidbody loadedBall;
    private Collider launcherCollider;
    private Vector3 plungerRestLocalPosition;
    private float charge;
    private bool isCharging;
    private float reloadTimer;
    private bool ignoreHold;
    private int pendingAutoLaunches;
    private float autoLaunchTimer;

    /// <summary>0-1 charge of the plunger, for UI or audio hooks.</summary>
    public float ChargeAmount => charge;
    public bool HasBallLoaded => loadedBall != null;

    private void Awake()
    {
        launcherCollider = GetComponent<Collider>();

        if (plungerVisual == null)
            plungerVisual = transform;

        plungerRestLocalPosition = plungerVisual.localPosition;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
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

        // The upgrade popup has the screen; keep the plunger from reacting to its key presses.
        if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsChoosing)
            return;

        // The start screen has the screen; keep the plunger from reacting to its key presses.
        if (BallTypeManager.IsSelecting)
            return;

        HandleInput();
        HandleAutoLaunch();
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
            // No ball is held on the plunger, so a tap racks one up. That press
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
                PlayPullBackSFX();
            }

            charge = chargeTime > 0f ? Mathf.Min(1f, charge + Time.deltaTime / chargeTime) : 1f;
        }

        if (isCharging && released)
            Launch();
    }

    private void HandleReload()
    {
        // Don't rack the player's next ball while extra balls are still waiting to be fired.
        if (loadedBall != null || !autoReload || pendingAutoLaunches > 0)
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

    private void HandleAutoLaunch()
    {
        if (pendingAutoLaunches <= 0)
            return;

        // Wait for the last ball to make it out of the lane (or roll back down onto
        // the plunger) so the two don't collide on the way up.
        float size = CurrentBallSize;
        if (IsLaneBusy(size))
        {
            autoLaunchTimer = autoLaunchDelay;
            return;
        }

        autoLaunchTimer -= Time.deltaTime;
        if (autoLaunchTimer > 0f)
            return;

        // A ball already on the plunger (the player's parked ball, or one that
        // rolled back down) is fired first. It isn't an extra ball, so the extra
        // one still follows on the next shot.
        bool plungerClear = loadedBall == null && FindBallAt(GetLoadPosition(size), 0.5f * size) == null;

        LoadBall();
        if (loadedBall == null)
            return;

        charge = autoLaunchCharge;
        Launch();
        autoLaunchTimer = autoLaunchDelay;

        if (plungerClear)
            pendingAutoLaunches--;
    }

    /// <summary>Parks a ball against the plunger, spawning a new one unless a loose ball is already resting there.</summary>
    public void LoadBall()
    {
        if (ballPrefab == null || loadedBall != null)
            return;

        float size = CurrentBallSize;
        Vector3 loadPosition = GetLoadPosition(size);

        // A weak shot can roll back down and come to rest on the plunger. Rack
        // that ball up again rather than spawning a new one on top of it.
        Rigidbody body = FindBallAt(loadPosition, 0.5f * size);
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            GameObject newBall = Instantiate(ballPrefab, loadPosition, transform.rotation);
            newBall.transform.localScale = Vector3.one * size;
            BallTypeManager.Apply(newBall);

            body = newBall.GetComponent<Rigidbody>();
            if (body == null)
            {
                Debug.LogError("Ball prefab has no Rigidbody, so it cannot be launched.", this);
                Destroy(newBall);
                autoReload = false;
                return;
            }

            if (BallSettingsManager.Instance != null)
                body.mass = BallSettingsManager.Instance.EffectiveWeight;
        }

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
        float speedMultiplier = 1f;
        if (UpgradeManager.Instance != null)
            speedMultiplier *= UpgradeManager.Instance.BallSpeedMultiplier;
        speedMultiplier *= BallTypeManager.SpeedMultiplier;
        body.AddForce(GetLaunchDirection() * Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, charge) * speedMultiplier, ForceMode.VelocityChange);

        PlayReleaseSFX();

        charge = 0f;
    }

    private void PlayPullBackSFX()
    {
        if (pullBackClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(pullBackClip, pullBackVolume);
    }

    private void PlayReleaseSFX()
    {
        if (releaseClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(releaseClip, releaseVolume);
    }

    /// <summary>
    /// Makes the launcher fire extra balls into play on its own. A ball already
    /// waiting on the plunger is fired first, then the extra balls follow.
    /// </summary>
    public void QueueAutoLaunch(int count = 1)
    {
        // Start a new batch without waiting out the delay left over from the last one.
        if (pendingAutoLaunches <= 0)
            autoLaunchTimer = 0f;

        pendingAutoLaunches += Mathf.Max(0, count);
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

    private static float CurrentBallSize => BallSettingsManager.Instance != null ? BallSettingsManager.Instance.CurrentSize : 1f;

    /// <summary>A loose ball overlapping the given spot, or null if the spot is clear.</summary>
    private Rigidbody FindBallAt(Vector3 position, float radius)
    {
        return FirstLooseBall(Physics.OverlapSphere(position, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore));
    }

    /// <summary>True while a ball is moving up the lane or rolling back down it.</summary>
    private bool IsLaneBusy(float ballSize)
    {
        Vector3 direction = GetLaunchDirection();
        Vector3 loadPosition = GetLoadPosition(ballSize);
        float radius = 0.5f * ballSize;

        // Start one ball-width up from the plunger so a ball sitting on it doesn't count.
        Vector3 start = loadPosition + direction * (2f * radius + loadGap);
        Vector3 end = loadPosition + direction * Mathf.Max(autoLaunchLaneLength, 2f * radius + loadGap);

        return FirstLooseBall(Physics.OverlapCapsule(start, end, radius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) != null;
    }

    /// <summary>The first ball among the hits that isn't held by the plunger, or null.</summary>
    private Rigidbody FirstLooseBall(Collider[] hits)
    {
        foreach (Collider hit in hits)
        {
            Rigidbody body = hit.attachedRigidbody;
            if (body != null && body != loadedBall && !body.isKinematic && body.CompareTag("Ball"))
                return body;
        }

        return null;
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(loadPosition, loadPosition + GetLaunchDirection() * autoLaunchLaneLength);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(loadPosition, 0.5f);
        Gizmos.DrawLine(loadPosition, loadPosition + GetLaunchDirection() * 3f);
    }
}