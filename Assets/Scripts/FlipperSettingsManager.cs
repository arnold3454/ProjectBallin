using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlipperSettingsManager : MonoBehaviour
{
    public static FlipperSettingsManager Instance { get; private set; }

    [Header("UI References - Flippers")]
    [SerializeField] private Slider strengthSlider;
    [SerializeField] private TMP_InputField strengthInput;
    [SerializeField] private Slider heightSlider;
    [SerializeField] private TMP_InputField heightInput;

    [Header("Ranges - Flippers")]
    [SerializeField] private float minStrength = 0.2f;
    [SerializeField] private float maxStrength = 3.0f;
    [SerializeField] private float minHeight = -1.0f;
    [SerializeField] private float maxHeight = 1.0f;

    public float CurrentStrength 
    {
        get { if (strengthInput != null && float.TryParse(strengthInput.text, out float val)) return val; return strengthSlider != null ? strengthSlider.value : 1f; }
    }

    public float CurrentHeight 
    {
        get { if (heightInput != null && float.TryParse(heightInput.text, out float val)) return val; return heightSlider != null ? heightSlider.value : 0f; }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Flipper Strength
        if (strengthSlider != null) 
        { 
            strengthSlider.minValue = minStrength; 
            strengthSlider.maxValue = maxStrength; 
            strengthSlider.value = 1f; 
            strengthSlider.onValueChanged.AddListener(v => { if (strengthInput != null) strengthInput.text = v.ToString("F2"); ApplyStrength(v); }); 
        }
        if (strengthInput != null) 
        { 
            strengthInput.text = "1.00"; 
            strengthInput.onEndEdit.AddListener(s => OnInputSubmitted(s, strengthSlider, strengthInput, minStrength, maxStrength, ApplyStrength)); 
        }

        // Flipper Height Offset
        if (heightSlider != null) 
        { 
            heightSlider.minValue = minHeight; 
            heightSlider.maxValue = maxHeight; 
            heightSlider.value = 0f; 
            heightSlider.onValueChanged.AddListener(v => { if (heightInput != null) heightInput.text = v.ToString("F2"); ApplyHeight(v); }); 
        }
        if (heightInput != null) 
        { 
            heightInput.text = "0.00"; 
            heightInput.onEndEdit.AddListener(s => OnInputSubmitted(s, heightSlider, heightInput, minHeight, maxHeight, ApplyHeight)); 
        }
    }

    private void OnInputSubmitted(string input, Slider slider, TMP_InputField inputField, float min, float max, System.Action<float> callback)
    {
        if (float.TryParse(input, out float parsed))
        {
            parsed = Mathf.Max(0.01f, parsed);
            inputField.text = parsed.ToString("F2");

            if (slider != null)
            {
                if (parsed >= slider.minValue && parsed <= slider.maxValue) slider.value = parsed;
                else if (parsed > slider.maxValue) slider.value = slider.maxValue;
                else if (parsed < slider.minValue) slider.value = slider.minValue;
            }

            callback?.Invoke(parsed);
        }
        else if (slider != null && inputField != null)
        {
            inputField.text = slider.value.ToString("F2");
        }
    }

    private void ApplyStrength(float val)
    {
        foreach (FlipperControls flipper in FindObjectsByType<FlipperControls>(FindObjectsInactive.Include))
        {
            flipper.UpdateStrength(val);
        }
    }

    private void ApplyHeight(float val)
    {
        foreach (FlipperControls flipper in FindObjectsByType<FlipperControls>(FindObjectsInactive.Include))
        {
            flipper.UpdateHeightOffset(val);
        }
    }
}