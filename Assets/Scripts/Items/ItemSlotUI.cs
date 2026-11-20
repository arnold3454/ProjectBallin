using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>One slot in the item HUD. Shows the item's color, label and name, plus the key that uses it.</summary>
public class ItemSlotUI : MonoBehaviour
{

    [SerializeField] private CanvasGroup ticketCanvasGroup;
    [SerializeField] private Vector2 dispenseOffset = new Vector2(0f, -100f);
    [SerializeField] private float dispenseDuration = 0.35f;

    [SerializeField] private Image tile;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI nameText;

    [Header("Look")]
    [SerializeField] private Color emptyKeyColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color filledKeyColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private float popScale = 1.2f;
    [SerializeField] private float popDuration = 0.25f;

    private RectTransform ticketRect;
    private Vector2 ticketRestPosition;
    private Coroutine dispenseCoroutine;
    private Vector3 baseScale;
    private float popTimer;

    private void Awake()
    {
        baseScale = transform.localScale;
        ticketRect = tile != null ? tile.rectTransform : null;
        if (ticketRect != null)
        {
            ticketRestPosition = ticketRect.anchoredPosition;
        }
    }

    public void Dispense()
    {
         if (ticketRect == null || ticketCanvasGroup == null)
        {
            Debug.LogError("The ticket Tile needs a RectTransform and CanvasGroup to dispense.", this);
            return;
        }

        if (dispenseCoroutine != null)
            StopCoroutine(dispenseCoroutine);

        dispenseCoroutine = StartCoroutine(DispenseRoutine());
    }

    private IEnumerator DispenseRoutine()
    {
        Vector2 startPosition = ticketRestPosition + dispenseOffset;
        ticketRect.anchoredPosition = startPosition;
        ticketCanvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < dispenseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dispenseDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            ticketRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, ticketRestPosition, eased);
            ticketCanvasGroup.alpha = t;

            yield return null;
        }

        ticketRect.anchoredPosition = ticketRestPosition;
        ticketCanvasGroup.alpha = 1f;
        dispenseCoroutine = null;

        Pop();
    }

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
            tile.color = Color.white;

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

    public void Pop()
    {
        if (popDuration <= 0f)
        {
            transform.localScale = baseScale;
            return;
        }

        popTimer = popDuration;
    }

    private void Update()
    {
        if (popTimer <= 0f)
            return;

        popTimer = Mathf.Max(0f, popTimer - Time.unscaledDeltaTime);
        float progress = popTimer / popDuration;
        transform.localScale = baseScale * Mathf.Lerp(1f, popScale, progress);
    }
}
