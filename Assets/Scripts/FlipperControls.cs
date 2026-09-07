using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HingeJoint))]
public class FlipperControls : MonoBehaviour
{
    [SerializeField] private InputActionReference flipAction;

    private HingeJoint hinge;
    private Rigidbody body;
    private float motorSpeed;
    private float baseMotorSpeed; // Added to store the original speed
    private Vector3 baseWorldPosition;
    private Vector3 baseConnectedAnchor;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        baseMotorSpeed = hinge.motor.targetVelocity; 
        motorSpeed = baseMotorSpeed;
        baseWorldPosition = transform.position;
        baseConnectedAnchor = hinge.connectedAnchor;
    }

    private void OnEnable()
    {
        flipAction.action.performed += OnFlip;
        flipAction.action.canceled += OnRelease;
        flipAction.action.Enable();
    }

    private void OnDisable()
    {
        flipAction.action.performed -= OnFlip;
        flipAction.action.canceled -= OnRelease;
        flipAction.action.Disable();
    }

    private void OnFlip(InputAction.CallbackContext ctx) => SetDirection(motorSpeed);
    private void OnRelease(InputAction.CallbackContext ctx) => SetDirection(-motorSpeed);

    private void SetDirection(float targetVelocity)
    {
        JointMotor motor = hinge.motor;
        motor.targetVelocity = targetVelocity; 
        hinge.motor = motor;
    }

    // New method to scale the motor speed dynamically
    public void ScaleMotor(float multiplier)
    {
        motorSpeed = baseMotorSpeed * multiplier;
    }

    public void ApplyMapPosition(float mapScale, Vector3 mapOrigin, float horizontalInset, Vector3 offset)
    {
        Vector3 position = mapOrigin + (baseWorldPosition - mapOrigin) * mapScale;
        position.x -= Mathf.Sign(baseWorldPosition.x - mapOrigin.x) * horizontalInset * mapScale;
        position += offset * mapScale;
        hinge.autoConfigureConnectedAnchor = false;
        hinge.connectedAnchor = baseConnectedAnchor + (position - baseWorldPosition);
        body.position = position;
        body.WakeUp();
        Physics.SyncTransforms();
    }
}