using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Runs the roulette boss trigger. Bet tunnels set the current bet, the wheel spins when the
/// ball rolls over it with a bet placed, and a winning spin triggers the boss fight and locks
/// the wheel for the rest of the round. Lives on the GameManager with the other managers.
/// </summary>
public class RouletteManager : MonoBehaviour
{
    public static RouletteManager Instance { get; private set; }

    [Header("Odds")]
    [SerializeField, Range(0f, 1f)] private float winChance = 0.5f;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private string noBetMessage = "Bet: none  (run a red or black tunnel)";
    [SerializeField] private string betMessage = "Bet: {0}  (roll over the wheel)";
    [SerializeField] private string spinningMessage = "Spinning...";
    [SerializeField] private string lostMessage = "Lost on {0}!  Bet again";
    [SerializeField] private string wonMessage = "{0} wins!  The dealer stirs...";

    [Header("Events")]
    [Tooltip("Invoked once when a winning spin triggers the boss fight.")]
    [SerializeField] private UnityEvent onBossFightTriggered;

    /// <summary>Raised once when a winning spin triggers the boss fight.</summary>
    public static event Action BossFightTriggered;

    public BetColor CurrentBet { get; private set; }
    public bool IsBossFightTriggered { get; private set; }

    /// <summary>The wheel currently registered in the scene, if any. The boss spawns on it.</summary>
    public RouletteWheel Wheel => wheel;

    private readonly List<BetTunnel> tunnels = new List<BetTunnel>();
    private RouletteWheel wheel;

    private void Awake()
    {
        Instance = this;
        UpdateHud();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

    /// <summary>Forgets the win and the bet and unlocks the wheel, so a new round can spin for the boss again.</summary>
    public void ResetRound()
    {
        IsBossFightTriggered = false;
        CurrentBet = BetColor.None;

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

        BetColor result = UnityEngine.Random.value < winChance ? CurrentBet : Opposite(CurrentBet);
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
