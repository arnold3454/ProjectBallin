using UnityEngine;

/// <summary>
/// One of the boss's attack phases. Phases live as components on the Boss prefab; the Boss
/// starts one at a time and ends it when the next phase is picked. Spawned objects that
/// should outlive the phase are removed in <see cref="Cleanup"/> when the fight ends.
/// </summary>
public abstract class BossPhase : MonoBehaviour
{
    [Header("Phase")]
    [SerializeField] private string phaseName = "Phase";
    [Tooltip("Colour shown on the boss's phase lamp while this phase runs.")]
    [SerializeField] private Color lampColor = Color.white;

    public string PhaseName => phaseName;
    public Color LampColor => lampColor;
    public bool IsActive { get; private set; }

    protected Boss Boss { get; private set; }
    protected BossManager Manager { get; private set; }

    /// <summary>0 when the boss timer is full, 1 when it has run out. Drives the scaling rules.</summary>
    protected float Urgency => Manager != null ? Manager.Urgency : 0f;

    public void Begin(Boss boss, BossManager manager)
    {
        Boss = boss;
        Manager = manager;
        IsActive = true;
        OnBegin();
    }

    public void End()
    {
        if (!IsActive)
            return;

        IsActive = false;
        OnEnd();
    }

    /// <summary>Removes anything this phase spawned. Called once when the fight ends.</summary>
    public virtual void Cleanup() { }

    protected abstract void OnBegin();
    protected abstract void OnEnd();
}
