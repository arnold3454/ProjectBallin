using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HingeJoint))]
public class FlipperControls : MonoBehaviour
{
    [SerializeField] private InputActionReference flipAction;
    private static readonly Dictionary<InputAction, int> activeActionUsers = new Dictionary<InputAction, int>();

    [Header("Audio")]
    [Tooltip("Played when the player presses the button to activate the flipper.")]
    [SerializeField] private AudioClip flipClip;
    [SerializeField, Range(0f, 1f)] private float flipVolume = 1f;

private HingeJoint hinge;
private Rigidbody body;
private AudioSource audioSource;
private float motorSpeed;
private float baseMotorSpeed; // Added to store the original speed
private float baseMotorForce;
private float heightOffset;
private Vector3 baseWorldPosition;
private Vector3 baseConnectedAnchor;
private bool tiltLocked;

private void Awake()
    {
hinge = GetComponent<HingeJoint>();
body = GetComponent<Rigidbody>();
body.useGravity = false;
baseMotorSpeed = hinge.motor.targetVelocity; 
baseMotorForce = hinge.motor.force;
motorSpeed = baseMotorSpeed;
baseWorldPosition = transform.position;
baseConnectedAnchor = hinge.connectedAnchor;

audioSource = GetComponent<AudioSource>();
if (audioSource == null)
    {
audioSource = gameObject.AddComponent<AudioSource>();
    }
audioSource.playOnAwake = false;
audioSource.spatialBlend = 0f;
    }

private void OnEnable()
    {
        if (flipAction == null || flipAction.action == null)
            return;

        InputAction action = flipAction.action;
        action.performed += OnFlip;
        action.canceled += OnRelease;

        if (activeActionUsers.TryGetValue(action, out int users))
            activeActionUsers[action] = users + 1;
        else
        {
            activeActionUsers.Add(action, 1);
            action.Enable();
        }

    }

private void OnDisable()
    {
        if (flipAction == null || flipAction.action == null)
            return;

        InputAction action = flipAction.action;
        action.performed -= OnFlip;
        action.canceled -= OnRelease;

        if (!activeActionUsers.TryGetValue(action, out int users))
            return;

        if (users <= 1)
        {
            activeActionUsers.Remove(action);
            action.Disable();
        }
        else
            activeActionUsers[action] = users - 1;

    }

private void OnFlip(InputAction.CallbackContext ctx)
    {
if (!tiltLocked)
    {
SetDirection(motorSpeed);
PlayFlipSFX();
    }
    }

private void OnRelease(InputAction.CallbackContext ctx) => SetDirection(-motorSpeed);

/// <summary>Enables or disables player control while preserving this flipper's settings.</summary>
public void SetTiltLocked(bool isLocked)
    {
tiltLocked = isLocked;

if (tiltLocked)
SetDirection(-motorSpeed);
    }

private void SetDirection(float targetVelocity)
    {
JointMotor motor = hinge.motor;
motor.targetVelocity = targetVelocity; 
hinge.motor = motor;
    }

private void PlayFlipSFX()
    {
if (flipClip == null || audioSource == null)
return;

audioSource.PlayOneShot(flipClip, flipVolume);
    }

// New method to scale the motor speed dynamically
public void ScaleMotor(float multiplier)
    {
motorSpeed = baseMotorSpeed * multiplier;
    }

public void UpdateStrength(float strengthMultiplier)
    {
JointMotor motor = hinge.motor;
motor.force = baseMotorForce * Mathf.Max(0f, strengthMultiplier);
hinge.motor = motor;
    }

    public void UpdateHeightOffset(float offset)
    {
        heightOffset = offset;
    }

    public void RebaseMapPosition()
    {
        baseWorldPosition = transform.position;
        body.position = baseWorldPosition;
        hinge.autoConfigureConnectedAnchor = false;
        baseConnectedAnchor = transform.TransformPoint(hinge.anchor);
        hinge.connectedAnchor = baseConnectedAnchor;
        body.WakeUp();
    }

    public void ApplyMapPosition(float mapScale, Vector3 mapOrigin, float horizontalInset, Vector3 offset)

    {
heightOffset = offset;
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