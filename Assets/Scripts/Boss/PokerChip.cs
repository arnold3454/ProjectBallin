using UnityEngine;

/// <summary>
/// One of the dealer's chips from the Club phase. Purely cosmetic: a big disc that floats above
/// the table and slides down it, hiding whatever is underneath. It has no collider or rigidbody,
/// so it never touches the ball, the flippers or anything else. It disappears once it reaches
/// the line just above the flippers.
/// </summary>
public class PokerChip : MonoBehaviour
{
    [Header("Visuals")]
    [Tooltip("Renderer tinted with the chip colour. Defaults to the first renderer found.")]
    [SerializeField] private Renderer face;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public float Radius { get; private set; } = 1.5f;

    private MaterialPropertyBlock colorBlock;
    private float speed;
    private float despawnZ;

    private void Awake()
    {
        if (face == null)
            face = GetComponentInChildren<Renderer>();

        // Safety net: the chip must never be part of the physics world, whatever the prefab holds.
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            Destroy(collider);
        foreach (Rigidbody body in GetComponentsInChildren<Rigidbody>(true))
            Destroy(body);
    }

    /// <summary>Sizes and arms the chip. Call right after spawning.</summary>
    public void Initialize(float radius, float driftSpeed, float despawnLineZ, Color chipColor)
    {
        Radius = Mathf.Max(0.1f, radius);
        speed = driftSpeed;
        despawnZ = despawnLineZ;

        // The chip is one unit across at scale 1; keep whatever thickness the prefab set.
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Radius * 2f, scale.y, Radius * 2f);

        ApplyColor(chipColor);
    }

    private void Update()
    {
        if (speed <= 0f)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        // Reached the line above the flippers: the chip has done its job and goes away.
        if (transform.position.z <= despawnZ)
        {
            Destroy(gameObject);
            return;
        }

        // Straight down the table. Nothing can block a cosmetic chip, so no obstacle checks.
        transform.position += Vector3.back * (speed * Time.deltaTime);
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
