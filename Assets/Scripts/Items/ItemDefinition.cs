using UnityEngine;

/// <summary>
/// Base class for the player's one-time use items. Each item is an asset
/// (Assets > Create > ProjectBallin > Items) that holds its display settings
/// and tuning values, and each subclass implements the item's effect in Use().
/// </summary>
public abstract class ItemDefinition : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string displayName = "Item";
    [Tooltip("Short text shown on the HUD slot and on pickups, e.g. \"+15s\".")]
    [SerializeField] private string shortLabel = "?";
    [Tooltip("Optional. Shown on the HUD slot in place of the short label.")]
    [SerializeField] private Sprite icon;
    [SerializeField] private Color color = Color.white;

    public string DisplayName => displayName;
    public string ShortLabel => shortLabel;
    public Sprite Icon => icon;
    public Color Color => color;

    /// <summary>
    /// Applies the item's effect. Returns false if the item can't be used right
    /// now, in which case the player keeps it.
    /// </summary>
    public abstract bool Use();
}
