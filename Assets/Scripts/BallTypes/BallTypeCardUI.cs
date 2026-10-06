using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Serialized UI references for one ball choice card.</summary>
public class BallTypeCardUI : MonoBehaviour
{
    private static readonly Color BonusColor = new Color(0.45f, 0.95f, 0.55f, 1f);

    [SerializeField] private Button button;
    [SerializeField] private Image ballImage;
    [SerializeField] private TextMeshProUGUI keyLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;

    public bool Initialize(BallTypeInfo info, int index, Action<BallType> onPick)
    {
        if (button == null || ballImage == null || keyLabel == null || nameLabel == null || descriptionLabel == null)
        {
            Debug.LogError("BallTypeCardUI is missing one or more serialized UI references.", this);
            return false;
        }

        keyLabel.text = $"[{index + 1}]";
        ballImage.color = info.Color;
        nameLabel.text = info.Name;
        descriptionLabel.text =
            $"{info.Flavor}\n\n<color=#{ColorUtility.ToHtmlStringRGB(BonusColor)}><b>{info.PointsText}</b></color>";

        BallType type = info.Type;
        button.onClick.AddListener(() => onPick(type));
        return true;
    }
}
