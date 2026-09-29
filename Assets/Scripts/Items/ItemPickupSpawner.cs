using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns item pickups at random open spots on the playfield at random
/// intervals. The spawn area is the box drawn around this object when it is
/// selected. Pickups spawn at this object's height, so keep it at ball height.
/// </summary>
public class ItemPickupSpawner : MonoBehaviour
{
    [SerializeField] private ItemPickup pickupPrefab;

    [Header("Area")]
    [Tooltip("Width (X) and length (Z) of the area pickups can appear in.")]
    [SerializeField] private Vector2 areaSize = new Vector2(27f, 34f);
    [Tooltip("Free space needed around a spot before a pickup can spawn there.")]
    [SerializeField] private float clearance = 1.5f;
    [SerializeField, Min(1)] private int placementAttempts = 20;

    [Header("Timing")]
    [SerializeField] private float firstSpawnDelay = 5f;
    [SerializeField] private float minSpawnInterval = 10f;
    [SerializeField] private float maxSpawnInterval = 15f;
    [SerializeField, Min(1)] private int maxActivePickups = 2;

    [Header("Audio")]
    [Tooltip("Played when an item spawns.")]
    [SerializeField] private AudioClip spawnClip;
    [SerializeField, Range(0f, 1f)] private float spawnVolume = 1f;

    private readonly List<ItemPickup> activePickups = new List<ItemPickup>();
    private float spawnTimer;
    private AudioSource audioSource;

    private void Start()
    {
        spawnTimer = firstSpawnDelay;
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.spatialBlend = 0f; // 0 = fully 2D, 1 = fully 3D
        audioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
        {
            return;
        }

        activePickups.RemoveAll(pickup => pickup == null);
        if (activePickups.Count >= maxActivePickups)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        // If no open spot was found, try again in a second.
        spawnTimer = TrySpawnPickup() ? Random.Range(minSpawnInterval, maxSpawnInterval) : 1f;
    }

    /// <summary>Spawns a pickup with a random item. Returns false if no open spot was found.</summary>
    public bool TrySpawnPickup()
    {
        if (pickupPrefab == null || ItemManager.Instance == null || !TryFindSpawnPoint(out Vector3 point))
            return false;

        ItemPickup pickup = Instantiate(pickupPrefab, point, Quaternion.identity);
        pickup.SetItem(ItemManager.Instance.GetRandomItem());
        activePickups.Add(pickup);

        PlaySpawnSFX(); // <-- here

        return true;
    }

    private bool TryFindSpawnPoint(out Vector3 point)
    {
        // Only check a thin slice at ball height so the table surface below doesn't count,
        // but walls, bumpers, balls and other pickups do.
        Vector3 halfExtents = new Vector3(clearance, 0.25f, clearance);

        for (int i = 0; i < placementAttempts; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-0.5f, 0.5f) * areaSize.x,
                0f,
                Random.Range(-0.5f, 0.5f) * areaSize.y);
            point = transform.position + transform.rotation * offset;

            if (!Physics.CheckBox(point, halfExtents, transform.rotation, Physics.AllLayers, QueryTriggerInteraction.Collide))
                return true;
        }

        point = transform.position;
        return false;
    }

    private void PlaySpawnSFX()
    {
        if (spawnClip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(spawnClip, spawnVolume);
        print($"Played spawn SFX {spawnClip.name} at volume {spawnVolume}");

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(areaSize.x, 0.1f, areaSize.y));
    }
}
