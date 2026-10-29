using UnityEngine;

/// <summary>
/// One card of the dealer's Spade shield. Stands as a wall until launched, then slides in its
/// outward direction until it meets anything that is not the ball, another card or the boss.
/// A joker card keeps ricocheting off whatever it hits for a while instead of stopping.
/// One ball hit destroys the card and scores.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShieldCard : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField] private int hitPoints = 2;

    [Header("Visuals")]
    [SerializeField] private Renderer face;
    [SerializeField] private Renderer pip;
    [SerializeField] private Color jokerFaceColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color jokerPipColor = new Color(0.6f, 0.1f, 0.8f);

    [Header("Sweep")]
    [Tooltip("Half size of the box swept ahead of the card to find obstacles.")]
    [SerializeField] private Vector3 sweepHalfExtents = new Vector3(0.7f, 0.8f, 0.08f);
    [SerializeField] private float sweepHeight = 0.9f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public bool IsJoker { get; private set; }

    private Rigidbody body;
    private MaterialPropertyBlock colorBlock;
    private Vector3 moveDirection = Vector3.forward;
    private float speed;
    private float bounceTimeLeft;
    private bool launched;
    private bool dead;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void SetOutwardDirection(Vector3 direction)
    {
        moveDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        if (moveDirection.sqrMagnitude < 0.0001f)
            moveDirection = Vector3.forward;
    }

    /// <summary>Turns this card gold with a purple pip so the player can see the joker.</summary>
    public void ShowJoker()
    {
        IsJoker = true;
        Apply(face, jokerFaceColor);
        Apply(pip, jokerPipColor);
    }

    /// <summary>
    /// Starts the card sliding outward. With <paramref name="bounceTime"/> above zero it
    /// ricochets off obstacles for that long instead of stopping at the first one.
    /// </summary>
    public void Launch(float moveSpeed, float bounceTime)
    {
        speed = moveSpeed;
        bounceTimeLeft = bounceTime;
        launched = true;
    }

    private void FixedUpdate()
    {
        if (!launched || dead)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        float step = speed * Time.fixedDeltaTime;
        Vector3 origin = body.position + Vector3.up * sweepHeight;

        RaycastHit[] hits = Physics.BoxCastAll(origin, sweepHalfExtents, moveDirection, body.rotation, step + 0.15f, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (IsIgnored(hit.collider))
                continue;

            if (bounceTimeLeft > 0f)
            {
                Vector3 normal = Vector3.ProjectOnPlane(hit.normal, Vector3.up);
                if (normal.sqrMagnitude < 0.0001f)
                    normal = -moveDirection;
                moveDirection = Vector3.Reflect(moveDirection, normal.normalized).normalized;
                body.MoveRotation(Quaternion.LookRotation(moveDirection, Vector3.up));
                return;
            }

            Destroy(gameObject);
            return;
        }

        body.MovePosition(body.position + moveDirection * step);

        if (bounceTimeLeft > 0f)
        {
            bounceTimeLeft -= Time.fixedDeltaTime;
            if (bounceTimeLeft <= 0f)
                Destroy(gameObject);
        }
    }

    private bool IsIgnored(Collider other)
    {
        if (other == null || other.CompareTag("Ball"))
            return true;

        if (other.GetComponentInParent<ShieldCard>() != null)
            return true;

        return other.GetComponentInParent<Boss>() != null;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (dead || !collision.collider.CompareTag("Ball"))
            return;

        dead = true;
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(hitPoints);

        Destroy(gameObject);
    }

    private void Apply(Renderer target, Color color)
    {
        if (target == null)
            return;

        colorBlock ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, color);
        colorBlock.SetColor(ColorId, color);
        target.SetPropertyBlock(colorBlock);
    }
}
