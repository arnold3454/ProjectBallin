using TMPro;
using UnityEngine;

/// <summary>Serialized UI reference for the Split ball cooldown indicator.</summary>
public class BallTypeSplitHud : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusLabel;

    private void Awake()
    {
        if (statusLabel == null)
        {
            Debug.LogError("BallTypeSplitHud requires a serialized status label.", this);
            enabled = false;
            return;
        }

        statusLabel.gameObject.SetActive(false);
    }

    public void SetVisible(bool visible)
    {
        if (statusLabel != null)
            statusLabel.gameObject.SetActive(visible);
    }

    public void SetStatus(string text, Color color)
    {
        if (statusLabel == null)
            return;

        statusLabel.text = text;
        statusLabel.color = color;
    }
}
