using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BallHitboxTrigger))]
public class DropGateTarget : MonoBehaviour
{
    [SerializeField] private DropGatePair pair;

    [Header("Movement")]
    [Tooltip("The transform moved between the raised and lowered poses. Usually the target's moving root.")]
    [SerializeField] private Transform movingPart;
    [SerializeField] private Transform raisedPose;
    [SerializeField] private Transform loweredPose;
    [Tooltip("Use the moving part's authored starting transform as its raised position.")]
    [SerializeField] private bool useInitialPoseAsRaised = true;
    [SerializeField, Min(0.01f)] private float moveDuration = 0.35f;

    private BallHitboxTrigger hitbox;
    private Coroutine movement;
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();

        if (movingPart == null)
            movingPart = transform;

        initialLocalPosition = movingPart.localPosition;
        initialLocalRotation = movingPart.localRotation;
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

        if (movement != null)
        {
            StopCoroutine(movement);
            movement = null;
        }
    }

    public void SetDown(bool down, bool instant = false)
    {
        if (movingPart == null)
        {
            Debug.LogError($"Drop gate target '{name}' needs its moving part assigned.", this);
            return;
        }

        Vector3 targetPosition;
        Quaternion targetRotation;
        if (!down && useInitialPoseAsRaised)
        {
            targetPosition = initialLocalPosition;
            targetRotation = initialLocalRotation;
        }
        else
        {
            Transform pose = down ? loweredPose : raisedPose;
            if (pose == null)
            {
                Debug.LogError($"Drop gate target '{name}' needs its {(down ? "lowered" : "raised")} pose assigned.", this);
                return;
            }

            GetLocalPose(pose, out targetPosition, out targetRotation);
        }

        if (movement != null)
        {
            StopCoroutine(movement);
            movement = null;
        }

        if (instant)
        {
            movingPart.SetLocalPositionAndRotation(targetPosition, targetRotation);
            return;
        }

        movement = StartCoroutine(MoveTo(targetPosition, targetRotation));
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

    private void GetLocalPose(Transform pose, out Vector3 position, out Quaternion rotation)
    {
        Transform parent = movingPart.parent;
        if (parent == null)
        {
            position = pose.position;
            rotation = pose.rotation;
            return;
        }

        position = parent.InverseTransformPoint(pose.position);
        rotation = Quaternion.Inverse(parent.rotation) * pose.rotation;
    }

    private IEnumerator MoveTo(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = movingPart.localPosition;
        Quaternion startRotation = movingPart.localRotation;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            movingPart.SetLocalPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Slerp(startRotation, targetRotation, t));
            yield return null;
        }

        movingPart.SetLocalPositionAndRotation(targetPosition, targetRotation);
        movement = null;
    }
}
