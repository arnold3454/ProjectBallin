using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Diamond phase: the dealer fans a stack of cards and blows gusts across the table. Each fan
/// sweeps the board either horizontally or vertically and pushes the ball sideways to its
/// travel: a fan sweeping left/right blows the ball down the table, a fan sweeping down the
/// table blows the ball to the right. The gust's reach and force grow as the boss timer runs low.
/// </summary>
public class DiamondPhase : BossPhase
{
    [Header("Fan")]
    [SerializeField] private CardFan fanPrefab;
    [SerializeField, Min(0.5f)] private float spawnInterval = 3f;
    [SerializeField, Min(0.1f)] private float travelSpeed = 7f;
    [SerializeField, Min(0.5f)] private float gustWidth = 4f;

    [Header("Scaling (calm -> urgent)")]
    [SerializeField] private float baseForce = 25f;
    [SerializeField] private float extraForce = 35f;
    [SerializeField] private float baseRange = 6f;
    [SerializeField] private float extraRange = 8f;

    [Header("Vertical fans")]
    [Tooltip("Fans sweeping down the table push the ball this way. The issue asks for right.")]
    [SerializeField] private bool pushRight = true;

    private readonly List<CardFan> fans = new List<CardFan>();
    private float spawnTimer;

    protected override void OnBegin()
    {
        spawnTimer = 0.5f;
    }

    protected override void OnEnd()
    {
        // Fans already in flight finish their sweep.
    }

    public override void Cleanup()
    {
        foreach (CardFan fan in fans)
        {
            if (fan != null)
                Destroy(fan.gameObject);
        }
        fans.Clear();
    }

    private void Update()
    {
        if (!IsActive)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        spawnTimer = spawnInterval;
        SpawnFan();
    }

    private void SpawnFan()
    {
        if (fanPrefab == null || Manager == null)
        {
            Debug.LogWarning("Diamond phase needs a fan prefab assigned.", this);
            return;
        }

        fans.RemoveAll(f => f == null);

        Vector2 min = Manager.BoardMin;
        Vector2 max = Manager.BoardMax;
        float y = Manager.SurfaceHeight;
        float force = baseForce + extraForce * Urgency;
        float range = baseRange + extraRange * Urgency;

        Vector3 start, travelDir, pushDir;
        float distance;
        if (Random.value < 0.5f)
        {
            // Sweep across the table, blow the ball down toward the flippers.
            bool leftToRight = Random.value < 0.5f;
            float z = Random.Range(min.y + 8f, max.y - 4f);
            start = new Vector3(leftToRight ? min.x - 2f : max.x + 2f, y, z);
            travelDir = leftToRight ? Vector3.right : Vector3.left;
            distance = (max.x - min.x) + 4f;
            pushDir = Vector3.back;
        }
        else
        {
            // Sweep down the table, blow the ball sideways.
            float x = Random.Range(min.x + 4f, max.x - 4f);
            start = new Vector3(x, y, max.y + 2f);
            travelDir = Vector3.back;
            distance = (max.y - min.y) + 4f;
            pushDir = pushRight ? Vector3.right : Vector3.left;
        }

        CardFan fan = Instantiate(fanPrefab, start, Quaternion.identity);
        fan.Launch(travelDir, distance, travelSpeed, pushDir, force, range, gustWidth);
        fans.Add(fan);
    }
}
