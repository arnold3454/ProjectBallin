using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the start screen where the player picks a ball type, then holds that choice for
/// the rest of the game: points multiplier, weight, speed, looks, and the Split ball's
/// duplicate ability. The game is paused until a ball is chosen.
/// </summary>
public class BallTypeManager : MonoBehaviour
{
    public static BallTypeManager Instance { get; private set; }

    /// <summary>True while the start screen is up. Gameplay scripts should ignore input until it clears.</summary>
    public static bool IsSelecting { get; private set; }

    [Header("Split Ball")]
    [SerializeField, Min(0f)] private float splitCooldown = 5f;
    [Tooltip("The Split ball can't duplicate while this many balls are already on the table.")]
    [SerializeField, Min(2)] private int maxBalls = 8;

    [Header("UI Prefabs")]
    [SerializeField] private GameObject screenPrefab;
    [SerializeField] private GameObject splitHudPrefab;
    [Tooltip("Assign one asset for each ball type. They are matched by their BallType value.")]
    [SerializeField] private BallTypeInfo[] ballTypes;

    private BallTypeSelectScreen screen;
    private BallTypeSplitHud splitHud;
    private BallTypeInfo current;
    private BallTypeInfo[] ballTypesByType;
    private Material ballMaterial;
    private PhysicsMaterial ballPhysicsMaterial;
    private float nextSplitTime;
    private bool choiceMade;

    public BallTypeInfo Current => current;

    // Static so callers needn't null-check the manager. Scenes without one play as the default ball.
    public static float PointsMultiplier => Instance != null && Instance.current != null ? Instance.current.PointsMultiplier : 1f;
    public static float WeightMultiplier => Instance != null && Instance.current != null ? Instance.current.WeightMultiplier : 1f;
    public static float SpeedMultiplier => Instance != null && Instance.current != null ? Instance.current.SpeedMultiplier : 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (screenPrefab == null || splitHudPrefab == null)
        {
            Debug.LogError("BallTypeManager is missing its selection screen prefab or split HUD prefab reference.", this);
            enabled = false;
            return;
        }

        if (!TryGetBallTypesByType(out ballTypesByType))
        {
            enabled = false;
            return;
        }

        current = ballTypesByType[(int)BallType.Default];
        GameObject screenObject = Instantiate(screenPrefab, transform);
        screen = screenObject.GetComponent<BallTypeSelectScreen>();
        if (screen == null)
        {
            Debug.LogError("BallTypeManager selection screen prefab is missing BallTypeSelectScreen.", this);
            enabled = false;
            Destroy(screenObject);
            return;
        }

        if (!screen.Initialize(ballTypesByType, Select))
        {
            Debug.LogError("BallTypeManager could not initialize its selection screen prefab.", this);
            enabled = false;
            Destroy(screen.gameObject);
            return;
        }

        GameObject splitHudObject = Instantiate(splitHudPrefab, transform);
        splitHud = splitHudObject.GetComponent<BallTypeSplitHud>();
        if (splitHud == null)
        {
            Debug.LogError("BallTypeManager split HUD prefab is missing BallTypeSplitHud.", this);
            enabled = false;
            Destroy(splitHudObject);
            Destroy(screen.gameObject);
            return;
        }

        // Pause until a ball is picked. Other scripts set the time scale back to 1 in their
        // Start, so Update keeps it at 0 while the screen is up.
        IsSelecting = true;
        Time.timeScale = 0f;
        screen.Show();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        IsSelecting = false;

