using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShakeSettingsManager : MonoBehaviour
{
    public static ShakeSettingsManager Instance { get; private set; }

    [Header("UI References - Shake")]
    [SerializeField] private Slider shakeForceSlider;
    [SerializeField] private TMP_InputField shakeForceInput;

    [Header("Ranges - Shake")]
    [SerializeField] private float minShakeForce = 0.2f;
    [SerializeField] private float maxShakeForce = 5f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeForce = 3f;

    public float ShakeForce => shakeForce;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        shakeForce = Mathf.Clamp(shakeForce, minShakeForce, maxShakeForce);

        if (shakeForceSlider != null)
        {
            shakeForceSlider.minValue = minShakeForce;
            shakeForceSlider.maxValue = maxShakeForce;
            shakeForceSlider.SetValueWithoutNotify(shakeForce);
            shakeForceSlider.onValueChanged.AddListener(OnSliderChanged);
        }

        if (shakeForceInput != null)
        {
            UpdateInputText();
            shakeForceInput.onEndEdit.AddListener(OnInputSubmitted);
        }
    }

    private void OnSliderChanged(float value)
    {
        shakeForce = Mathf.Clamp(value, minShakeForce, maxShakeForce);
        UpdateInputText();
    }

    private void OnInputSubmitted(string input)
    {
        if (float.TryParse(input, out float parsed))
            shakeForce = Mathf.Clamp(parsed, minShakeForce, maxShakeForce);

        if (shakeForceSlider != null)
            shakeForceSlider.SetValueWithoutNotify(shakeForce);

        UpdateInputText();
    }

    private void UpdateInputText()
    {
        if (shakeForceInput != null)
            shakeForceInput.SetTextWithoutNotify(shakeForce.ToString("F2"));
    }
}