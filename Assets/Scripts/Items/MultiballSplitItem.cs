using UnityEngine;

/// <summary>
/// Multiball: splits the ball in play into two. The new ball spawns next to
/// the original and moves with the opposite momentum.
/// </summary>
[CreateAssetMenu(fileName = "MultiballSplitItem", menuName = "ProjectBallin/Items/Multiball (Split)")]
public class MultiballSplitItem : ItemDefinition
{
    [Tooltip("Space left between the two balls when the new one spawns, as a fraction of the ball's radius.")]
    [SerializeField] private float spawnGap = 0.1f;

    public override bool Use() => BallSplitter.TrySplit(spawnGap);
}
