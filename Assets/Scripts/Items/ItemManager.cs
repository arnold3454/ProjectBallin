using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Holds the player's one-time use items. Each slot has its own key, and an
/// item is removed from its slot once it has been used. Items stay in the slot
/// they were added to, so each key always uses the item shown in that slot.
/// </summary>
public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance
    {
        get; private set;
    }

    [Header("Items")]
    [Tooltip("Every item the player can start with or find in a pickup.")]
    [SerializeField] private ItemDefinition[] itemPool;
    [SerializeField, Min(1)] private int slotCount = 3;
    [Tooltip("Number of random items the player starts the game with.")]
    [SerializeField, Min(0)] private int startingItems = 1;

    [Header("Controls")]
    [Tooltip("Key that uses each slot's item, in slot order.")]
    [SerializeField] private Key[] slotKeys = { Key.Digit1, Key.Digit2, Key.Digit3 };

    private ItemDefinition[] slots;

    /// <summary>Raised with the slot index when an item is added.</summary>
    public event Action<int, ItemDefinition> ItemAdded;
    /// <summary>Raised with the slot index after an item has been used.</summary>
    public event Action<int, ItemDefinition> ItemUsed;
    /// <summary>Raised with the slot index when an item can't be used right now. The player keeps it.</summary>
    public event Action<int, ItemDefinition> ItemUseFailed;

    public int SlotCount => slotCount;
    public bool IsFull => FindEmptySlot() < 0;

    private void Awake()
    {
        Instance = this;
        slots = new ItemDefinition[slotCount];
    }

    private void Start()
    {
        for (int i = 0; i < startingItems; i++)
            TryAddItem(GetRandomItem());
    }

    private void Update()
    {
        if (Keyboard.current == null || !CanUseItems())
            return;

        for (int i = 0; i < slotCount && i < slotKeys.Length; i++)
        {
            if (slotKeys[i] != Key.None && Keyboard.current[slotKeys[i]].wasPressedThisFrame)
                UseSlot(i);
        }
    }

    public ItemDefinition GetItem(int slot)
    {
        return slots != null && slot >= 0 && slot < slots.Length ? slots[slot] : null;
    }

    /// <summary>A random item from the pool, or null if the pool is empty.</summary>
    public ItemDefinition GetRandomItem()
    {
        if (itemPool == null || itemPool.Length == 0)
            return null;

        return itemPool[UnityEngine.Random.Range(0, itemPool.Length)];
    }

    /// <summary>Puts the item in the first free slot. Returns false if the item is null or every slot is taken.</summary>
    public bool TryAddItem(ItemDefinition item)
    {
        int slot = FindEmptySlot();
        if (item == null || slot < 0)
            return false;

        slots[slot] = item;
        ItemAdded?.Invoke(slot, item);
        return true;
    }

    /// <summary>Uses the item in the given slot. Returns true if the item was used.</summary>
    public bool UseSlot(int slot)
    {
        ItemDefinition item = GetItem(slot);
        if (item == null || !CanUseItems())
            return false;

        if (!item.Use())
        {
            ItemUseFailed?.Invoke(slot, item);
            return false;
        }

        slots[slot] = null;
        ItemUsed?.Invoke(slot, item);
        return true;
    }

    /// <summary>Name of the key that uses the given slot, e.g. "1".</summary>
    public string GetKeyLabel(int slot)
    {
        if (slot < 0 || slot >= slotKeys.Length || slotKeys[slot] == Key.None)
            return string.Empty;

        return Keyboard.current != null
            ? Keyboard.current[slotKeys[slot]].displayName
            : slotKeys[slot].ToString();
    }

    private static bool CanUseItems()
    {
        // Items can't be used while the pause menu is open or after the clock runs out.
        if (Time.timeScale <= 0f)
            return false;

        return GameTimer.Instance == null || !GameTimer.Instance.IsGameOver;
    }

    private int FindEmptySlot()
    {
        if (slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                return i;
        }

        return -1;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
