using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        InitializeSlider(masterSlider, "MasterVolume", "MasterVolume");
        InitializeSlider(musicSlider, "MusicVolume", "MusicVolume");
        InitializeSlider(sfxSlider, "SFXVolume", "SFXVolume");
    }

    private void InitializeSlider(Slider slider, string parameter, string preferenceKey)
    {
        if (slider == null) return;

        float value = PlayerPrefs.GetFloat(preferenceKey, 1f);
        slider.SetValueWithoutNotify(value);
        SetVolume(parameter, value);
        slider.onValueChanged.AddListener(v => OnVolumeChanged(parameter, preferenceKey, v));
    }

    private void OnVolumeChanged(string parameter, string preferenceKey, float value)
    {
        SetVolume(parameter, value);
        PlayerPrefs.SetFloat(preferenceKey, value);
    }

    private void SetVolume(string parameter, float value)
    {
        // Audio Mixer volume is in decibels; log(0) is undefined, so clamp mute.
        float decibels = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        mixer.SetFloat(parameter, decibels);
    }
}
