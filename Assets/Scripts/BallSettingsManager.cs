using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BallSettingsManager : MonoBehaviour
{
    public static BallSettingsManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private Slider sizeSlider;
    [SerializeField] private TMP_InputField sizeInput;
    [SerializeField] private Slider weightSlider;
    [SerializeField] private TMP_InputField weightInput;

    [Header("Ranges")]
    [SerializeField] private float minSize = 0.1f;
    [SerializeField] private float maxSize = 10.0f;
    [SerializeField] private float minWeight = 0.5f;
    [SerializeField] private float maxWeight = 30.0f;

    private float currentSize = 1f;
    private float currentWeight = 1f;

    public float CurrentSize => currentSize;
    public float CurrentWeight => currentWeight;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (sizeSlider != null)
        {
            sizeSlider.minValue = minSize;
            sizeSlider.maxValue = maxSize;
            currentSize = Mathf.Clamp(sizeSlider.value, minSize, maxSize);
            sizeSlider.onValueChanged.AddListener(OnSizeSliderChanged);
            sizeSlider.SetValueWithoutNotify(currentSize);
        }

        if (sizeInput != null)
        {
            sizeInput.text = currentSize.ToString("F2");
            sizeInput.onEndEdit.AddListener(OnSizeInputSubmitted);
        }

        if (weightSlider != null)
        {
            weightSlider.minValue = minWeight;
            weightSlider.maxValue = maxWeight;
            currentWeight = Mathf.Clamp(weightSlider.value, minWeight, maxWeight);
            weightSlider.onValueChanged.AddListener(OnWeightSliderChanged);
            weightSlider.SetValueWithoutNotify(currentWeight);
        }

        if (weightInput != null)
        {
            weightInput.text = currentWeight.ToString("F2");
            weightInput.onEndEdit.AddListener(OnWeightInputSubmitted);
        }

        ApplySizeToExistingBalls(currentSize);
        ApplyWeightToExistingBalls(currentWeight);
    }

    private void OnSizeSliderChanged(float value)
    {
        currentSize = Mathf.Clamp(value, minSize, maxSize);
        if (sizeInput != null) sizeInput.text = currentSize.ToString("F2");
        ApplySizeToExistingBalls(currentSize);
    }

    private void OnSizeInputSubmitted(string input)
    {
        if (float.TryParse(input, out float parsed))
        {
            // Ensure size doesn't go below a safe tiny threshold
            parsed = Mathf.Max(0.01f, parsed);
            currentSize = parsed;

            // Keep the text input showing the exact extended number the player typed
            if (sizeInput != null) 
            {
                sizeInput.text = parsed.ToString("F2");
            }

            // Snap slider to its bounds if the typed value exceeds them, otherwise match it
            if (sizeSlider != null)
            {
                if (parsed >= sizeSlider.minValue && parsed <= sizeSlider.maxValue)
                    sizeSlider.value = parsed;
                else if (parsed > sizeSlider.maxValue)
                    sizeSlider.value = sizeSlider.maxValue;
                else if (parsed < sizeSlider.minValue)
                    sizeSlider.value = sizeSlider.minValue;
            }

            ApplySizeToExistingBalls(currentSize);
        }
        else if (sizeSlider != null && sizeInput != null)
        {
            sizeInput.text = sizeSlider.value.ToString("F2");
        }
    }

    private void OnWeightSliderChanged(float value)
    {
        currentWeight = Mathf.Clamp(value, minWeight, maxWeight);
        if (weightInput != null) weightInput.text = currentWeight.ToString("F2");
        ApplyWeightToExistingBalls(currentWeight);
    }

    private void OnWeightInputSubmitted(string input)
    {
        if (float.TryParse(input, out float parsed))
        {
            // Ensure weight doesn't go below a safe tiny threshold
            parsed = Mathf.Max(0.01f, parsed);
            currentWeight = parsed;

            // Keep the text input showing the exact extended number the player typed
            if (weightInput != null) 
            {
                weightInput.text = parsed.ToString("F2");
            }

            // Snap slider to its bounds if the typed value exceeds them, otherwise match it
            if (weightSlider != null)
            {
                if (parsed >= weightSlider.minValue && parsed <= weightSlider.maxValue)
                    weightSlider.value = parsed;
                else if (parsed > weightSlider.maxValue)
                    weightSlider.value = weightSlider.maxValue;
                else if (parsed < weightSlider.minValue)
                    weightSlider.value = weightSlider.minValue;
            }

            ApplyWeightToExistingBalls(currentWeight);
        }
        else if (weightSlider != null && weightInput != null)
        {
            weightInput.text = weightSlider.value.ToString("F2");
        }
    }

    private void ApplySizeToExistingBalls(float sizeMultiplier)
    {
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in balls)
        {
            ball.transform.localScale = Vector3.one * sizeMultiplier;
        }
    }

    private void ApplyWeightToExistingBalls(float massValue)
    {
        GameObject[] balls = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in balls)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.mass = massValue;
            }
        }
    }
}