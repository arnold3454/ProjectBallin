using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>View for one pre-authored skill-tree button in the scene.</summary>
public sealed class SkillTreeNodeView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI stateText;
    [SerializeField] private Graphic stateGraphic;
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color availableColor = Color.white;
    [SerializeField] private Color purchasedColor = new Color(0.4f, 0.9f, 0.55f, 1f);

    private UnityAction clickHandler;

    public void Bind(Action onClick)
    {
        if (button == null)
        {
            Debug.LogError($"Skill node view '{name}' is missing its Button reference.", this);
            return;
        }

        if (clickHandler != null)
            button.onClick.RemoveListener(clickHandler);

        clickHandler = onClick == null ? null : new UnityAction(onClick);
        if (clickHandler != null)
            button.onClick.AddListener(clickHandler);
    }

    public void SetState(
        string title,
        string description,
        string rank,
        string state,
        bool interactable,
        bool isUnlocked,
        bool isPurchased)
    {
        if (titleText != null)
        {
            bool useSingleText = descriptionText == null && rankText == null && stateText == null;
            titleText.text = useSingleText
                ? $"{title}\n{rank} - {state}\n{description}"
                : title;
        }
        if (descriptionText != null)
            descriptionText.text = description;
        if (rankText != null)
            rankText.text = rank;
        if (stateText != null)
            stateText.text = state;
        if (button != null)
            button.interactable = interactable;

        if (stateGraphic != null)
            stateGraphic.color = isPurchased ? purchasedColor : isUnlocked ? availableColor : lockedColor;
    }

    private void OnDestroy()
    {
        if (button != null && clickHandler != null)
            button.onClick.RemoveListener(clickHandler);
    }
}
