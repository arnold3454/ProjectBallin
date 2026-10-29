using System;
using UnityEngine;

/// <summary>
/// One of the dealer's chips from the Club phase. A solid disc the ball bounces off. It slides
/// down the table until something solid is in the way (a wall, a bumper, another chip) or it
/// reaches its rest line near the gutter, and it carries on sliding if the way clears again.
/// Each ball hit scores; after enough hits the chip breaks.
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
    /// <summary>True once the chip has reached its rest line and stopped for good.</summary>
    public bool IsSettled { get; private set; }

    private Rigidbody body;
    private Collider[] ownColliders;
    private MaterialPropertyBlock colorBlock;
    private Color color = Color.red;
    private float speed;
    private float restZ;
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
    public void Initialize(float radius, float driftSpeed, float restLineZ, int hitsToBreak, int pointsPerHit, Color chipColor)
    {
        Radius = Mathf.Max(0.1f, radius);
        speed = driftSpeed;
        restZ = restLineZ;
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
        if (dead || IsSettled || speed <= 0f)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        if (body.position.z <= restZ)
        {
            IsSettled = true;
            return;
        }

        float step = Mathf.Min(speed * Time.fixedDeltaTime, body.position.z - restZ);
        Vector3 halfExtents = new Vector3(Radius, sweepHalfHeight, Radius);

        RaycastHit[] hits = Physics.BoxCastAll(body.position, halfExtents, Vector3.back, Quaternion.identity, step + 0.1f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            // Something solid is in the way. Wait here; the way may clear if it was a chip that broke.
            if (!IsIgnored(hit.collider))
                return;
        }

        body.MovePosition(body.position + Vector3.back * step);
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