        if (ballMaterial != null)
            Destroy(ballMaterial);
        if (ballPhysicsMaterial != null)
            Destroy(ballPhysicsMaterial);
    }

    private void Update()
    {
        if (IsSelecting)
        {
            Time.timeScale = 0f;
            HandleSelectKeys();
            return;
        }

        UpdateSplit();
    }

    private void HandleSelectKeys()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
            Select(BallType.Default);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
            Select(BallType.Bouncy);
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
            Select(BallType.Split);
    }

    private void Select(BallType type)
    {
        if (choiceMade)
            return;

        BallTypeInfo selected = GetBallType(type);
        if (selected == null)
        {
            Debug.LogError($"No ball type asset is configured for {type}.", this);
            return;
        }

        choiceMade = true;
        current = selected;

        // A ball may already be waiting on the plunger from before the choice was made.
        foreach (GameObject ball in GameObject.FindGameObjectsWithTag("Ball"))
            ApplyTo(ball);

        screen.Hide();
        StartCoroutine(BeginPlay());
    }

    private IEnumerator BeginPlay()
    {
        // Resume a frame later so the click or key that made the choice can't also be read by
        // other scripts (the 1-3 keys use items) once time starts again.
        yield return null;

        IsSelecting = false;
        Time.timeScale = 1f;
    }

    /// <summary>Gives a freshly spawned ball the look and physics of the chosen type.</summary>
    public static void Apply(GameObject ball)
    {
        if (Instance != null)
            Instance.ApplyTo(ball);
    }

    private void ApplyTo(GameObject ball)
    {
        if (current == null || current.Type == BallType.Default || ball == null)
            return;

        // Called once per ball. The weight slider applies the same multiplier when it is moved.
        if (ball.TryGetComponent(out Rigidbody body))
            body.mass *= current.WeightMultiplier;

        if (ball.TryGetComponent(out MeshRenderer ballRenderer))
        {
            if (ballMaterial == null)
                ballMaterial = CreateTintedMaterial(ballRenderer.sharedMaterial, current.Color);

            ballRenderer.sharedMaterial = ballMaterial;
        }

        if (current.Bounciness >= 0f && ball.TryGetComponent(out Collider ballCollider))
        {
            if (ballPhysicsMaterial == null)
                ballPhysicsMaterial = CreateBouncyMaterial(ballCollider.sharedMaterial, current.Bounciness);

            ballCollider.sharedMaterial = ballPhysicsMaterial;
        }
    }

    private static Material CreateTintedMaterial(Material source, Color color)
    {
        Material material = new Material(source);

        // Cover both the built-in (_Color) and URP (_BaseColor) shaders.
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);

        return material;
    }

    private static PhysicsMaterial CreateBouncyMaterial(PhysicsMaterial source, float bounciness)
    {
        PhysicsMaterial material = source != null
            ? new PhysicsMaterial { dynamicFriction = source.dynamicFriction, staticFriction = source.staticFriction, frictionCombine = source.frictionCombine }
            : new PhysicsMaterial { dynamicFriction = 0f, staticFriction = 0f };

        material.name = "BouncyBall";
        material.bounciness = bounciness;

        // Maximum wins over every other combine mode, so the table can't damp the bounce.
        material.bounceCombine = PhysicsMaterialCombine.Maximum;
        return material;
    }

    private void UpdateSplit()
    {
        bool active = current.Type == BallType.Split;
        if (splitHud != null)
            splitHud.SetVisible(active);

        if (!active)
            return;

        // Time stops for the pause menu, level-up choice and game over, so one check covers them all.
        if (Time.timeScale <= 0f)
            return;

        float wait = nextSplitTime - Time.time;
        splitHud.SetStatus(
            wait > 0f
                ? $"[{BallTypeInfo.SplitKey}] Split in {wait:0.0}s"
                : $"[{BallTypeInfo.SplitKey}] Split ready",
            wait > 0f ? new Color(0.7f, 0.72f, 0.8f) : new Color(0.55f, 0.7f, 1f));

        if (wait > 0f || Keyboard.current == null || !Keyboard.current[BallTypeInfo.SplitKey].wasPressedThisFrame)
            return;

        if (GameObject.FindGameObjectsWithTag("Ball").Length >= maxBalls)
            return;

        // Only start the cooldown if a ball was actually split.
        if (BallSplitter.TrySplit())
            nextSplitTime = Time.time + splitCooldown;
    }

    private bool TryGetBallTypesByType(out BallTypeInfo[] typesByType)
    {
        int typeCount = (int)BallType.Split + 1;
        typesByType = new BallTypeInfo[typeCount];

        if (ballTypes == null || ballTypes.Length != typeCount)
        {
            Debug.LogError($"BallTypeManager expects {typeCount} ball type assets, but has {(ballTypes == null ? 0 : ballTypes.Length)}.", this);
            return false;
        }

        for (int i = 0; i < ballTypes.Length; i++)
        {
            BallTypeInfo info = ballTypes[i];
            if (info == null)
            {
                Debug.LogError($"BallTypeManager ball type asset at array index {i} is unassigned.", this);
                return false;
            }

            int typeIndex = (int)info.Type;
            if (typeIndex < 0 || typeIndex >= typeCount)
            {
                Debug.LogError($"Ball type asset '{info.name}' has an unsupported BallType value: {info.Type}.", this);
                return false;
            }

            if (typesByType[typeIndex] != null)
            {
                Debug.LogError($"BallTypeManager has more than one asset assigned for {info.Type}.", this);
                return false;
            }

            typesByType[typeIndex] = info;
        }

        for (int i = 0; i < typesByType.Length; i++)
        {
            if (typesByType[i] != null)
                continue;

            Debug.LogError($"BallTypeManager has no asset assigned for {(BallType)i}.", this);
            return false;
        }

        return true;
    }

    private BallTypeInfo GetBallType(BallType type)
    {
        int index = (int)type;
        return index >= 0 && index < ballTypesByType.Length
            ? ballTypesByType[index]
            : null;
    }
}
