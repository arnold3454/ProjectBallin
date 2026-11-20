using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class BallMagnetLauncher : MonoBehaviour
{
    [Header("Capture")]
    [SerializeField] private Transform holdPoint;
    [Tooltip("Gameplay seconds to hold the ball before launching it. The countdown pauses with the game.")]
    [SerializeField, Min(0f)] private float holdDuration = 2f;

    [Header("Launch")]
    [SerializeField, Min(0f)] private float launchSpeed = 18f;
    [Tooltip("Total horizontal angle of the random launch arc. 180 degrees launches into a semicircle facing this object's forward direction.")]
    [SerializeField, Range(0f, 180f)] private float launchArc = 180f;

    private Rigidbody capturedBall;
    private Coroutine launchRoutine;
    private Vector3 capturedVelocity;
    private Vector3 capturedAngularVelocity;
    private Collider[] capturedBallColliders;
    private readonly List<bool> capturedColliderStates = new List<bool>();
    private Rigidbody recentlyLaunchedBall;
    private float recentlyLaunchedBallRadius;
    private SphereCollider captureTrigger;

    private void Awake()
    {
        captureTrigger = GetComponent<SphereCollider>();
        captureTrigger.isTrigger = true;

        if (holdPoint == null)
            holdPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody ball = other.attachedRigidbody;
        if (capturedBall != null ||
            ball == null ||
            ball == recentlyLaunchedBall ||
            !ball.CompareTag("Ball") ||
            ball.isKinematic)
            return;

        capturedBall = ball;
        capturedVelocity = ball.linearVelocity;
        capturedAngularVelocity = ball.angularVelocity;
        CaptureBallColliders(ball);
        ball.linearVelocity = Vector3.zero;
        ball.angularVelocity = Vector3.zero;
        ball.position = holdPoint.position;
        ball.isKinematic = true;

        launchRoutine = StartCoroutine(WaitThenLaunch());
    }

    private void FixedUpdate()
    {
        if (capturedBall != null && holdPoint != null)
        {
            capturedBall.MovePosition(holdPoint.position);
            return;
        }

        if (recentlyLaunchedBall == null)
            return;

        Vector3 triggerCenter = captureTrigger.transform.TransformPoint(captureTrigger.center);
        Vector3 ballPosition = recentlyLaunchedBall.worldCenterOfMass;
        float triggerScale = Mathf.Max(
            Mathf.Abs(captureTrigger.transform.lossyScale.x),
            Mathf.Abs(captureTrigger.transform.lossyScale.y),
            Mathf.Abs(captureTrigger.transform.lossyScale.z));
        float captureDistance = captureTrigger.radius * triggerScale + recentlyLaunchedBallRadius;

        if ((ballPosition - triggerCenter).sqrMagnitude > captureDistance * captureDistance)
            recentlyLaunchedBall = null;
    }

    private void OnDisable()
    {
        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            launchRoutine = null;
        }

        if (capturedBall != null)
        {
            RestoreBallColliders();
            capturedBall.isKinematic = false;
            capturedBall.linearVelocity = capturedVelocity;
            capturedBall.angularVelocity = capturedAngularVelocity;
            capturedBall = null;
        }
    }

    private IEnumerator WaitThenLaunch()
    {
        yield return new WaitForSeconds(holdDuration);

        if (capturedBall == null)
        {
            launchRoutine = null;
            yield break;
        }

        Rigidbody ball = capturedBall;
        capturedBall = null;
        launchRoutine = null;
        recentlyLaunchedBall = ball;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        float halfArc = launchArc * 0.5f;
        float angle = Random.Range(-halfArc, halfArc);
        Vector3 launchDirection = Quaternion.AngleAxis(angle, Vector3.up) * forward.normalized;
        float speedMultiplier = UpgradeManager.Instance != null
            ? UpgradeManager.Instance.BallSpeedMultiplier
            : 1f;

        RestoreBallColliders();
        ball.isKinematic = false;
        ball.linearVelocity = launchDirection * launchSpeed * speedMultiplier;
        ball.angularVelocity = Vector3.zero;
        ball.WakeUp();
    }

    private void CaptureBallColliders(Rigidbody ball)
    {
        capturedBallColliders = ball.GetComponentsInChildren<Collider>(true);
        capturedColliderStates.Clear();
        recentlyLaunchedBallRadius = 0f;

        foreach (Collider ballCollider in capturedBallColliders)
        {
            capturedColliderStates.Add(ballCollider.enabled);
            if (ballCollider.enabled)
                recentlyLaunchedBallRadius = Mathf.Max(recentlyLaunchedBallRadius, ballCollider.bounds.extents.magnitude);
            ballCollider.enabled = false;
        }
    }

    private void RestoreBallColliders()
    {
        if (capturedBallColliders == null)
            return;

        for (int i = 0; i < capturedBallColliders.Length; i++)
        {
            if (capturedBallColliders[i] != null)
                capturedBallColliders[i].enabled = capturedColliderStates[i];
        }

        capturedBallColliders = null;
        capturedColliderStates.Clear();
    }
}
