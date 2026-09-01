using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(HingeJoint))]
public class FlipperControls : MonoBehaviour
{
    [SerializeField] private InputActionReference flipAction;

    private HingeJoint hinge;
    private float motorSpeed;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        motorSpeed = hinge.motor.targetVelocity;
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
}