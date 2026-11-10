using UnityEngine;

/// <summary>
/// A fanned stack of cards that sweeps across the table and blows a gust ahead of itself.
/// Any ball inside the gust zone is pushed in the gust direction every physics step.
/// Launched by the Diamond phase, which sets its path, reach and force.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class CardFan : MonoBehaviour
{
    [Header("Gust")]
    [Tooltip("Trigger volume the gust fills. Resized on launch to match the reach.")]
    [SerializeField] private BoxCollider gustZone;
    [Tooltip("Optional flat visual stretched along the gust so the player can see its reach.")]
    [SerializeField] private Transform gustVisual;
    [SerializeField, Min(0.1f)] private float gustHeight = 2f;

    [Header("Audio")]
    [Tooltip("Played once when the fan starts its sweep.")]
    [SerializeField] private AudioClip wooshClip;
    [SerializeField, Range(0f, 1f)] private float wooshVolume = 1f;

    private Rigidbody body;
    private AudioSource audioSource;
    private Vector3 travelDirection;
    private Vector3 pushDirection;
    private float speed;
    private float force;
    private float distanceLeft;
    private bool launched;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (gustZone == null)
            gustZone = GetComponentInChildren<BoxCollider>();
        if (gustZone != null)
            gustZone.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Starts the sweep. The fan faces <paramref name="pushDir"/> and moves along
    /// <paramref name="travelDir"/> for <paramref name="distance"/> units, then disappears.
    /// </summary>
    public void Launch(Vector3 travelDir, float distance, float travelSpeed, Vector3 pushDir, float pushForce, float range, float width)
    {
        travelDirection = travelDir.normalized;
        pushDirection = pushDir.normalized;
        distanceLeft = distance;
        speed = travelSpeed;
        force = pushForce;
        launched = true;

        transform.rotation = Quaternion.LookRotation(pushDirection, Vector3.up);

        if (gustZone != null)
        {
            gustZone.size = new Vector3(width, gustHeight, range);
            gustZone.center = new Vector3(0f, gustHeight * 0.5f, range * 0.5f);
        }

        if (gustVisual != null)
        {
            gustVisual.localScale = new Vector3(width, gustVisual.localScale.y, range);
            gustVisual.localPosition = new Vector3(0f, gustVisual.localPosition.y, range * 0.5f);
        }

        if (wooshClip != null && audioSource != null)
            audioSource.PlayOneShot(wooshClip, wooshVolume);
    }

    private void FixedUpdate()
    {
        if (!launched)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        float step = speed * Time.fixedDeltaTime;
        body.MovePosition(body.position + travelDirection * step);
        distanceLeft -= step;

        if (distanceLeft <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!launched || !other.CompareTag("Ball"))
            return;

        Rigidbody ball = other.attachedRigidbody;
        if (ball == null)
            return;

        ball.AddForce(pushDirection * force, ForceMode.Force);
    }
}
