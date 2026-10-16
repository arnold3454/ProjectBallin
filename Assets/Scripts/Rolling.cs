using UnityEngine;

public class Rolling : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Looping roll sound, played while the ball is moving on the board.")]
    [SerializeField] private AudioClip rollClip;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.5f;

    [Header("Roll Settings")]
    [SerializeField] private string boardTag = "Board";
    [Tooltip("Minimum speed before the roll sound starts.")]
    [SerializeField] private float minRollSpeed = 0.5f;
    [Tooltip("Speed at which the roll sound reaches max volume.")]
    [SerializeField] private float maxRollSpeed = 10f;
    [Tooltip("How quickly volume ramps up/down as speed changes.")]
    [SerializeField] private float volumeSmoothing = 8f;

    private AudioSource audioSource;
    private Rigidbody rb;
    private int boardContactCount;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.clip = rollClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(boardTag))
        {
            boardContactCount++;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag(boardTag))
        {
            boardContactCount = Mathf.Max(0, boardContactCount - 1);
        }
    }

    private void Update()
    {
        if (rollClip == null)
            return;

        float speed = rb.linearVelocity.magnitude;
        bool onBoard = boardContactCount > 0;
        bool shouldRoll = onBoard && speed >= minRollSpeed;

        // Start/stop the loop as contact/speed conditions change.
        if (shouldRoll && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (!shouldRoll && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        // Smoothly scale volume with speed so it doesn't snap on/off abruptly.
        float targetVolume = shouldRoll
            ? Mathf.Lerp(0f, maxVolume, Mathf.InverseLerp(minRollSpeed, maxRollSpeed, speed))
            : 0f;

        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * volumeSmoothing);
    }
}