using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Heart phase: the dealer calls in Suit enemies, a stronger pit-boss variant that dies in a
/// couple of hits and pays out more points. One to three arrive while the boss timer is
/// comfortable; up to six when it is nearly out. Each gets its own random patrol line.
/// Suits that survive the phase stay on the table until killed or the fight ends.
/// </summary>
public class HeartPhase : BossPhase
{
    [Header("Suits")]
    [SerializeField] private Enemy suitPrefab;
    [SerializeField, Min(1)] private int minCount = 1;
    [Tooltip("Most suits sent at the start of the fight.")]
    [SerializeField, Min(1)] private int maxCountCalm = 3;
    [Tooltip("Most suits sent when the boss timer is nearly out.")]
    [SerializeField, Min(1)] private int maxCountUrgent = 6;
    [SerializeField, Min(0f)] private float spawnStagger = 0.4f;

    [Header("Patrol lines")]
    [SerializeField, Min(2f)] private float minPatrolWidth = 8f;
    [SerializeField, Min(2f)] private float maxPatrolWidth = 20f;
    [Tooltip("How far from the bottom of the usable board the lowest patrol may run.")]
    [SerializeField] private float minDepthFromBottom = 14f;
    [Tooltip("How far from the top of the usable board the highest patrol may run.")]
    [SerializeField] private float minDepthFromTop = 5f;
    [Tooltip("Keep patrol lines this far from the side walls.")]
    [SerializeField] private float sideMargin = 2.5f;

    private readonly List<Enemy> suits = new List<Enemy>();
    private Coroutine spawning;

    protected override void OnBegin()
    {
        suits.RemoveAll(s => s == null);

        int max = Mathf.RoundToInt(Mathf.Lerp(maxCountCalm, maxCountUrgent, Urgency));
        int wanted = Random.Range(minCount, max + 1);
        int toSpawn = Mathf.Max(0, Mathf.Min(wanted, max - suits.Count));

        if (spawning != null)
            StopCoroutine(spawning);
        spawning = StartCoroutine(SpawnSuits(toSpawn));
    }

    protected override void OnEnd()
    {
        if (spawning != null)
        {
            StopCoroutine(spawning);
            spawning = null;
        }
    }

    public override void Cleanup()
    {
        OnEnd();
        foreach (Enemy suit in suits)
        {
            if (suit != null)
            {
                suit.Died -= OnSuitDied;
                Destroy(suit.gameObject);
            }
        }
        suits.Clear();
    }

    private IEnumerator SpawnSuits(int count)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnSuit();
            if (spawnStagger > 0f)
                yield return new WaitForSeconds(spawnStagger);
        }
        spawning = null;
    }

    private void SpawnSuit()
    {
        if (suitPrefab == null || Manager == null)
        {
            Debug.LogWarning("Heart phase needs a suit enemy prefab assigned.", this);
            return;
        }

        Vector2 min = Manager.BoardMin;
        Vector2 max = Manager.BoardMax;

        float width = Random.Range(minPatrolWidth, maxPatrolWidth);
        float usable = (max.x - min.x) - 2f * sideMargin;
        width = Mathf.Min(width, usable);
        float centerX = Random.Range(min.x + sideMargin + width * 0.5f, max.x - sideMargin - width * 0.5f);
        float z = Random.Range(min.y + minDepthFromBottom, max.y - minDepthFromTop);
        float y = Manager.SurfaceHeight;

        Vector3 left = new Vector3(centerX - width * 0.5f, y, z);
        Vector3 right = new Vector3(centerX + width * 0.5f, y, z);

        Enemy suit = Instantiate(suitPrefab, left, Quaternion.identity);
        suit.SetPatrol(left, right);
        suit.Died += OnSuitDied;
        suits.Add(suit);
    }

    private void OnSuitDied(Enemy suit)
    {
        suit.Died -= OnSuitDied;
        suits.Remove(suit);
    }
}
