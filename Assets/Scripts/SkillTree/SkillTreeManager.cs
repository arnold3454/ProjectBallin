using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SkillEffect
{
    None,
    BallBounciness,
    NudgeStamina,
    AdditionalTime,
    NudgeStrength,
    Damage,
    ItemEffect,
    Split,
    DamageOverTime,
    Glass,
    Critical,
}

/// <summary>Owns the optional between-round skill tree and its Inspector-authored nodes and UI.</summary>
public sealed class SkillTreeManager : MonoBehaviour
{
    [Serializable]
    private sealed class Node
    {
        [SerializeField] public string id;
        [SerializeField] public string title;
        [SerializeField, TextArea] public string description;
        [SerializeField] public SkillEffect effect;
        [SerializeField] public string[] prerequisites;
        [SerializeField] public int[] rankCosts;
        [SerializeField] public float[] rankValues;
        [SerializeField] public bool startNode;
        [SerializeField] public SkillTreeNodeView view;

        public string Id => id;
        public string Title => title;
        public string Description => description;
        public SkillEffect Effect => effect;
        public string[] Prerequisites => prerequisites;
        public int[] RankCosts => rankCosts;
        public float[] RankValues => rankValues;
        public bool IsStartNode => startNode;
        public SkillTreeNodeView View => view;
    }

