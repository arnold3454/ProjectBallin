using UnityEngine;

public class OrbitAroundCenter : MonoBehaviour
{
    [Header("Orbit Settings")]
    [SerializeField] private Transform centerPoint;
    [Tooltip("World-space axis to revolve around.")]
    [SerializeField] private Vector3 axis = Vector3.up;
    [Tooltip("Orbit speed in degrees per second.")]
    [SerializeField] private float degreesPerSecond = 45f;

    private bool warnedAboutMissingCenter;
    private bool warnedAboutInvalidAxis;

    private void Update()
    {
        if (centerPoint == null)
        {
            if (!warnedAboutMissingCenter)
            {
                Debug.LogWarning("OrbitAroundCenter requires a center point. Assign one in the Inspector.", this);
                warnedAboutMissingCenter = true;
            }

            return;
        }

        if (axis.sqrMagnitude < 0.0001f)
        {
            if (!warnedAboutInvalidAxis)
            {
                Debug.LogWarning("OrbitAroundCenter requires a non-zero orbit axis.", this);
                warnedAboutInvalidAxis = true;
            }

            return;
        }

        transform.RotateAround(centerPoint.position, axis.normalized, degreesPerSecond * Time.deltaTime);
    }
}
