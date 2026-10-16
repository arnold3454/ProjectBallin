using UnityEngine;

/// <summary>
/// Club phase. The issue for this phase has no description yet, so this is a placeholder
/// that still reads as a distinct phase: the dealer shows its hand and is vulnerable, taking
/// extra damage per ball hit while its body pulses the phase colour. Replace or extend once
/// the design is written up.
/// </summary>
public class ClubPhase : BossPhase
{
    [Header("Vulnerability")]
    [Tooltip("Hits dealt per ball collision while this phase runs.")]
    [SerializeField, Min(1f)] private float damageMultiplier = 2f;

    [Header("Pulse")]
    [SerializeField] private Color pulseColor = new Color(0.2f, 0.9f, 0.3f);
    [SerializeField, Min(0.1f)] private float pulseSpeed = 4f;

    private Color restColor = new Color(0.12f, 0.12f, 0.14f);

    protected override void OnBegin()
    {
        if (Boss != null)
            Boss.DamageMultiplier = damageMultiplier;
    }

    protected override void OnEnd()
    {
        if (Boss == null)
            return;

        Boss.DamageMultiplier = 1f;
        Boss.ClearTint();
    }

    private void Update()
    {
        if (!IsActive || Boss == null)
            return;

        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        Boss.SetTint(Color.Lerp(restColor, pulseColor, t));
    }
}
