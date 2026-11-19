using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BallHitboxTrigger))]
public class CapsulePickup : MonoBehaviour
{
    [Header("Scoring")]
    [SerializeField, Min(0)] private int points = 5;
    [SerializeField, Min(0f)] private float respawnDelay = 5f;

    [Header("Animation")]
    [Tooltip("Optional mesh transform. Assign a child to keep the pickup collider stationary.")]
    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float spinSpeed = 90f;
    [SerializeField, Min(0f)] private float bounceHeight = 0.2f;
    [SerializeField, Min(0f)] private float bounceFrequency = 1.5f;

    private BallHitboxTrigger hitbox;
    private Collider[] pickupColliders;
    private Renderer[] pickupRenderers;
    private bool[] colliderEnabledStates;
    private bool[] rendererEnabledStates;
    private Vector3 initialLocalPosition;
    private bool collected;

    private Transform AnimatedTransform => visual != null ? visual : transform;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();
        initialLocalPosition = AnimatedTransform.localPosition;

        pickupColliders = GetComponentsInChildren<Collider>(true);
        pickupRenderers = GetComponentsInChildren<Renderer>(true);
        colliderEnabledStates = new bool[pickupColliders.Length];
        rendererEnabledStates = new bool[pickupRenderers.Length];

        for (int i = 0; i < pickupColliders.Length; i++)
            colliderEnabledStates[i] = pickupColliders[i].enabled;

        for (int i = 0; i < pickupRenderers.Length; i++)
            rendererEnabledStates[i] = pickupRenderers[i].enabled;
    }

    private void OnEnable()
    {
        if (hitbox == null)
            hitbox = GetComponent<BallHitboxTrigger>();

        hitbox.BallHit += OnBallHit;
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.BallHit -= OnBallHit;
    }

    private void Update()
    {
        if (collected)
            return;

        Transform target = AnimatedTransform;
        target.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        Vector3 position = initialLocalPosition;
        position.y += Mathf.Sin(Time.time * bounceFrequency * Mathf.PI * 2f) * bounceHeight;
        target.localPosition = position;
    }

    private void OnBallHit(Collider ball)
    {
        if (collected)
            return;

        collected = true;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(points);

        SetPickupActive(false);
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        hitbox.ResetTrigger();
        collected = false;
        SetPickupActive(true);
    }

    private void SetPickupActive(bool active)
    {
        for (int i = 0; i < pickupColliders.Length; i++)
            pickupColliders[i].enabled = active && colliderEnabledStates[i];

        for (int i = 0; i < pickupRenderers.Length; i++)
            pickupRenderers[i].enabled = active && rendererEnabledStates[i];
    }
}
