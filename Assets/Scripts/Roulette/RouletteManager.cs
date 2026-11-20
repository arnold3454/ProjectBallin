using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs the roulette boss trigger. Bet tunnels set the current bet, the wheel spins when the
/// ball rolls over it with a bet placed, and a winning spin authorizes a corner trigger. The
/// wheel stays locked until that authorization is used or the round is reset. Every losing spin
/// improves the odds of the next one; spending a win on a corner trigger (or the boss spawning)
/// puts the odds back where they started.
/// </summary>
public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance { get; private set; }

    [Header("Odds")]
    [Tooltip("Chance a spin lands on the player's bet, by how many spins in a row they have lost: the first entry is the opening odds, the second applies after one loss, and so on. Losses past the end of the list keep using the last entry. Goes back to the first entry when a win is spent on a corner trigger or the boss spawns.")]
    [SerializeField, Range(0f, 1f)] private float[] winChances = { 0.5f, 2f / 3f, 0.75f, 1f };

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private string noBetMessage = "Bet: none  (run a red or black tunnel)";
    [SerializeField] private string betMessage = "Bet: {0}  (roll over the wheel)";
    [SerializeField] private string spinningMessage = "Spinning...";
    [SerializeField] private string lostMessage = "Lost on {0}!  Bet again";
    [SerializeField] private string wonMessage = "{0} wins!  The dealer stirs...";

    [Header("Events")]
    [Tooltip("Invoked each time a winning spin authorizes a corner trigger.")]
    [SerializeField] private UnityEvent onBossFightTriggered;

    /// <summary>Raised when a winning spin authorizes a corner trigger.</summary>
    public static event Action BossFightTriggered;

    public BetColor CurrentBet { get; private set; }
    /// <summary>True after winning until the roulette is reset for another corner trigger.</summary>
    public bool IsBossFightTriggered { get; private set; }

    /// <summary>Losing spins since the odds were last reset. Each one improves the odds.</summary>
    public int LossStreak { get; private set; }

    /// <summary>Chance the next spin lands on the player's bet, after the losses so far.</summary>
    public float CurrentWinChance
    {
        get
        {
            if (winChances == null || winChances.Length == 0)
                return 0.5f;

            return winChances[Mathf.Min(LossStreak, winChances.Length - 1)];
        }
    }

    /// <summary>The wheel currently registered in the scene, if any. The boss spawns on it.</summary>
    public RouletteWheel Wheel => wheel;

    private readonly List<BetTunnel> tunnels = new List<BetTunnel>();
    private RouletteWheel wheel;

    private void Awake()
    {
        Instance = this;
        UpdateHud();
    }

    private void OnEnable()
    {
        BossManager.BossFightStarted += ResetOdds;
    }

    private void OnDisable()
    {
        BossManager.BossFightStarted -= ResetOdds;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Puts the odds back to the opening chance. Runs when a win is spent (through
    /// <see cref="ResetRound"/>) and when the boss spawns, which covers the win spent on the last
    /// corner trigger: that one starts the fight instead of resetting the round.
    /// </summary>
    public void ResetOdds()
    {
        LossStreak = 0;
    }

    public void RegisterTunnel(BetTunnel tunnel)
    {
        if (tunnel == null || tunnels.Contains(tunnel))
            return;

        tunnels.Add(tunnel);
        tunnel.SetActive(tunnel.BetColor == CurrentBet);
    }

    public void UnregisterTunnel(BetTunnel tunnel)
    {
        tunnels.Remove(tunnel);
    }

    public void RegisterWheel(RouletteWheel newWheel)
    {
        if (newWheel == null || wheel == newWheel)
            return;

        if (wheel != null)
            wheel.SpinFinished -= OnSpinFinished;

        wheel = newWheel;
        wheel.SpinFinished += OnSpinFinished;
        wheel.ShowBet(CurrentBet);
    }

    public void UnregisterWheel(RouletteWheel oldWheel)
    {
        if (wheel != oldWheel)
            return;

        wheel.SpinFinished -= OnSpinFinished;
        wheel = null;
    }

    /// <summary>
    /// Forgets the win and bet and unlocks the wheel for another trigger or a new round. The odds
    /// go back to the opening chance, so the spin after a spent win starts fresh.
    /// </summary>
    public void ResetRound()
    {
        IsBossFightTriggered = false;
        CurrentBet = BetColor.None;
        ResetOdds();

        if (wheel != null)
            wheel.Unlock();

        RefreshBetVisuals();
        UpdateHud();
    }

    /// <summary>Called by a bet tunnel when the ball runs through it.</summary>
    public void PlaceBet(BetColor color)
    {
        if (IsBossFightTriggered || color == BetColor.None)
            return;

        if (wheel != null && wheel.IsSpinning)
            return;

        CurrentBet = color;
        RefreshBetVisuals();
        UpdateHud();
    }

    /// <summary>Called by the wheel when the ball rolls over it.</summary>
    public void WheelHit(RouletteWheel hitWheel)
    {
        if (IsBossFightTriggered || hitWheel == null || hitWheel.IsSpinning)
            return;

        if (CurrentBet == BetColor.None)
        {
            UpdateHud();
            return;
        }

        // Random.value can come back as exactly 1, so a chance of 1 is checked on its own to stay a sure win.
        float chance = CurrentWinChance;
        bool won = chance >= 1f || UnityEngine.Random.value < chance;
        BetColor result = won ? CurrentBet : Opposite(CurrentBet);
        SetHud(spinningMessage);
        hitWheel.Spin(result);
    }

    private void OnSpinFinished(BetColor result)
    {
        if (result == CurrentBet)
        {
            IsBossFightTriggered = true;
            if (wheel != null)
                wheel.Lock();

            SetHud(string.Format(wonMessage, result.ToString().ToUpperInvariant()));
            Debug.Log("Roulette won: boss fight triggered.");
            onBossFightTriggered?.Invoke();
            BossFightTriggered?.Invoke();
            return;
        }

        // A loss makes the next spin more likely to land on the player's bet.
        LossStreak++;
        CurrentBet = BetColor.None;
        RefreshBetVisuals();
        SetHud(string.Format(lostMessage, result.ToString().ToUpperInvariant()));
    }

    private void RefreshBetVisuals()
    {
        foreach (BetTunnel tunnel in tunnels)
        {
            if (tunnel != null)
                tunnel.SetActive(tunnel.BetColor == CurrentBet);
        }

        if (wheel != null)
            wheel.ShowBet(CurrentBet);
    }

    private void UpdateHud()
    {
        if (IsBossFightTriggered)
            return;

        SetHud(CurrentBet == BetColor.None
            ? noBetMessage
            : string.Format(betMessage, CurrentBet.ToString().ToUpperInvariant()));
    }

    private void SetHud(string message)
    {
        if (betText != null)
            betText.text = message;
    }

    private static BetColor Opposite(BetColor color)
    {
        switch (color)
        {
            case BetColor.Red: return BetColor.Black;
            case BetColor.Black: return BetColor.Red;
            default: return BetColor.None;
        }
    }
}
