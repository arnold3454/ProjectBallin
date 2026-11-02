using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The dealer's shop, shown after every boss the player beats. Continue on the win screen
/// opens it; Next Round in the shop starts the next round. It sells three one-time use
/// items (from the ItemManager's pool) and one upgrade (from the UpgradeManager's list),
/// paid for with score. Prices scale the same way the XP bar does: base cost times the XP
/// growth per level, raised to the player's upgrade level. Stock is rerolled every visit
/// and each offer can be bought once. Gameplay stays paused while the shop is open.
/// Lives on the GameManager; the ShopPanel and its cards live under the Canvas.
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Stock")]
    [Tooltip("Base price of a one-time use item, before level scaling.")]
    [SerializeField, Min(0)] private int itemBaseCost = 10;
    [Tooltip("Base price of an upgrade, before level scaling.")]
    [SerializeField, Min(0)] private int upgradeBaseCost = 25;

    [Header("Shop UI")]
    [Tooltip("The ShopPanel under the Canvas. Hidden until a boss is beaten.")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [Tooltip("Feedback line: what was bought, or why a purchase failed.")]
    [SerializeField] private TextMeshProUGUI messageText;
    [Tooltip("One card per item for sale, in order. Keys 1-3 buy them.")]
    [SerializeField] private ShopCardUI[] itemCards = new ShopCardUI[3];
    [Tooltip("The card that sells the upgrade. Key 4 buys it.")]
    [SerializeField] private ShopCardUI upgradeCard;
    [Tooltip("The panel's Next Round button. Its On Click calls Leave, wired in the Inspector.")]
    [SerializeField] private Button leaveButton;

    [Header("Controls")]
    [Tooltip("Keys that buy each card, in card order: the three items then the upgrade.")]
    [SerializeField] private Key[] buyKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4 };

    [Header("Audio")]
    [Tooltip("Played when the shop opens.")]
    [SerializeField] private AudioClip enterSound;
    [SerializeField, Range(0f, 1f)] private float enterVolume = 1f;
    [Tooltip("Played when the player leaves the shop (Next Round).")]
    [SerializeField] private AudioClip leaveSound;
    [SerializeField, Range(0f, 1f)] private float leaveVolume = 1f;
    [Tooltip("Played when the player successfully buys an item or upgrade.")]
    [SerializeField] private AudioClip buySound;
    [SerializeField, Range(0f, 1f)] private float buyVolume = 1f;

    private const string ItemHeader = "ITEM";
    private const string UpgradeHeader = "UPGRADE";

    /// <summary>One thing for sale. TryTake hands it over and returns null, or the reason it couldn't.</summary>
    private class Offer
    {
        public string Name;
        public string Description;
        public int Cost;
        public Func<string> TryTake;
        public bool Sold;
    }

    private readonly List<Offer> offers = new List<Offer>();
    private readonly List<ShopCardUI> cards = new List<ShopCardUI>();
    private Action onClosed;
    private AudioSource audioSource;

    /// <summary>True while the shop is on screen.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Raised with the offer's name after every purchase.</summary>
    public event Action<string> Purchased;

    /// <summary>Price of an item at the player's current level.</summary>
    public int ItemCost => ScaleCost(itemBaseCost);

    /// <summary>Price of an upgrade at the player's current level.</summary>
    public int UpgradeCost => ScaleCost(upgradeBaseCost);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound

        // The Next Round button's On Click is wired to Leave in the Inspector.
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!IsOpen || Keyboard.current == null)
            return;

        for (int i = 0; i < buyKeys.Length && i < cards.Count; i++)
        {
            if (buyKeys[i] != Key.None && Keyboard.current[buyKeys[i]].wasPressedThisFrame)
                Buy(i);
        }
    }

    /// <summary>
    /// Rolls fresh stock and shows the shop. <paramref name="closed"/> runs when the player
    /// presses Next Round. Returns false, without opening, when there is no panel to show.
    /// </summary>
    public bool TryOpen(Action closed)
    {
        if (shopPanel == null)
        {
            Debug.LogWarning("ShopManager has no shop panel assigned (the ShopPanel under the Canvas), so the shop is skipped.", this);
            return false;
        }

        onClosed = closed;
        IsOpen = true;
        Time.timeScale = 0f;

        RollStock();
        SetMessage(string.Empty);
        shopPanel.SetActive(true);
        Refresh();

        PlaySound(enterSound, enterVolume);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        return true;
    }

    /// <summary>Closes the shop and hands control back to whoever opened it, so the next round starts.</summary>
    public void Leave()
    {
        if (!IsOpen)
            return;

        IsOpen = false;
        if (shopPanel != null)
            shopPanel.SetActive(false);

        PlaySound(leaveSound, leaveVolume);

        Action closed = onClosed;
        onClosed = null;
        closed?.Invoke();
    }

    /// <summary>Scales a base cost with the XP bar's growth curve, so prices rise with the player's level.</summary>
    private static int ScaleCost(int baseCost)
    {
        return UpgradeManager.Instance != null ? UpgradeManager.Instance.ScaleWithLevel(baseCost) : baseCost;
    }

    private void RollStock()
    {
        offers.Clear();
        cards.Clear();

        // Three items, as different as the pool allows.
        List<ItemDefinition> pool = new List<ItemDefinition>();
        if (ItemManager.Instance != null)
        {
            foreach (ItemDefinition item in ItemManager.Instance.ItemPool)
            {
                if (item != null)
                    pool.Add(item);
            }
        }
        Shuffle(pool);

        for (int i = 0; i < itemCards.Length; i++)
        {
            cards.Add(itemCards[i]);
            if (pool.Count == 0)
            {
                offers.Add(null);
                continue;
            }

            ItemDefinition item = pool[i % pool.Count];
            offers.Add(new Offer
            {
                Name = item.DisplayName,
                Description = item.Description,
                Cost = ItemCost,
                TryTake = () => TakeItem(item),
            });
        }

        // One upgrade, chosen from whatever isn't maxed out.
        cards.Add(upgradeCard);
        UpgradeDefinition upgrade = null;
        if (UpgradeManager.Instance != null)
        {
            List<UpgradeDefinition> available = UpgradeManager.Instance.GetAvailableUpgrades();
            if (available.Count > 0)
                upgrade = available[UnityEngine.Random.Range(0, available.Count)];
        }

        if (upgrade == null)
        {
            offers.Add(null);
        }
        else
        {
            int owned = UpgradeManager.Instance.StacksOf(upgrade);
            offers.Add(new Offer
            {
                Name = upgrade.Name,
                Description = $"{upgrade.Description}\n\nTotal bonus: {upgrade.TotalAt(owned)} → {upgrade.TotalAt(owned + 1)}",
                Cost = UpgradeCost,
                TryTake = () => TakeUpgrade(upgrade),
            });
        }
    }

    private void Buy(int index)
    {
        if (index < 0 || index >= offers.Count)
            return;

        Offer offer = offers[index];
        if (offer == null || offer.Sold)
            return;

        if (ScoreManager.Instance == null)
        {
            SetMessage("No score to spend.");
            return;
        }

        if (ScoreManager.Instance.CurrentScore < offer.Cost)
        {
            SetMessage($"Not enough score for {offer.Name} ({offer.Cost} needed).");
            return;
        }

        // Make sure the player can actually receive it before taking their score.
        string problem = offer.TryTake();
        if (problem != null)
        {
            SetMessage(problem);
            return;
        }

        ScoreManager.Instance.TrySpendScore(offer.Cost);
        offer.Sold = true;
        PlaySound(buySound, buyVolume);
        SetMessage($"Bought {offer.Name}!");
        Purchased?.Invoke(offer.Name);
        Refresh();
    }

    /// <summary>Gives the item to the player. Returns the reason it couldn't, or null on success.</summary>
    private static string TakeItem(ItemDefinition item)
    {
        if (ItemManager.Instance == null)
            return "There is nowhere to keep items.";

        if (!ItemManager.Instance.TryAddItem(item))
            return "Your item slots are full.";

        return null;
    }

    /// <summary>Applies the upgrade. Returns the reason it couldn't, or null on success.</summary>
    private static string TakeUpgrade(UpgradeDefinition upgrade)
    {
        if (UpgradeManager.Instance == null)
            return "Upgrades aren't available.";

        if (!UpgradeManager.Instance.ApplyUpgrade(upgrade))
            return $"{upgrade.Name} is already maxed out.";

        return null;
    }

    private void Refresh()
    {
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
        if (scoreText != null)
            scoreText.text = $"Score: {score}";

        for (int i = 0; i < cards.Count; i++)
        {
            ShopCardUI card = cards[i];
            if (card == null)
                continue;

            Offer offer = offers[i];
            bool isItem = i < itemCards.Length;
            string header = isItem ? ItemHeader : UpgradeHeader;
            if (offer == null)
            {
                card.ShowEmpty(header, isItem ? "No items in the pool." : "Every upgrade is maxed out.");
            }
            else if (offer.Sold)
            {
                card.ShowSold();
            }
            else
            {
                int index = i;
                card.Show(header, offer.Name, offer.Description, offer.Cost, score >= offer.Cost, () => Buy(index));
            }
        }
    }

    private void SetMessage(string text)
    {
        if (messageText != null)
            messageText.text = text;
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, volume);
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}