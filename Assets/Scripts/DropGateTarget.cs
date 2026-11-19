using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BallHitboxTrigger))]
public class DropGateTarget : MonoBehaviour
{
    [SerializeField] private DropGatePair pair;

    [Header("Movement")]
    [Tooltip("The transform moved up and down. Usually the target's moving root.")]
    [SerializeField] private Transform movingPart;
    [Tooltip("How far the target moves downward, in world units.")]
    [SerializeField, Min(0f)] private float dropDistance = 4.5f;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.35f;

    private BallHitboxTrigger hitbox;
    private Coroutine movement;
    private Vector3 raisedLocalPosition;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();

        if (movingPart == null)
            movingPart = transform;

        raisedLocalPosition = movingPart.localPosition;
    }

    private void OnEnable()
    {
        hitbox.BallHit += OnBallHit;
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.BallHit -= OnBallHit;

        StopMovement();
    }

    public void Raise(bool instantly = false)
    {
        MoveToPose(lowered: false, instantly: instantly);
    }

    public void Lower(bool instantly = false)
    {
        MoveToPose(lowered: true, instantly: instantly);
    }

    private void MoveToPose(bool lowered, bool instantly)
    {
        if (movingPart == null)
        {
            Debug.LogError($"Drop gate target '{name}' needs its moving part assigned.", this);
            return;
        }

        StopMovement();
        Vector3 targetPosition = lowered ? GetLoweredPosition() : raisedLocalPosition;

        if (instantly || moveDuration <= 0f)
        {
            movingPart.localPosition = targetPosition;
            return;
        }

        movement = StartCoroutine(AnimateTo(targetPosition));
    }

    private Vector3 GetLoweredPosition()
    {
        Vector3 worldDrop = Vector3.down * dropDistance;
        Transform parent = movingPart.parent;
        Vector3 localDrop = parent != null ? parent.InverseTransformVector(worldDrop) : worldDrop;
        return raisedLocalPosition + localDrop;
    }

    private void StopMovement()
    {
        if (movement == null)
            return;

        StopCoroutine(movement);
        movement = null;
    }

    private void OnBallHit(Collider ball)
    {
        if (pair == null)
        {
            Debug.LogError($"Drop gate target '{name}' needs a DropGatePair assigned.", this);
            return;
        }

        pair.TargetHit(this);
    }

    private IEnumerator AnimateTo(Vector3 targetPosition)
    {
        Vector3 startPosition = movingPart.localPosition;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            movingPart.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        movingPart.localPosition = targetPosition;
        movement = null;
    }
}
