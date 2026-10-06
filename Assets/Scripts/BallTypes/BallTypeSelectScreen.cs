using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Full-screen "choose your ball" overlay. Its layout and card references are authored in a prefab.
/// BallTypeManager owns pausing and applying the choice.
/// </summary>
public class BallTypeSelectScreen : MonoBehaviour
{
    [SerializeField] private BallTypeCardUI[] cards;

    public bool Initialize(IReadOnlyList<BallTypeInfo> types, Action<BallType> onPick)
    {
        if (types == null || onPick == null || cards == null || cards.Length != types.Count)
        {
            Debug.LogError("BallTypeSelectScreen requires one serialized card for each ball type.", this);
            return false;
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
            {
                Debug.LogError($"BallTypeSelectScreen card reference {i} is missing.", this);
                return false;
            }

            if (!cards[i].Initialize(types[i], i, onPick))
                return false;
        }

        return true;
    }

    public void Show()
    {
        gameObject.SetActive(true);

        if (UnityEngine.EventSystems.EventSystem.current != null)
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
