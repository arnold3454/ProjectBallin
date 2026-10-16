using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One slot in the item HUD. Shows the item's color, label and name, plus the key that uses it.</summary>
public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image tile;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Look")]
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.12f);
    [SerializeField] private Color emptyKeyColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color filledKeyColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.25f;

    private float popTimer;

    public void SetKey(string key)
    {
        if (keyText != null)
            keyText.text = key;
    }

    public void SetItem(ItemDefinition item)
    {
        bool hasItem = item != null;
        bool hasIcon = hasItem && item.Icon != null;

        if (tile != null)
            tile.color = hasItem ? item.Color : emptyColor;

        if (icon != null)
        {
            icon.sprite = hasIcon ? item.Icon : null;
            icon.enabled = hasIcon;
        }

        if (label != null)
            label.text = hasItem && !hasIcon ? item.ShortLabel : string.Empty;

        if (keyText != null)
            keyText.color = hasItem ? filledKeyColor : emptyKeyColor;

        if (nameText != null)
            nameText.text = hasItem ? item.DisplayName : string.Empty;
    }

    /// <summary>Briefly scales the slot up to draw attention to it.</summary>
    public void Pop()
    {
        popTimer = popDuration;
    }

    private void Update()
    {
        if (popTimer <= 0f)
            return;

        // Use unscaled time so the animation still plays while the game is paused.
        popTimer = Mathf.Max(0f, popTimer - Time.unscaledDeltaTime);
        transform.localScale = Vector3.one * Mathf.Lerp(1f, popScale, popTimer / popDuration);
    }
}
