using UnityEngine;

/// <summary>
/// A red or black tunnel on the side of the table. Running the ball through it places
/// that colour as the player's current bet at the roulette wheel. The lamp on top lights
/// up while this tunnel's colour is the active bet.
/// </summary>
[RequireComponent(typeof(BallHitboxTrigger))]
public class BetTunnel : MonoBehaviour
{
    [Header("Bet")]
    [SerializeField] private BetColor betColor = BetColor.Red;

    [Header("Lamp")]
    [Tooltip("Renderer that shows whether this tunnel's bet is active.")]
    [SerializeField] private Renderer lamp;
    [SerializeField] private Color lampOffColor = new Color(0.25f, 0.25f, 0.25f);
    [SerializeField] private Color lampOnColor = Color.white;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public BetColor BetColor => betColor;

    private BallHitboxTrigger hitbox;
    private MaterialPropertyBlock colorBlock;

    private void Awake()
    {
        hitbox = GetComponent<BallHitboxTrigger>();
        colorBlock = new MaterialPropertyBlock();
        SetActive(false);
    }

    private void OnEnable()
    {
        if (hitbox == null)
            hitbox = GetComponent<BallHitboxTrigger>();

        hitbox.BallHit += OnBallHit;

        if (RouletteManager.Instance != null)
            RouletteManager.Instance.RegisterTunnel(this);
    }

    private void Start()
    {
        // The manager may not have existed yet in OnEnable.
        if (RouletteManager.Instance != null)
            RouletteManager.Instance.RegisterTunnel(this);
    }

    private void OnDisable()
    {
        if (hitbox != null)
            hitbox.BallHit -= OnBallHit;

        if (RouletteManager.Instance != null)
            RouletteManager.Instance.UnregisterTunnel(this);
    }

    /// <summary>Lights the lamp while this tunnel's colour is the active bet.</summary>
    public void SetActive(bool active)
    {
        if (lamp == null)
            return;

        if (colorBlock == null)
            colorBlock = new MaterialPropertyBlock();

        Color color = active ? lampOnColor : lampOffColor;
        lamp.GetPropertyBlock(colorBlock);
        colorBlock.SetColor(BaseColorId, color);
        colorBlock.SetColor(ColorId, color);
        lamp.SetPropertyBlock(colorBlock);
    }

    private void OnBallHit(Collider ball)
    {
        if (RouletteManager.Instance == null)
        {
            Debug.LogWarning($"Bet tunnel '{name}' needs a RouletteManager in the scene.", this);
            return;
        }

        RouletteManager.Instance.PlaceBet(betColor);
    }
}