    [Header("Tree UI")]
    [Tooltip("Pre-authored tree panel in the scene. The panel is shown after a successful round.")]
    [SerializeField] private GameObject panelRoot;
    [Tooltip("Pre-authored button that closes the tree and continues to the win screen.")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI tokenText;
    [SerializeField] private string tokenFormat = "Skill tokens: {0}";

    [Header("Nodes")]
    [Tooltip("Create and position each node's UI in the scene, then assign its SkillTreeNodeView here.")]
    [SerializeField] private Node[] nodes = CreateDefaultNodes();

    [Header("Corner Skill Tuning")]
    [SerializeField, Range(0f, 1f)] private float glassShatterChance = 0.05f;
    [SerializeField, Min(0f)] private float dotTickInterval = 1f;
    [SerializeField, Min(1)] private int dotTickCount = 9;
    [SerializeField, Min(0f)] private float dotInitialDamageFraction = 0.25f;
    [SerializeField, Min(0f)] private float dotTickDamageFraction = 0.1f;
    [SerializeField, Range(0f, 1f)] private float criticalChance = 0.25f;
    [SerializeField, Min(1f)] private float criticalMultiplier = 2f;

    private readonly Dictionary<string, int> nodeIndexById = new Dictionary<string, int>();
    private int[] ranks;
    private int tokensEarned;
    private int tokensSpent;
    private int lastObservedXpLevel;
    private bool xpLevelObserved;
    private bool subscribed;
    private UnityEngine.Events.UnityAction continueHandler;

    public static SkillTreeManager Instance { get; private set; }
    public bool IsOpen { get; private set; }
    public static bool IsSkillTreeOpen => Instance != null && Instance.IsOpen;
    public int AvailableTokens => Mathf.Max(0, tokensEarned - tokensSpent);
    public int TotalRanks { get; private set; }

    private bool IsSelectedSystem =>
        ProgressionManager.Instance != null && ProgressionManager.Instance.UseSkillTreeSystem;

    public static bool IsSkillTreeActive =>
        Instance != null && Instance.IsSelectedSystem;

    public static float DamageMultiplier =>
        IsSkillTreeActive
            ? (1f + Instance.GetEffectValue(SkillEffect.Damage)) * (Instance.HasEffect(SkillEffect.Glass) ? 1.25f : 1f)
            : 1f;

    public static float ScoreMultiplier =>
        IsSkillTreeActive && Instance.HasEffect(SkillEffect.Glass) ? 1.25f : 1f;

    public static float ItemEffectMultiplier =>
        IsSkillTreeActive ? 1f + Instance.GetEffectValue(SkillEffect.ItemEffect) : 1f;

    public static float BouncinessMultiplier =>
        IsSkillTreeActive ? 1f + Instance.GetEffectValue(SkillEffect.BallBounciness) : 1f;

    public static int NudgeStaminaBonus =>
        IsSkillTreeActive ? Mathf.RoundToInt(Instance.GetEffectValue(SkillEffect.NudgeStamina)) : 0;

    public static float NudgeStrengthBonus =>
        IsSkillTreeActive ? Instance.GetEffectValue(SkillEffect.NudgeStrength) : 0f;

    public static float NudgeCostMultiplier =>
        IsSkillTreeActive ? 1f + Instance.GetEffectRanks(SkillEffect.NudgeStrength) * 0.1f : 1f;

    public static int NudgeStrengthRanks =>
        IsSkillTreeActive ? Instance.GetEffectRanks(SkillEffect.NudgeStrength) : 0;

    public static float AdditionalTime =>
        IsSkillTreeActive ? Instance.GetEffectValue(SkillEffect.AdditionalTime) : 0f;

    public static float CriticalChance =>
        IsSkillTreeActive && Instance.HasEffect(SkillEffect.Critical) ? Instance.criticalChance : 0f;

    public static float CriticalMultiplier =>
        IsSkillTreeActive && Instance.HasEffect(SkillEffect.Critical) ? Instance.criticalMultiplier : 1f;

    public static bool SplitUnlocked => IsSkillTreeActive && Instance.HasEffect(SkillEffect.Split);
    public static bool DotUnlocked => IsSkillTreeActive && Instance.HasEffect(SkillEffect.DamageOverTime);
    public static bool GlassUnlocked => IsSkillTreeActive && Instance.HasEffect(SkillEffect.Glass);
    public static bool HasBallImpactEffects => DotUnlocked || GlassUnlocked;
    public static float GlassShatterChance => IsSkillTreeActive ? Instance.glassShatterChance : 0f;
    public static float DotTickInterval => IsSkillTreeActive ? Instance.dotTickInterval : 1f;
    public static int DotTickCount => IsSkillTreeActive ? Instance.dotTickCount : 0;
    public static float DotInitialDamageFraction => IsSkillTreeActive ? Instance.dotInitialDamageFraction : 0f;
    public static float DotTickDamageFraction => IsSkillTreeActive ? Instance.dotTickDamageFraction : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (nodes == null)
            nodes = Array.Empty<Node>();

        ranks = new int[nodes != null ? nodes.Length : 0];
        BuildNodeIndex();

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] == null)
                continue;

            if (nodes[i].IsStartNode)
                ranks[i] = 1;

            if (nodes[i].View != null)
            {
                int nodeIndex = i;
                nodes[i].View.Bind(() => Purchase(nodeIndex));
            }
        }

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            Debug.LogWarning("SkillTreeManager has no tree panel assigned.", this);

        if (continueButton != null)
        {
            continueHandler = Close;
            continueButton.onClick.AddListener(continueHandler);
        }

        RecountRanks();
        RefreshViews();
    }

    private void OnEnable()
    {
        TrySubscribeToXpLevels();
    }

    private void Start()
    {
        TrySubscribeToXpLevels();
    }

    private void OnDisable()
    {
        UnsubscribeFromXpLevels();
    }

    private void OnDestroy()
    {
        UnsubscribeFromXpLevels();

        if (continueButton != null && continueHandler != null)
            continueButton.onClick.RemoveListener(continueHandler);

        if (Instance == this)
            Instance = null;
    }

    /// <summary>Opens the tree when the player has earned tokens; returns false if there is nothing to show.</summary>
    public bool TryOpenForRound()
    {
        if (!IsSelectedSystem || AvailableTokens <= 0 || IsOpen)
            return false;

        if (panelRoot == null)
        {
            Debug.LogError("SkillTreeManager cannot open the skill tree without a panel assigned.", this);
            return false;
        }

        if (continueButton == null)
        {
            Debug.LogError("SkillTreeManager cannot open the skill tree without a Continue button assigned.", this);
            return false;
        }

        RefreshViews();
        IsOpen = true;
        panelRoot.SetActive(true);
        Time.timeScale = 0f;
        return true;
    }

    /// <summary>Closes the tree and returns control to the round-win flow.</summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (GameTimer.Instance == null || !GameTimer.Instance.IsGameOver)
            Time.timeScale = 1f;
    }

    public bool HasEffect(SkillEffect effect)
    {
        if (nodes == null || ranks == null)
            return false;

        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].Effect == effect && ranks[i] > 0)
                return true;
        }

        return false;
    }

    public static bool RollCritical(out float multiplier)
    {
        multiplier = CriticalMultiplier;
        float chance = CriticalChance;
        return chance > 0f && UnityEngine.Random.value < chance;
    }

    public void ApplyDamageOverTime(Enemy enemy)
    {
        if (enemy != null && DotUnlocked)
            StartCoroutine(ApplyDamageOverTimeToEnemy(enemy));
    }

    public void ApplyDamageOverTime(Boss boss)
    {
        if (boss != null && DotUnlocked)
            StartCoroutine(ApplyDamageOverTimeToBoss(boss));
    }

    private IEnumerator ApplyDamageOverTimeToEnemy(Enemy enemy)
    {
        enemy.ApplySkillDamage(dotInitialDamageFraction);

        for (int i = 0; i < dotTickCount; i++)
        {
            yield return new WaitForSeconds(dotTickInterval);
            if (enemy == null || enemy.Health <= 0)
                yield break;

            enemy.ApplySkillDamage(dotTickDamageFraction);
        }
    }

    private IEnumerator ApplyDamageOverTimeToBoss(Boss boss)
    {
        boss.ApplySkillDamage(dotInitialDamageFraction);

        for (int i = 0; i < dotTickCount; i++)
        {
            yield return new WaitForSeconds(dotTickInterval);
            if (boss == null || boss.IsDead)
                yield break;

            boss.ApplySkillDamage(dotTickDamageFraction);
        }
    }

    private void TrySubscribeToXpLevels()
    {
        if (subscribed || UpgradeManager.Instance == null)
            return;

        UpgradeManager manager = UpgradeManager.Instance;
        manager.XpLevelCompleted += OnXpLevelCompleted;
        subscribed = true;

        int currentLevel = manager.UpgradeLevel;
        if (!xpLevelObserved)
        {
            tokensEarned = currentLevel;
            xpLevelObserved = true;
        }
        else if (currentLevel > lastObservedXpLevel)
        {
            tokensEarned += currentLevel - lastObservedXpLevel;
        }

        lastObservedXpLevel = currentLevel;
        RefreshViews();
    }

    private void UnsubscribeFromXpLevels()
    {
        if (!subscribed)
            return;

        if (UpgradeManager.Instance != null)
            UpgradeManager.Instance.XpLevelCompleted -= OnXpLevelCompleted;
        subscribed = false;
    }

    private void OnXpLevelCompleted(int level)
    {
        if (level <= lastObservedXpLevel)
            return;

        tokensEarned += level - lastObservedXpLevel;
        lastObservedXpLevel = level;
        RefreshViews();
    }

    private void BuildNodeIndex()
    {
        nodeIndexById.Clear();
        if (nodes == null)
            return;

        for (int i = 0; i < nodes.Length; i++)
        {
            Node node = nodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.Id))
            {
                Debug.LogError($"Skill tree node at index {i} has no ID.", this);
                continue;
            }

            node.prerequisites ??= Array.Empty<string>();
            node.rankCosts ??= Array.Empty<int>();
            node.rankValues ??= Array.Empty<float>();

            if (nodeIndexById.ContainsKey(node.Id))
                Debug.LogError($"Skill tree contains duplicate node ID '{node.Id}'.", this);
            else
                nodeIndexById.Add(node.Id, i);

            if (!node.IsStartNode && (node.RankCosts == null || node.RankCosts.Length == 0))
                Debug.LogError($"Skill tree node '{node.Id}' needs at least one rank cost.", this);
            else if (!node.IsStartNode && node.RankValues != null &&
                     node.RankValues.Length > 0 && node.RankValues.Length != node.RankCosts.Length)
                Debug.LogWarning($"Skill tree node '{node.Id}' has different rank-cost and rank-value counts.", this);

            if (node.View == null && !node.IsStartNode)
                Debug.LogWarning($"Skill tree node '{node.Id}' has no view assigned.", this);
        }

        for (int i = 0; i < nodes.Length; i++)
        {
            Node node = nodes[i];
            if (node == null || node.IsStartNode || node.Prerequisites == null || node.Prerequisites.Length == 0)
                continue;

            foreach (string prerequisite in node.Prerequisites)
            {
                if (!nodeIndexById.ContainsKey(prerequisite))
                    Debug.LogError($"Skill tree node '{node.Id}' references missing prerequisite '{prerequisite}'.", this);
            }
        }
    }

    private bool Purchase(int index)
    {
        if (!IsSelectedSystem || index < 0 || index >= nodes.Length || nodes[index] == null || !CanPurchase(index))
            return false;

        Node node = nodes[index];
        int cost = node.RankCosts[ranks[index]];
        tokensSpent += cost;
        ranks[index]++;
        RecountRanks();
        RefreshViews();

        if (NudgeController.Instance != null)
            NudgeController.Instance.RefreshUpgrades();
        if (BallTypeManager.Instance != null)
            BallTypeManager.Instance.RefreshSkillEffects();

        return true;
    }

    private bool CanPurchase(int index)
    {
        Node node = nodes[index];
        if (node == null || node.IsStartNode || node.RankCosts == null || ranks[index] >= node.RankCosts.Length)
            return false;

        int nextCost = Mathf.Max(0, node.RankCosts[ranks[index]]);
        return AvailableTokens >= nextCost && HasUnlockedPrerequisite(node);
    }

    private bool HasUnlockedPrerequisite(Node node)
    {
        if (node.Prerequisites == null || node.Prerequisites.Length == 0)
            return false;

        foreach (string prerequisite in node.Prerequisites)
        {
            if (nodeIndexById.TryGetValue(prerequisite, out int index) && ranks[index] > 0)
                return true;
        }

        return false;
    }

    private float GetEffectValue(SkillEffect effect)
    {
        float total = 0f;
        for (int i = 0; i < nodes.Length; i++)
        {
            Node node = nodes[i];
            if (node == null)
                continue;

            if (node.Effect != effect || ranks[i] <= 0 || node.RankValues == null || node.RankValues.Length == 0)
                continue;

            int valueIndex = Mathf.Min(ranks[i] - 1, node.RankValues.Length - 1);
            total += node.RankValues[valueIndex];
        }

        return total;
    }

    private int GetEffectRanks(SkillEffect effect)
    {
        int total = 0;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i] != null && nodes[i].Effect == effect)
                total += ranks[i];
        }

        return total;
    }

    private void RecountRanks()
    {
        TotalRanks = 0;
        if (ranks == null)
            return;

        for (int i = 0; i < ranks.Length; i++)
        {
            if (nodes[i] != null && !nodes[i].IsStartNode)
                TotalRanks += ranks[i];
        }
    }

    private void RefreshViews()
    {
        if (tokenText != null)
            tokenText.text = string.Format(tokenFormat, AvailableTokens);

        if (nodes == null || ranks == null)
            return;

        for (int i = 0; i < nodes.Length; i++)
        {
            Node node = nodes[i];
            if (node == null || node.View == null)
                continue;

            bool unlocked = HasUnlockedPrerequisite(node);
            bool purchased = ranks[i] > 0;
            int maxRanks = node.RankCosts != null ? node.RankCosts.Length : 0;
            bool maxed = ranks[i] >= maxRanks;
            bool canPurchase = CanPurchase(i);
            string rank = node.IsStartNode ? "START" : $"Rank {ranks[i]}/{maxRanks}";
            string state;

            if (node.IsStartNode)
                state = "Unlocked";
            else if (maxed)
                state = "Maxed";
            else if (!unlocked)
                state = "Locked";
            else
                state = $"Cost: {Mathf.Max(0, node.RankCosts[ranks[i]])} token(s)";

            string description = BuildNodeDescription(node, ranks[i]);
            node.View.SetState(
                node.Title,
                description,
                rank,
                state,
                canPurchase,
                unlocked,
                purchased);
        }
    }

    private static string BuildNodeDescription(Node node, int rank)
    {
        if (node.RankValues == null || node.RankValues.Length == 0)
            return node.Description;

        string current = rank > 0
            ? FormatRankValue(node.Effect, node.RankValues[Mathf.Min(rank - 1, node.RankValues.Length - 1)])
            : "None";
        string next = rank < node.RankValues.Length
            ? FormatRankValue(node.Effect, node.RankValues[rank])
            : "Max";

        return $"{node.Description}\nCurrent: {current}  |  Next: {next}";
    }

    private static string FormatRankValue(SkillEffect effect, float value)
    {
        switch (effect)
        {
            case SkillEffect.BallBounciness:
            case SkillEffect.NudgeStrength:
            case SkillEffect.Damage:
            case SkillEffect.ItemEffect:
                return $"+{value:P0}";
            case SkillEffect.AdditionalTime:
                return $"+{value:0} seconds";
            case SkillEffect.NudgeStamina:
                return $"+{value:0} nudges";
            default:
                return value.ToString("0.##");
        }
    }

    private static Node[] CreateDefaultNodes()
    {
        return new[]
        {
            new Node
            {
                id = "start",
                title = "Start",
                description = "Starting point",
                effect = SkillEffect.None,
                startNode = true,
                rankCosts = Array.Empty<int>(),
                rankValues = Array.Empty<float>(),
                prerequisites = Array.Empty<string>(),
            },
            new Node
            {
                id = "bounciness",
                title = "Ball Bounciness",
                description = "Increases the ball's bounciness.",
                effect = SkillEffect.BallBounciness,
                prerequisites = new[] { "start", "nudge-strength-left", "nudge-stamina-top" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 0.05f, 0.10f, 0.15f, 0.25f, 0.40f },
            },
            new Node
            {
                id = "nudge-stamina-top",
                title = "Increased Nudge Stamina",
                description = "Adds nudges before the table tilts.",
                effect = SkillEffect.NudgeStamina,
                prerequisites = new[] { "start", "bounciness", "additional-time" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 1f, 2f, 3f, 4f, 5f },
            },
            new Node
            {
                id = "additional-time",
                title = "Additional Time",
                description = "Adds time to the game clock.",
                effect = SkillEffect.AdditionalTime,
                prerequisites = new[] { "start", "nudge-stamina-top", "nudge-strength-right" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 5f, 10f, 15f, 25f, 40f },
            },
            new Node
            {
                id = "nudge-strength-left",
                title = "Increased Nudge Strength",
                description = "Increases nudge force.",
                effect = SkillEffect.NudgeStrength,
                prerequisites = new[] { "start", "bounciness", "damage" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 0.2f, 0.4f, 0.6f, 0.8f, 1f },
            },
            new Node
            {
                id = "nudge-strength-right",
                title = "Increased Nudge Strength",
                description = "Increases nudge force.",
                effect = SkillEffect.NudgeStrength,
                prerequisites = new[] { "start", "additional-time", "item-effect" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 0.2f, 0.4f, 0.6f, 0.8f, 1f },
            },
            new Node
            {
                id = "damage",
                title = "Increased Damage",
                description = "Increases damage dealt.",
                effect = SkillEffect.Damage,
                prerequisites = new[] { "start", "nudge-strength-left", "nudge-stamina-bottom" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 0.025f, 0.05f, 0.10f, 0.15f, 0.25f },
            },
            new Node
            {
                id = "item-effect",
                title = "Increased Item Effect",
                description = "Increases the effectiveness of items.",
                effect = SkillEffect.ItemEffect,
                prerequisites = new[] { "start", "nudge-strength-right", "nudge-stamina-bottom" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 0.05f, 0.10f, 0.15f, 0.25f, 0.50f },
            },
            new Node
            {
                id = "nudge-stamina-bottom",
                title = "Increased Nudge Stamina",
                description = "Adds nudges before the table tilts.",
                effect = SkillEffect.NudgeStamina,
                prerequisites = new[] { "start", "damage", "item-effect" },
                rankCosts = new[] { 1, 1, 1, 1, 1 },
                rankValues = new[] { 1f, 2f, 3f, 4f, 5f },
            },
            new Node
            {
                id = "split",
                title = "Split Ball",
                description = "All balls can split into two using the Split key. The Split Ball cooldown is adjustable on BallTypeManager.",
                effect = SkillEffect.Split,
                prerequisites = new[] { "bounciness" },
                rankCosts = new[] { 2 },
                rankValues = Array.Empty<float>(),
            },
            new Node
            {
                id = "dot",
                title = "DOT Ball",
                description = "Hits deal 25% extra damage and apply 10% damage each second for 9 ticks to enemies and the boss.",
                effect = SkillEffect.DamageOverTime,
                prerequisites = new[] { "additional-time" },
                rankCosts = new[] { 2 },
                rankValues = Array.Empty<float>(),
            },
            new Node
            {
                id = "glass",
                title = "Glass Ball",
                description = "All balls gain 25% damage and score, with a 5% chance to shatter on impact.",
                effect = SkillEffect.Glass,
                prerequisites = new[] { "damage" },
                rankCosts = new[] { 2 },
                rankValues = Array.Empty<float>(),
            },
            new Node
            {
                id = "critical",
                title = "Critical Ball",
                description = "All balls have a 25% critical chance for 2x damage and score.",
                effect = SkillEffect.Critical,
                prerequisites = new[] { "item-effect" },
                rankCosts = new[] { 2 },
                rankValues = Array.Empty<float>(),
            },
        };
    }
}
