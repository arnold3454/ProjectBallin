using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Full-screen "pick an upgrade" overlay, built in code so no scene or prefab setup is needed.
/// It only draws and reports the click. UpgradeManager owns pausing and applying the pick.
/// </summary>
public class UpgradePopup : MonoBehaviour
{
    private const int CardCount = 3;

    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.8f);
    private static readonly Color CardColor = new Color(0.14f, 0.15f, 0.21f, 1f);
    private static readonly Color CardHoverColor = new Color(0.24f, 0.27f, 0.40f, 1f);
    private static readonly Color CardPressedColor = new Color(0.32f, 0.37f, 0.54f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.82f, 0.25f, 1f);
    private static readonly Color BonusColor = new Color(0.45f, 0.95f, 0.55f, 1f);

    private class Card
    {
        public GameObject Root;
        public Button Button;
        public TextMeshProUGUI Key;
        public TextMeshProUGUI Name;
        public TextMeshProUGUI Description;
        public TextMeshProUGUI Total;
    }

    private readonly Card[] cards = new Card[CardCount];
    private GameObject canvasObject;

    public void Build()
    {
        EnsureEventSystem();

        canvasObject = new GameObject("UpgradeCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        Image dim = CreateChild("Dim", canvasObject.transform).gameObject.AddComponent<Image>();
        Stretch(dim.rectTransform);
        dim.color = DimColor;

        TextMeshProUGUI title = CreateText("Title", canvasObject.transform, "LEVEL UP!", 88, AccentColor, FontStyles.Bold);
        Anchor(title.rectTransform, new Vector2(0f, 290f), new Vector2(1400f, 120f));

        TextMeshProUGUI subtitle = CreateText("Subtitle", canvasObject.transform, "Choose one upgrade  (click it, or press 1 / 2 / 3)", 36, new Color(0.8f, 0.82f, 0.9f), FontStyles.Normal);
        Anchor(subtitle.rectTransform, new Vector2(0f, 205f), new Vector2(1400f, 60f));

        RectTransform row = CreateChild("Cards", canvasObject.transform);
        Anchor(row, new Vector2(0f, -40f), new Vector2(1560f, 540f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < CardCount; i++)
            cards[i] = BuildCard(row, i);

        canvasObject.SetActive(false);
    }

    /// <summary>Shows the choices. <paramref name="owned"/> is how many times each has already been picked.</summary>
    public void Show(IReadOnlyList<UpgradeDefinition> choices, int[] owned, Action<int> onPick)
    {
        for (int i = 0; i < CardCount; i++)
        {
            Card card = cards[i];
            bool used = i < choices.Count;
            card.Root.SetActive(used);
            if (!used)
                continue;

            UpgradeDefinition upgrade = choices[i];
            card.Name.text = upgrade.Name;
            card.Description.text = upgrade.Description;
            card.Total.text = $"Total bonus: {upgrade.TotalAt(owned[i])} → {upgrade.TotalAt(owned[i] + 1)}";

            int index = i;
            card.Button.onClick.RemoveAllListeners();
            card.Button.onClick.AddListener(() => onPick(index));
        }

        canvasObject.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void Hide()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private Card BuildCard(Transform parent, int index)
    {
        Card card = new Card();

        RectTransform root = CreateChild($"Card {index + 1}", parent);
        card.Root = root.gameObject;

        Image background = root.gameObject.AddComponent<Image>();
        background.color = Color.white;

        // Without this, a card with more text would claim a wider share of the row.
        LayoutElement cardSize = root.gameObject.AddComponent<LayoutElement>();
        cardSize.preferredWidth = 0f;
        cardSize.flexibleWidth = 1f;
        cardSize.flexibleHeight = 1f;

        card.Button = root.gameObject.AddComponent<Button>();
        card.Button.targetGraphic = background;
        card.Button.transition = Selectable.Transition.ColorTint;
        card.Button.colors = new ColorBlock
        {
            normalColor = CardColor,
            highlightedColor = CardHoverColor,
            selectedColor = CardColor,
            pressedColor = CardPressedColor,
            disabledColor = CardColor,
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        card.Key = CreateText("Key", root, $"[{index + 1}]", 30, new Color(0.6f, 0.64f, 0.78f), FontStyles.Normal);
        card.Name = CreateText("Name", root, string.Empty, 50, AccentColor, FontStyles.Bold);
        card.Description = CreateText("Description", root, string.Empty, 32, Color.white, FontStyles.Normal);
        card.Total = CreateText("Total", root, string.Empty, 30, BonusColor, FontStyles.Bold);

        // Let the description soak up the spare height so the bonus line sits at the bottom.
        card.Description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        return card;
    }

    private static RectTransform CreateChild(string name, Transform parent)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, Color color, FontStyles style)
    {
        TextMeshProUGUI label = CreateChild(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Top;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;
        return label;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Anchor(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
