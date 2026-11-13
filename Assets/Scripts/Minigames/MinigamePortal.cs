using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A round portal on the board. When a ball rolls into it, the board pauses and a minigame
/// scene is loaded on top of it. When the minigame calls <see cref="ReturnToBoard"/>, the
/// minigame scene is unloaded and the ball is spat back out down the board as a mirror image
/// of how it went in: it leaves from the opposite side of the portal, keeps its sideways
/// direction, and has its motion up the table turned back down it.
/// </summary>
/// <remarks>
/// The portal's own axes describe the board: +Z is up the table, +X is across it and +Y is
/// the board's normal. The board stays loaded underneath the minigame, so the score, timer
/// and every other ball are exactly as they were when play resumes. The minigame scene has
/// to be in the build settings.
/// </remarks>
[RequireComponent(typeof(Collider))]
public class MinigamePortal : MonoBehaviour
{
    [Header("Minigame")]
    [Tooltip("Scene loaded on top of the board when a ball enters. Must be in the build settings.")]
    [SerializeField] private string minigameSceneName = "Minigame1";

    [Header("Exit")]
    [Tooltip("Exit speed as a multiple of the speed the ball went in at.")]
    [SerializeField, Min(0f)] private float exitSpeedMultiplier = 1f;
    [Tooltip("Slowest the ball can leave, so a ball that crept in still makes it back into play.")]
    [SerializeField, Min(0f)] private float minExitSpeed = 12f;
    [SerializeField, Min(0f)] private float maxExitSpeed = 50f;
    [Tooltip("Shallowest angle, in degrees below straight across the table, the ball can leave at. Keeps a ball that skimmed in sideways heading down the board.")]
    [SerializeField, Range(0f, 90f)] private float minExitAngle = 20f;
    [Tooltip("Space left between the ball and the portal's trigger when the ball is placed at the exit.")]
    [SerializeField, Min(0f)] private float exitGap = 0.2f;
    [Tooltip("Seconds after spitting a ball out before the portal takes another one.")]
    [SerializeField, Min(0f)] private float reentryCooldown = 0.75f;

    /// <summary>The portal whose minigame is open, or null while the board is in play.</summary>
    public static MinigamePortal Active { get; private set; }

    /// <summary>True while a minigame is open on top of the board. Board input checks this.</summary>
    public static bool IsMinigameOpen => Active != null;

    private Collider trigger;
    private Rigidbody heldBall;
    private Renderer[] hiddenRenderers;
    private CollisionDetectionMode heldDetectionMode;

    // How the ball went in, in the portal's frame (x across the table, z up it).
    private Vector2 entryOffset;
    private Vector2 entryVelocity;
    private float entryHeight;
    private float ballRadius;

    private float resumeTimeScale = 1f;
    private float acceptAfter = float.NegativeInfinity;
    private Scene minigameScene;
    private bool returning;

    // Statics survive between play sessions when domain reload is turned off.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active = null;

    private void Awake()
    {
        trigger = GetComponent<Collider>();
        if (!trigger.isTrigger)
            Debug.LogWarning($"Minigame portal '{name}' needs its collider set to Is Trigger.", this);
    }

    private void OnDestroy()
    {
        // The board is going away with the minigame still up (a restart, say): don't leave the game frozen.
        if (Active == this)
        {
            Active = null;
            Time.timeScale = resumeTimeScale;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsMinigameOpen || Time.time < acceptAfter)
            return;

        if (!other.CompareTag("Ball"))
            return;

        Rigidbody ball = other.attachedRigidbody;
        if (ball == null || ball.isKinematic)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        RecordEntry(ball, other);
        HoldBall(ball);

        Active = this;
        resumeTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        StartCoroutine(LoadMinigame());
    }

    /// <summary>
    /// Closes the open minigame and puts its ball back on the board. Minigames call this when
    /// they're done. Returns false if no minigame was opened from a portal.
    /// </summary>
    public static bool ReturnToBoard()
    {
        if (Active == null)
            return false;

        Active.BeginReturn();
        return true;
    }

    private void RecordEntry(Rigidbody ball, Collider ballCollider)
    {
        Vector3 offset = ball.position - transform.position;
        Vector3 velocity = ball.linearVelocity;

        entryOffset = ToBoard(offset);
        entryVelocity = ToBoard(velocity);
        entryHeight = Vector3.Dot(offset, transform.up);

        Vector3 extents = ballCollider.bounds.extents;
        ballRadius = Mathf.Max(extents.x, extents.z);
    }

