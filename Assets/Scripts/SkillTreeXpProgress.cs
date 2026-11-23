using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillTreeXpProgress : MonoBehaviour
{
    [SerializeField] private UpgradeManager xpSource;
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text tokensText;

    private void OnEnable()
    {
        if (xpSource == null || fillImage == null || tokensText == null)
        {
            Debug.LogError("Assign the XP source, fill image, and text fields.", this);
            return;
        }

        fillImage.gameObject.SetActive(true);
        tokensText.gameObject.SetActive(true);
        xpSource.XpChanged += Refresh;
        Refresh(xpSource.CurrentXp, xpSource.XpPerUpgrade);
    }

    private void Start()
    {
        if (xpSource != null)
            Refresh(xpSource.CurrentXp, xpSource.XpPerUpgrade);
    }

    private void OnDisable()
    {
        if (xpSource != null)
            xpSource.XpChanged -= Refresh;
    }

    private void Refresh(int currentXp, int xpRequired)
    {
        xpRequired = Mathf.Max(1, xpRequired);
        currentXp = Mathf.Clamp(currentXp, 0, xpRequired);

        fillImage.fillAmount = (float)currentXp / xpRequired;
        tokensText.text = $"{xpSource.UpgradeLevel}";
    }
}