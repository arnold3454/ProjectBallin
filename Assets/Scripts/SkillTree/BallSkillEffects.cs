using UnityEngine;

/// <summary>Impact effects granted to balls by purchased skill-tree nodes.</summary>
public sealed class BallSkillEffects : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        ApplyImpactEffects(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ApplyImpactEffects(other);
    }

    private void ApplyImpactEffects(Collider target)
    {
        if (SkillTreeManager.GlassUnlocked && Random.value < SkillTreeManager.GlassShatterChance)
        {
            Destroy(gameObject);
            return;
        }

        if (!SkillTreeManager.DotUnlocked)
            return;

        Enemy enemy = target.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            SkillTreeManager.Instance.ApplyDamageOverTime(enemy);
            return;
        }

        Boss boss = target.GetComponentInParent<Boss>();
        if (boss != null)
            SkillTreeManager.Instance.ApplyDamageOverTime(boss);
    }
}