    private void HoldBall(Rigidbody ball)
    {
        heldBall = ball;
        heldDetectionMode = ball.collisionDetectionMode;

        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;

        // Kinematic bodies only support speculative CCD, so switch modes first to avoid a warning.
        ball.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        ball.isKinematic = true;
        ball.detectCollisions = false;

        // Tuck the ball into the middle of the portal and hide it until it comes back out.
        Vector3 held = transform.position + transform.up * entryHeight;
        ball.position = held;
        ball.transform.position = held;

        hiddenRenderers = ball.GetComponentsInChildren<Renderer>();
        for (int i = 0; i < hiddenRenderers.Length; i++)
        {
            if (hiddenRenderers[i].enabled)
                hiddenRenderers[i].enabled = false;
            else
                hiddenRenderers[i] = null; // was already off; leave it off on the way out
        }
    }

    private IEnumerator LoadMinigame()
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(minigameSceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError($"Minigame portal '{name}' couldn't load '{minigameSceneName}'. Is it in the build settings?", this);
            ReleaseBall();
            yield break;
        }

        // Async loads keep going while the time scale is 0.
        yield return load;

        // Make it the active scene so anything the minigame spawns lands in it and is unloaded with it.
        minigameScene = SceneManager.GetSceneByName(minigameSceneName);
        if (minigameScene.IsValid())
            SceneManager.SetActiveScene(minigameScene);
    }

    private void BeginReturn()
    {
        if (returning)
            return;

        returning = true;
        StartCoroutine(ReturnFromMinigame());
    }

    private IEnumerator ReturnFromMinigame()
    {
        if (minigameScene.IsValid() && minigameScene.isLoaded)
        {
            SceneManager.SetActiveScene(gameObject.scene);

            AsyncOperation unload = SceneManager.UnloadSceneAsync(minigameScene);
            if (unload != null)
                yield return unload;
        }

        ReleaseBall();
    }

    private void ReleaseBall()
    {
        if (heldBall != null)
        {
            // Leave from the mirror image of the spot the ball went in at: the other side of the
            // portal, and always the half facing down the table.
            Vector2 side = new Vector2(-entryOffset.x, -Mathf.Abs(entryOffset.y));
            side = side.sqrMagnitude > 0.0001f ? side.normalized : Vector2.down;

            float distance = TriggerRadius() + ballRadius + exitGap;
            Vector3 exitPosition = transform.position + ToWorld(side) * distance + transform.up * entryHeight;
            Vector3 exitVelocity = ToWorld(ExitDirection(side)) * ExitSpeed();

            heldBall.position = exitPosition;
            heldBall.transform.position = exitPosition;
            heldBall.isKinematic = false;
            heldBall.collisionDetectionMode = heldDetectionMode;
            heldBall.detectCollisions = true;
            heldBall.linearVelocity = exitVelocity;
            heldBall.angularVelocity = Vector3.zero;

            // Don't draw a streak from the portal's middle to the exit.
            foreach (TrailRenderer trail in heldBall.GetComponentsInChildren<TrailRenderer>())
                trail.Clear();

            if (hiddenRenderers != null)
            {
                foreach (Renderer hidden in hiddenRenderers)
                {
                    if (hidden != null)
                        hidden.enabled = true;
                }
            }
        }

        heldBall = null;
        hiddenRenderers = null;
        returning = false;
        Active = null;

        Time.timeScale = resumeTimeScale;
        acceptAfter = Time.time + reentryCooldown;
    }

    /// <summary>
    /// The entry path mirrored: sideways motion carries on and motion up the table is turned back
    /// down it, so a ball that went in heading up and right comes out heading down and right.
    /// </summary>
    private Vector2 ExitDirection(Vector2 side)
    {
        Vector2 direction = new Vector2(entryVelocity.x, -Mathf.Abs(entryVelocity.y));
        if (direction.sqrMagnitude < 0.0001f)
            direction = side; // the ball crept in, so send it straight out the way it's facing
        direction.Normalize();

        float minDown = Mathf.Sin(minExitAngle * Mathf.Deg2Rad);
        if (-direction.y < minDown)
        {
            float across = Mathf.Cos(minExitAngle * Mathf.Deg2Rad);
            direction = new Vector2(direction.x >= 0f ? across : -across, -minDown);
        }

        return direction;
    }

    private float ExitSpeed()
    {
        float speed = entryVelocity.magnitude * exitSpeedMultiplier;
        return Mathf.Clamp(speed, minExitSpeed, Mathf.Max(minExitSpeed, maxExitSpeed));
    }

    private float TriggerRadius()
    {
        Vector3 extents = trigger.bounds.extents;
        return Mathf.Max(extents.x, extents.z);
    }

    private Vector2 ToBoard(Vector3 world) =>
        new Vector2(Vector3.Dot(world, transform.right), Vector3.Dot(world, transform.forward));

    private Vector3 ToWorld(Vector2 board) =>
        transform.right * board.x + transform.forward * board.y;
}
