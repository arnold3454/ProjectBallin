using UnityEngine;

/// <summary>
/// Keeps one enemy patrolling the table. The patrol line is drawn as a gizmo when this
/// object is selected: it runs along this object's X axis, centered on its position, so
/// place it near the top of the table at ball height. A new enemy spawns a few seconds
/// after the old one dies.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Enemy to spawn. Leave empty to build a simple red block at runtime.")]
    [SerializeField] private Enemy enemyPrefab;

    [Header("Patrol")]
    [Tooltip("Length of the patrol line, in world units.")]
    [SerializeField] private float patrolWidth = 28f;

    [Header("Default Enemy")]
    [Tooltip("Size of the block built when no prefab is assigned.")]
    [SerializeField] private Vector3 defaultSize = new Vector3(3f, 1.5f, 1.5f);

    [Header("Timing")]
    [SerializeField] private float firstSpawnDelay = 1f;
    [SerializeField] private float respawnDelay = 5f;

    private Enemy activeEnemy;
    private float spawnTimer;

    private void Start()
    {
        spawnTimer = firstSpawnDelay;
    }

    private void Update()
    {
        if (activeEnemy != null)
            return;

        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
            Spawn();
    }

    private void Spawn()
    {
        Vector3 halfLine = transform.right * (patrolWidth * 0.5f);
        Vector3 left = transform.position - halfLine;
        Vector3 right = transform.position + halfLine;

        activeEnemy = enemyPrefab != null
            ? Instantiate(enemyPrefab, left, transform.rotation)
            : CreateDefaultEnemy(left);

        activeEnemy.SetPatrol(left, right);
        activeEnemy.Died += OnEnemyDied;
    }

    private void OnEnemyDied(Enemy enemy)
    {
        enemy.Died -= OnEnemyDied;
        activeEnemy = null;
        spawnTimer = respawnDelay;
    }

    private Enemy CreateDefaultEnemy(Vector3 position)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Enemy";
        block.transform.SetPositionAndRotation(position, transform.rotation);
        block.transform.localScale = defaultSize;
        return block.AddComponent<Enemy>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f);
        Vector3 halfLine = transform.right * (patrolWidth * 0.5f);
        Gizmos.DrawLine(transform.position - halfLine, transform.position + halfLine);
        Gizmos.DrawWireCube(transform.position - halfLine, defaultSize);
        Gizmos.DrawWireCube(transform.position + halfLine, defaultSize);
    }
}
