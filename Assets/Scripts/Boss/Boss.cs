using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The dealer. Spawned over the roulette wheel when the player wins the spin. Every ball hit
/// wears it down and scores; the hit that kills it scores a bonus. Its attack phases are the
/// BossPhase components on the same object, and the BossManager tells it when to switch.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Boss : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 12;

    [Header("Scoring")]
    [SerializeField] private int hitPoints = 5;
    [SerializeField] private int killPoints = 100;

    [Header("Phases")]
    [Tooltip("Phases to pick from. A random one that differs from the current phase is chosen each switch.")]
    [SerializeField] private BossPhase[] phases;

    [Header("Visuals")]
    [SerializeField] private Renderer visualRenderer;
    [Tooltip("Lamp tinted with the current phase's colour.")]
    [SerializeField] private Renderer phaseLamp;
    [SerializeField] private Color healthyColor = new Color(0.12f, 0.12f, 0.14f);
    [SerializeField] private Color damagedColor = new Color(0.5f, 0.05f, 0.05f);
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashTime = 0.1f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>Raised once when health reaches zero. The manager removes the boss afterwards.</summary>
    public event Action<Boss> Died;
    /// <summary>Raised with (health, maxHealth) after every hit.</summary>
    public event Action<int, int> HealthChanged;
    /// <summary>Raised whenever a new phase begins.</summary>
    public event Action<BossPhase> PhaseChanged;

    public int Health { get; private set; }
    public int MaxHealth => maxHealth;
    public BossPhase CurrentPhase { get; private set; }
    public bool IsDead => Health <= 0;
    /// <summary>Number of phases on the prefab. The manager picks which of them a fight may use.</summary>
    public int PhaseCount => phases != null ? phases.Length : 0;

    private BossPhase[] allowedPhases;

    /// <summary>Hits per ball collision. Phases such as Club raise this to open a vulnerability window.</summary>
    public float DamageMultiplier { get; set; } = 1f;

    private Rigidbody body;
    private MaterialPropertyBlock colorBlock;
    private float flashTimer;
    private Color? tint;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        if (visualRenderer == null)
            visualRenderer = GetComponentInChildren<Renderer>();

        Health = maxHealth;
        ApplyHealthColor();
        ApplyLamp(Color.gray);
    }

    private void Update()
    {
        if (flashTimer <= 0f)
            return;

        flashTimer -= Time.deltaTime;
        if (flashTimer <= 0f)
            ApplyHealthColor();
    }

    /// <summary>
    /// Limits phase switches to the phases at these indices of the prefab's phase list. Null or
    /// an empty list allows every phase. Used to give each fight its own set of phases.
    /// </summary>
    public void SetAllowedPhases(IReadOnlyList<int> indices)
    {
        allowedPhases = null;
        if (indices == null || indices.Count == 0 || phases == null)
            return;

        List<BossPhase> allowed = new List<BossPhase>();
        foreach (int index in indices)
        {
            if (index >= 0 && index < phases.Length && phases[index] != null && !allowed.Contains(phases[index]))
                allowed.Add(phases[index]);
        }

        if (allowed.Count > 0)
            allowedPhases = allowed.ToArray();
    }

    /// <summary>Ends the current phase and starts a random different one from the allowed set.</summary>
    public void SwitchToRandomPhase(BossManager manager)
    {
        BossPhase[] pool = allowedPhases ?? phases;
        if (pool == null || pool.Length == 0 || IsDead)
            return;

        BossPhase next = pool[UnityEngine.Random.Range(0, pool.Length)];
        if (pool.Length > 1 && next == CurrentPhase)
        {
            int index = Array.IndexOf(pool, next);
            next = pool[(index + 1 + UnityEngine.Random.Range(0, pool.Length - 1)) % pool.Length];
        }

        if (CurrentPhase != null)
            CurrentPhase.End();

        CurrentPhase = next;
        if (next == null)
            return;

        next.Begin(this, manager);
        ApplyLamp(next.LampColor);
        PhaseChanged?.Invoke(next);
    }

    /// <summary>Stops the running phase and removes everything the phases spawned.</summary>
    public void EndPhases()
    {
        if (CurrentPhase != null)
        {
            CurrentPhase.End();
            CurrentPhase = null;
        }

        if (phases == null)
            return;

        foreach (BossPhase phase in phases)
        {
            if (phase != null)
                phase.Cleanup();
        }
    }

    /// <summary>
    /// Sets the health directly, clamped to 1..MaxHealth. Used to bring the dealer back with the
    /// health it had left when it got away. Doesn't score or kill.
    /// </summary>
    public void SetHealth(int health)
    {
        Health = Mathf.Clamp(health, 1, maxHealth);
        ApplyHealthColor();
        HealthChanged?.Invoke(Health, maxHealth);
    }

    public void TakeDamage(int hits)
    {
        if (IsDead || hits <= 0)
            return;

        int damage = Mathf.Max(1, Mathf.RoundToInt(hits * DamageMultiplier));
        Health = Mathf.Max(0, Health - damage);
        AddScore(hitPoints);
        HealthChanged?.Invoke(Health, maxHealth);

        if (Health > 0)
        {
            flashTimer = flashTime;
            ApplyColor(flashColor);
            return;
        }

        AddScore(killPoints);
        Died?.Invoke(this);
    }

    /// <summary>Overrides the body colour until <see cref="ClearTint"/> is called.</summary>
    public void SetTint(Color color)
    {
        tint = color;
        if (flashTimer <= 0f)
            ApplyColor(color);
    }

    public void ClearTint()
    {
        tint = null;
        if (flashTimer <= 0f)
            ApplyHealthColor();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag("Ball"))
            TakeDamage(1);
    }

    private void ApplyHealthColor()
    {
        if (tint.HasValue)
        {
            ApplyColor(tint.Value);
            return;
        }

        ApplyColor(Color.Lerp(damagedColor, healthyColor, (float)Health / maxHealth));
    }

    private void ApplyColor(Color color)
    {
        Apply(visualRenderer, color);
    }

    private void ApplyLamp(Color color)
    {
        Apply(phaseLamp, color);
    }

    private void Apply(Renderer target, Color color)
    {
        if (target == null)
            return;

        colorBlock ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, color);
        colorBlock.SetColor(ColorId, color);
        target.SetPropertyBlock(colorBlock);
    }

    private static void AddScore(int amount)
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(amount);
    }
}
