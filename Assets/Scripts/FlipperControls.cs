using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(HingeJoint))]
public class FlipperControls : MonoBehaviour
{
    [SerializeField] private InputActionReference flipAction;

    [Header("Audio")]
    [Tooltip("Played when the player presses the button to activate the flipper.")]
    [SerializeField] private AudioClip flipClip;
    [SerializeField, Range(0f, 1f)] private float flipVolume = 1f;

    [Header("Cooldown")]
    [Tooltip("Seconds after each flip before the flipper can swing again. The Quick Recovery upgrade shortens it. A press during the cooldown fires as soon as it ends. Holding the flipper up past the cooldown costs nothing.")]
    [SerializeField, Min(0f)] private float flipCooldown = 0.6f;
    [Tooltip("The flipper is tinted this colour right after a flip and fades back as the cooldown runs out.")]
    [SerializeField] private Color cooldownTint = new Color(0.9f, 0.2f, 0.15f, 1f);

private HingeJoint hinge;
private Rigidbody body;
private AudioSource audioSource;
private float motorScale = 1f;
private float baseMotorSpeed; // Added to store the original speed
private float baseMotorForce;
private float heightOffset;
private Vector3 baseWorldPosition;
private Vector3 baseConnectedAnchor;
private bool tiltLocked;
private float cooldownEndTime;
private float cooldownDuration;
private bool pendingFlip;
private bool tinted;
private Renderer[] renderers;
private Color[] baseColors;
private MaterialPropertyBlock tintBlock;

private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
private static readonly int ColorId = Shader.PropertyToID("_Color");

private float MotorSpeed => baseMotorSpeed * motorScale * (UpgradeManager.Instance != null ? UpgradeManager.Instance.FlipperSpeedMultiplier : 1f);
/// <summary>Cooldown before any upgrades, in seconds.</summary>
public float BaseCooldown => flipCooldown;

private float CooldownDuration => flipCooldown * (UpgradeManager.Instance != null ? UpgradeManager.Instance.FlipperCooldownMultiplier : 1f);

private void Awake()
    {
hinge = GetComponent<HingeJoint>();
body = GetComponent<Rigidbody>();
body.useGravity = false;
baseMotorSpeed = hinge.motor.targetVelocity; 
baseMotorForce = hinge.motor.force;
baseWorldPosition = transform.position;
baseConnectedAnchor = hinge.connectedAnchor;
CacheRenderers();

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

private void Update()
    {
UpdateCooldownTint();

// A press that landed during the cooldown goes off the moment it ends.
if (pendingFlip && !tiltLocked && Time.time >= cooldownEndTime)
    {
pendingFlip = false;
Activate();
    }
    }

private void OnFlip(InputAction.CallbackContext ctx)
    {
if (tiltLocked)
return;

// The upgrade popup has the screen; keep the flippers from reacting to its key presses.
if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsChoosing)
return;

if (Time.time < cooldownEndTime)
    {
pendingFlip = true;
return;
    }

Activate();
    }

private void OnRelease(InputAction.CallbackContext ctx)
    {
pendingFlip = false;
SetDirection(-MotorSpeed);
    }

private void Activate()
    {
SetDirection(MotorSpeed);
PlayFlipSFX();

// The cooldown runs from each flip, so quick taps are what it slows down.
cooldownDuration = CooldownDuration;
cooldownEndTime = Time.time + cooldownDuration;
    }

private void CacheRenderers()
    {
renderers = GetComponentsInChildren<Renderer>();
baseColors = new Color[renderers.Length];
for (int i = 0; i < renderers.Length; i++)
    {
Material material = renderers[i].sharedMaterial;
if (material == null)
continue;

if (material.HasProperty(BaseColorId))
baseColors[i] = material.GetColor(BaseColorId);
else if (material.HasProperty(ColorId))
baseColors[i] = material.GetColor(ColorId);
else
baseColors[i] = Color.white;
    }
    }

private void UpdateCooldownTint()
    {
if (renderers == null || renderers.Length == 0)
return;

float remaining = cooldownEndTime - Time.time;
if (remaining <= 0f || cooldownDuration <= 0f)
    {
if (tinted)
        {
// Back to the material's own colour.
foreach (Renderer r in renderers)
r.SetPropertyBlock(null);
tinted = false;
        }
return;
    }

tintBlock ??= new MaterialPropertyBlock();
float strength = Mathf.Clamp01(remaining / cooldownDuration);
for (int i = 0; i < renderers.Length; i++)
    {
Color color = Color.Lerp(baseColors[i], cooldownTint, strength);
renderers[i].GetPropertyBlock(tintBlock);
tintBlock.SetColor(BaseColorId, color);
tintBlock.SetColor(ColorId, color);
renderers[i].SetPropertyBlock(tintBlock);
    }
tinted = true;
    }

/// <summary>Enables or disables player control while preserving this flipper's settings.</summary>
public void SetTiltLocked(bool isLocked)
    {
tiltLocked = isLocked;

if (tiltLocked)
    {
pendingFlip = false;
SetDirection(-MotorSpeed);
    }
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
motorScale = multiplier;
    }

/// <summary>Re-reads the upgrade stats. A flipper that is mid-swing keeps its direction at the new speed.</summary>
public void RefreshUpgrades()
    {
float target = hinge.motor.targetVelocity;
if (target != 0f)
SetDirection(Mathf.Sign(target) * Mathf.Abs(MotorSpeed));
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

public void RebaseMapPosition()
{
baseWorldPosition = transform.position;
body.position = baseWorldPosition;
hinge.autoConfigureConnectedAnchor = false;
baseConnectedAnchor = transform.TransformPoint(hinge.anchor);
hinge.connectedAnchor = baseConnectedAnchor;
body.WakeUp();
}

}