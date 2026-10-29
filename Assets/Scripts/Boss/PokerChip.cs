using System;
using UnityEngine;

/// <summary>
/// One of the dealer's chips from the Club phase. A solid disc the ball bounces off. It keeps
/// sliding down the table, slipping sideways around walls, bumpers and other chips, and
/// disappears once it reaches the line just above the flippers. Each ball hit scores; after
/// enough hits the chip breaks early.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PokerChip : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Renderer tinted with the chip colour. Defaults to the first renderer found.")]
    [SerializeField] private Renderer face;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashTime = 0.1f;

    [Header("Sweep")]
    [Tooltip("Half height of the box swept ahead of the chip to find obstacles.")]
    [SerializeField] private float sweepHalfHeight = 0.25f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Raised once when the chip has taken enough hits, just before it is destroyed.</summary>
    public event Action<PokerChip> Broke;

    public float Radius { get; private set; } = 1.5f;

    private Rigidbody body;
    private Collider[] ownColliders;
    private MaterialPropertyBlock colorBlock;
    private Color color = Color.red;
    private float speed;
    private float despawnZ;
    private int slipSide = 1;
    private int hitsLeft = 3;
    private int hitPoints = 2;
    private float flashTimer;
    private bool dead;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        ownColliders = GetComponentsInChildren<Collider>();
        if (face == null)
            face = GetComponentInChildren<Renderer>();
    }

    /// <summary>Sizes and arms the chip. Call right after spawning, before its first FixedUpdate.</summary>
    public void Initialize(float radius, float driftSpeed, float despawnLineZ, int hitsToBreak, int pointsPerHit, Color chipColor)
    {
        Radius = Mathf.Max(0.1f, radius);
        speed = driftSpeed;
        despawnZ = despawnLineZ;
        slipSide = UnityEngine.Random.value < 0.5f ? -1 : 1;
        hitsLeft = Mathf.Max(1, hitsToBreak);
        hitPoints = pointsPerHit;
        color = chipColor;

        // The chip is one unit across at scale 1; keep whatever height the prefab or builder set.
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Radius * 2f, scale.y, Radius * 2f);

        ownColliders = GetComponentsInChildren<Collider>();
        ApplyColor(color);
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
            ApplyColor(color);
    }

    private void FixedUpdate()
    {
        if (dead || speed <= 0f)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        // Reached the line above the flippers: the chip has done its job and goes away.
        if (body.position.z <= despawnZ)
        {
            dead = true;
            Destroy(gameObject);
            return;
        }

        float step = speed * Time.fixedDeltaTime;

        // Straight down first. If something solid is in the way, slip diagonally around it,
        // trying the chip's preferred side and then the other, so the rain keeps flowing.
        Vector3[] directions =
        {
            Vector3.back,
            (Vector3.back + Vector3.right * slipSide).normalized,
            (Vector3.back - Vector3.right * slipSide).normalized,
        };

        foreach (Vector3 direction in directions)
        {
            if (IsBlocked(direction, step))
                continue;

            body.MovePosition(body.position + direction * step);
            return;
        }

        // Boxed in on every side. Swap the preferred side so the next clear gap gets taken.
        slipSide = -slipSide;
    }

    private bool IsBlocked(Vector3 direction, float step)
    {
        Vector3 halfExtents = new Vector3(Radius, sweepHalfHeight, Radius);
        RaycastHit[] hits = Physics.BoxCastAll(body.position, halfExtents, direction, Quaternion.identity, step + 0.1f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (!IsIgnored(hit.collider))
                return true;
        }

        return false;
    }

    private bool IsIgnored(Collider other)
    {
        if (other == null || other.CompareTag("Ball") || other.CompareTag("Board"))
            return true;

        foreach (Collider own in ownColliders)
        {
            if (other == own)
                return true;
        }

        return other.GetComponentInParent<Boss>() != null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (dead || !collision.collider.CompareTag("Ball"))
            return;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(hitPoints);

        hitsLeft--;
        if (hitsLeft > 0)
        {
            flashTimer = flashTime;
            ApplyColor(flashColor);
            return;
        }

        dead = true;
        Broke?.Invoke(this);
        Destroy(gameObject);
    }

    private void ApplyColor(Color value)
    {
        if (face == null)
            return;

        colorBlock ??= new MaterialPropertyBlock();
        face.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, value);
        colorBlock.SetColor(ColorId, value);
        face.SetPropertyBlock(colorBlock);
    }
}
