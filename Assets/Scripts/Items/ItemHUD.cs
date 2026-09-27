using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Shows the player's item slots and a short message whenever an item is
/// picked up or used. Builds one slot from the prefab for each inventory slot.
/// </summary>
public class ItemHUD : MonoBehaviour
{
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private ItemSlotUI slotPrefab;
    [SerializeField] private Transform slotContainer;

    [Header("Message")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float messageDuration = 1.5f;
    [SerializeField] private float messageFadeTime = 0.4f;

    private readonly List<ItemSlotUI> slots = new List<ItemSlotUI>();
    private float messageTimer;

    private void Awake()
    {
        if (itemManager == null)
            itemManager = FindAnyObjectByType<ItemManager>();

        BuildSlots();
        SetMessageAlpha(0f);
    }

    private void OnEnable()
    {
        if (itemManager == null)
            return;

        itemManager.ItemAdded += OnItemAdded;
        itemManager.ItemUsed += OnItemUsed;
        itemManager.ItemUseFailed += OnItemUseFailed;
        RefreshSlots();
    }

    private void OnDisable()
    {
        if (itemManager == null)
            return;

        itemManager.ItemAdded -= OnItemAdded;
        itemManager.ItemUsed -= OnItemUsed;
        itemManager.ItemUseFailed -= OnItemUseFailed;
    }

    private void Update()
    {
        if (messageTimer <= 0f)
            return;

        // Use unscaled time so the message still fades while the game is paused.
        messageTimer -= Time.unscaledDeltaTime;
        SetMessageAlpha(messageFadeTime > 0f ? Mathf.Clamp01(messageTimer / messageFadeTime) : 0f);
    }

    private void BuildSlots()
    {
        if (itemManager == null || slotPrefab == null || slotContainer == null)
            return;

        for (int i = 0; i < itemManager.SlotCount; i++)
        {
            ItemSlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.name = $"Slot {i + 1}";
            slots.Add(slot);
        }
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].SetKey(itemManager.GetKeyLabel(i));
            slots[i].SetItem(itemManager.GetItem(i));
        }
    }

    private void OnItemAdded(int slot, ItemDefinition item)
    {
        if (slot < slots.Count)
        {
            slots[slot].SetItem(item);
            slots[slot].Pop();
        }

        ShowMessage($"+ {item.DisplayName}", item.Color);
    }

    private void OnItemUsed(int slot, ItemDefinition item)
    {
        if (slot < slots.Count)
            slots[slot].SetItem(null);

        ShowMessage($"{item.DisplayName}!", item.Color);
    }

    private void OnItemUseFailed(int slot, ItemDefinition item)
    {
        ShowMessage($"Can't use {item.DisplayName} right now", Color.white);
    }

    private void ShowMessage(string text, Color color)
    {
        if (messageText == null)
            return;

        messageText.text = text;
        messageText.color = color;
        messageTimer = messageDuration + messageFadeTime;
        SetMessageAlpha(1f);
    }

    private void SetMessageAlpha(float alpha)
    {
        if (messageText != null)
            messageText.alpha = alpha;
    }
}
