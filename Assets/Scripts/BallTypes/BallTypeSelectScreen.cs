using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen "choose your ball" overlay, built in code so no scene or prefab setup is needed.
/// It only draws and reports the click. BallTypeManager owns pausing and applying the choice.
/// </summary>
public class BallTypeSelectScreen : MonoBehaviour
{
    private static readonly Color DimColor = new Color(0.02f, 0.03f, 0.06f, 0.94f);
    private static readonly Color CardColor = new Color(0.14f, 0.15f, 0.21f, 1f);
    private static readonly Color CardHoverColor = new Color(0.24f, 0.27f, 0.40f, 1f);
    private static readonly Color CardPressedColor = new Color(0.32f, 0.37f, 0.54f, 1f);
    private static readonly Color AccentColor = new Color(1f, 0.82f, 0.25f, 1f);
    private static readonly Color BonusColor = new Color(0.45f, 0.95f, 0.55f, 1f);

    private GameObject canvasObject;
    private Sprite ballSprite;

    public void Build(IReadOnlyList<BallTypeInfo> types, Action<BallType> onPick)
    {
        BallTypeUI.EnsureEventSystem();
        ballSprite = CreateBallSprite();

        canvasObject = new GameObject("BallTypeCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        Image dim = BallTypeUI.CreateChild("Dim", canvasObject.transform).gameObject.AddComponent<Image>();
        BallTypeUI.Stretch(dim.rectTransform);
        dim.color = DimColor;

        TextMeshProUGUI title = BallTypeUI.CreateText("Title", canvasObject.transform, "CHOOSE YOUR BALL", 88, AccentColor, FontStyles.Bold);
        BallTypeUI.Anchor(title.rectTransform, new Vector2(0f, 400f), new Vector2(1400f, 120f));

        TextMeshProUGUI subtitle = BallTypeUI.CreateText("Subtitle", canvasObject.transform, "Click a ball to start  (or press 1 / 2 / 3)", 36, new Color(0.8f, 0.82f, 0.9f), FontStyles.Normal);
        BallTypeUI.Anchor(subtitle.rectTransform, new Vector2(0f, 315f), new Vector2(1400f, 60f));

        RectTransform row = BallTypeUI.CreateChild("Cards", canvasObject.transform);
        BallTypeUI.Anchor(row, new Vector2(0f, -45f), new Vector2(1680f, 660f));
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 40f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        for (int i = 0; i < types.Count; i++)
            BuildCard(row, i, types[i], onPick);

        canvasObject.SetActive(false);
    }

    public void Show()
    {
        canvasObject.SetActive(true);

        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    public void Hide()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);
    }

    private void BuildCard(Transform parent, int index, BallTypeInfo info, Action<BallType> onPick)
    {
        RectTransform root = BallTypeUI.CreateChild($"Card {index + 1}", parent);

        Image background = root.gameObject.AddComponent<Image>();
        background.color = Color.white;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = CardColor,
            highlightedColor = CardHoverColor,
            selectedColor = CardColor,
            pressedColor = CardPressedColor,
            disabledColor = CardColor,
            colorMultiplier = 1f,
            fadeDuration = 0.08f,
        };

        // Without this, a card with more text would claim a wider share of the row.
        LayoutElement cardSize = root.gameObject.AddComponent<LayoutElement>();
        cardSize.preferredWidth = 0f;
        cardSize.flexibleWidth = 1f;
        cardSize.flexibleHeight = 1f;

        BallType type = info.Type;
        button.onClick.AddListener(() => onPick(type));

        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(32, 32, 28, 28);
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        BallTypeUI.CreateText("Key", root, $"[{index + 1}]", 30, new Color(0.6f, 0.64f, 0.78f), FontStyles.Normal);

        Image swatch = BallTypeUI.CreateChild("Ball", root).gameObject.AddComponent<Image>();
        swatch.sprite = ballSprite;
        swatch.color = info.Color;
        swatch.preserveAspect = true;
        swatch.raycastTarget = false;
        swatch.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;

        BallTypeUI.CreateText("Name", root, info.Name, 50, AccentColor, FontStyles.Bold);

        string points = $"<color=#{ColorUtility.ToHtmlStringRGB(BonusColor)}><b>{info.PointsText}</b></color>";
        TextMeshProUGUI description = BallTypeUI.CreateText("Description", root, info.Flavor + "\n\n" + points, 30, Color.white, FontStyles.Normal);
        description.enableAutoSizing = true;
        description.fontSizeMin = 18f;
        description.fontSizeMax = 30f;

        // Let the description soak up the spare height so every card ends at the same place.
        description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
    }

    /// <summary>A shaded white disc, tinted by the Image colour. Avoids needing a sprite asset.</summary>
    private static Sprite CreateBallSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "BallSwatch",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        Vector3 light = new Vector3(-0.45f, 0.55f, 0.7f).normalized;
        float radius = size * 0.5f - 1f;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = (x + 0.5f - size * 0.5f) / radius;
                float py = (y + 0.5f - size * 0.5f) / radius;
                float distance = Mathf.Sqrt(px * px + py * py);
                float alpha = Mathf.Clamp01((1f - distance) * radius);

                float shade = 0f;
                if (distance < 1f)
                {
                    Vector3 normal = new Vector3(px, py, Mathf.Sqrt(1f - distance * distance));
                    float diffuse = Mathf.Max(0f, Vector3.Dot(normal, light));
                    float specular = Mathf.Pow(diffuse, 24f) * 0.6f;
                    shade = Mathf.Clamp01(0.4f + 0.6f * diffuse + specular);
                }

                byte value = (byte)Mathf.RoundToInt(shade * 255f);
                pixels[y * size + x] = new Color32(value, value, value, (byte)Mathf.RoundToInt(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
