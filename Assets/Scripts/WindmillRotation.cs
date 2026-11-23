using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WindmillRotation : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 90f;

    private Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (degreesPerSecond == 0f)
            return;

        Quaternion rotationStep = Quaternion.AngleAxis(
            degreesPerSecond * Time.fixedDeltaTime,
            Vector3.up);
        body.MoveRotation(body.rotation * rotationStep);
    }
}
