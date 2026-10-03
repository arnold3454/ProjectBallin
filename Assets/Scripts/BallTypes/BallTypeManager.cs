using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows the start screen where the player picks a ball type, then holds that choice for
/// the rest of the game: points multiplier, weight, speed, looks, and the Split ball's
/// duplicate ability. The game is paused until a ball is chosen.
/// Creates itself in any scene that has a ScoreManager, so it needs no scene setup.
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

    private BallTypeSelectScreen screen;
    private BallTypeInfo current = BallTypeInfo.Get(BallType.Default);
    private Material ballMaterial;
    private PhysicsMaterial ballPhysicsMaterial;
    private TextMeshProUGUI splitLabel;
    private float nextSplitTime;
    private bool choiceMade;

    public BallTypeInfo Current => current;

    // Static so callers needn't null-check the manager. Scenes without one play as the default ball.
    public static float PointsMultiplier => Instance != null ? Instance.current.PointsMultiplier : 1f;
    public static float WeightMultiplier => Instance != null ? Instance.current.WeightMultiplier : 1f;
    public static float SpeedMultiplier => Instance != null ? Instance.current.SpeedMultiplier : 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureExists();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureExists();

    private static void EnsureExists()
    {
        if (FindAnyObjectByType<BallTypeManager>() != null || FindAnyObjectByType<ScoreManager>() == null)
            return;

        new GameObject("BallTypeManager").AddComponent<BallTypeManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        screen = new GameObject("BallTypeSelectScreen").AddComponent<BallTypeSelectScreen>();
        screen.transform.SetParent(transform, false);
        screen.Build(BallTypeInfo.All, Select);

        BuildSplitLabel();

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

        choiceMade = true;
        current = BallTypeInfo.Get(type);

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
        if (current.Type == BallType.Default || ball == null)
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
        if (splitLabel != null)
            splitLabel.gameObject.SetActive(active);

        if (!active)
            return;

        // Time stops for the pause menu, level-up choice and game over, so one check covers them all.
        if (Time.timeScale <= 0f)
            return;

        float wait = nextSplitTime - Time.time;
        if (splitLabel != null)
        {
            splitLabel.text = wait > 0f
                ? $"[{BallTypeInfo.SplitKey}] Split in {wait:0.0}s"
                : $"[{BallTypeInfo.SplitKey}] Split ready";
            splitLabel.color = wait > 0f ? new Color(0.7f, 0.72f, 0.8f) : new Color(0.55f, 0.7f, 1f);
        }

        if (wait > 0f || Keyboard.current == null || !Keyboard.current[BallTypeInfo.SplitKey].wasPressedThisFrame)
            return;

        if (GameObject.FindGameObjectsWithTag("Ball").Length >= maxBalls)
            return;

        // Only start the cooldown if a ball was actually split.
        if (BallSplitter.TrySplit())
            nextSplitTime = Time.time + splitCooldown;
    }

    private void BuildSplitLabel()
    {
        GameObject canvasObject = new GameObject("SplitHudCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        splitLabel = BallTypeUI.CreateText("SplitLabel", canvasObject.transform, string.Empty, 36, Color.white, FontStyles.Bold);
        RectTransform rect = splitLabel.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 40f);
        rect.sizeDelta = new Vector2(700f, 60f);
        splitLabel.gameObject.SetActive(false);
    }
}
