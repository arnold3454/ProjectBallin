using System;
using UnityEngine;

public class DropGatePair : MonoBehaviour
{
    [SerializeField] private DropGateTarget[] targets;

    private DropGateTarget loweredTarget;

    private void Start()
    {
        if (targets == null || targets.Length < 2)
        {
            Debug.LogError($"Drop gate pair '{name}' needs at least two targets assigned.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                Debug.LogError($"Drop gate pair '{name}' has an unassigned target at index {i}.", this);
                enabled = false;
                return;
            }

            for (int j = 0; j < i; j++)
            {
                if (targets[i] == targets[j])
                {
                    Debug.LogError($"Drop gate pair '{name}' has the same target assigned more than once.", this);
                    enabled = false;
                    return;
                }
            }
        }

        foreach (DropGateTarget target in targets)
            target.SetDown(false, instant: true);
    }

    public void TargetHit(DropGateTarget hitTarget)
    {
        if (!enabled || Array.IndexOf(targets, hitTarget) < 0)
        {
            Debug.LogError($"Drop gate target '{hitTarget.name}' is not registered with pair '{name}'.", this);
            return;
        }

        if (hitTarget == loweredTarget)
            return;

        if (loweredTarget != null)
            loweredTarget.SetDown(false);

        loweredTarget = hitTarget;
        loweredTarget.SetDown(true);
    }
}
