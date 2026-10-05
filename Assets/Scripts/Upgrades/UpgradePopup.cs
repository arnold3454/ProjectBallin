using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Full-screen "pick an upgrade" overlay, backed by a prefab with serialized UI references.
/// It only draws and reports the click. UpgradeManager owns pausing and applying the pick.
/// </summary>
public class UpgradePopup : MonoBehaviour
{
    private const int CardCount = 3;

    [Serializable]
    private class Card
    {
        [SerializeField] public GameObject Root;
        [SerializeField] public Button Button;
        [SerializeField] public TextMeshProUGUI Name;
        [SerializeField] public TextMeshProUGUI Description;
        [SerializeField] public TextMeshProUGUI Total;
    }

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Card[] cards = new Card[CardCount];

    private void Awake()
    {
        EnsureEventSystem();
        Hide();
    }

    /// <summary>Shows the choices. <paramref name="owned"/> is how many times each has already been picked.</summary>
    public void Show(IReadOnlyList<UpgradeDefinition> choices, int[] owned, Action<int> onPick)
    {
        for (int i = 0; i < cards.Length; i++)
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

        popupRoot.SetActive(true);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private static void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
