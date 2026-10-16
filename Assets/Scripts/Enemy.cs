using System;
using UnityEngine;

/// <summary>
/// An enemy that patrols back and forth along a line near the top of the table.
/// Every ball hit scores points and wears it down; the hit that kills it scores a bonus.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Enemy : MonoBehaviour
{
    [Header("Patrol")]
    [SerializeField] private float speed = 6f;

    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 5;

    [Header("Scoring")]
    [SerializeField] private int hitPoints = 3;
    [SerializeField] private int killPoints = 10;

    [Header("Visuals")]
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private Color healthyColor = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color damagedColor = new Color(0.35f, 0.05f, 0.05f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashTime = 0.1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Rigidbody body;
    private MaterialPropertyBlock colorBlock;
    private Vector3 leftPoint;
    private Vector3 rightPoint;
    private int direction = 1;
    private int health;
    private float flashTimer;

    /// <summary>Raised once when the enemy's health reaches zero, just before it is destroyed.</summary>
    public event Action<Enemy> Died;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<Renderer>();

        health = maxHealth;
        leftPoint = rightPoint = transform.position;
        ApplyColor(healthyColor);
    }

    /// <summary>Sets the patrol line and starts the enemy at the left end heading right.</summary>
    public void SetPatrol(Vector3 left, Vector3 right)
    {
        leftPoint = left;
        rightPoint = right;
        direction = 1;
        transform.position = left;
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
            ApplyColor(Color.Lerp(damagedColor, healthyColor, (float)health / maxHealth));
    }

    private void FixedUpdate()
    {
        if (GameTimer.Instance != null && GameTimer.Instance.IsGameOver)
            return;

        Vector3 target = direction > 0 ? rightPoint : leftPoint;
        Vector3 next = Vector3.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        body.MovePosition(next);

        if ((next - target).sqrMagnitude < 0.0001f)
            direction = -direction;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (health <= 0 || !collision.collider.CompareTag("Ball"))
            return;

        health--;
        AddScore(hitPoints);

        if (health > 0)
        {
            flashTimer = flashTime;
            ApplyColor(flashColor);
            return;
        }

        AddScore(killPoints);
        Died?.Invoke(this);
        Destroy(gameObject);
    }

    private static void AddScore(int amount)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(amount);
    }

    private void ApplyColor(Color color)
    {
        if (visualRenderer == null)
            return;

        colorBlock ??= new MaterialPropertyBlock();
        visualRenderer.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, color);
        colorBlock.SetColor(ColorId, color);
        visualRenderer.SetPropertyBlock(colorBlock);
    }
}
