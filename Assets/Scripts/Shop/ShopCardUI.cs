using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One card in the dealer's shop: a header ("ITEM" or "UPGRADE"), the offer's name and
/// description, its cost and a Buy button. It only draws and reports the click;
/// <see cref="ShopManager"/> owns the stock, the prices and the purchase itself.
/// The cards live under the ShopPanel in the scene and are referenced by the ShopManager.
/// </summary>
public class ShopCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyLabel;

    [Header("Look")]
    [SerializeField] private Color affordableCostColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private Color unaffordableCostColor = new Color(1f, 0.4f, 0.4f);
    [SerializeField] private string buyText = "BUY";
    [SerializeField] private string soldText = "SOLD";
    [SerializeField] private string emptyText = "NOTHING LEFT";

    /// <summary>Fills the card with an offer that is still for sale.</summary>
    public void Show(string header, string offerName, string description, int cost, bool affordable, Action onBuy)
    {
        if (headerText != null)
            headerText.text = header;
        if (nameText != null)
            nameText.text = offerName;
        if (descriptionText != null)
            descriptionText.text = description;
        if (costText != null)
        {
            costText.text = $"Cost: {cost}";
            costText.color = affordable ? affordableCostColor : unaffordableCostColor;
        }
        if (buyLabel != null)
            buyLabel.text = buyText;

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy?.Invoke());
            buyButton.interactable = affordable;
        }
    }

    /// <summary>Keeps the offer's text on the card but marks it as bought.</summary>
    public void ShowSold()
    {
        if (buyLabel != null)
            buyLabel.text = soldText;
        if (costText != null)
            costText.color = affordableCostColor;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = false;
        }
    }

    /// <summary>Shows the card with nothing to sell, e.g. when every upgrade is maxed out.</summary>
    public void ShowEmpty(string header, string reason)
    {
        if (headerText != null)
            headerText.text = header;
        if (nameText != null)
            nameText.text = "-";
        if (descriptionText != null)
            descriptionText.text = reason;
        if (costText != null)
            costText.text = string.Empty;
        if (buyLabel != null)
            buyLabel.text = emptyText;
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = false;
        }
    }
}
