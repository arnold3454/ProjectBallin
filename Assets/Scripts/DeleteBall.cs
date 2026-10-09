using UnityEngine;
using UnityEngine.Audio;

public class DeleteBall : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip drainClip;
    [SerializeField, Range(0f, 1f)] private float drainVolume = 1f;
    [SerializeField] private AudioMixerGroup sfxGroup;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Killzone"))
            return;

        PlayDrainSFX();
        Destroy(gameObject);
    }

    private void PlayDrainSFX()
    {
        if (drainClip == null)
            return;

        GameObject tempAudio = new GameObject("DrainSFX_Temp");
        AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
        tempSource.outputAudioMixerGroup = sfxGroup;
        tempSource.clip = drainClip;
        tempSource.volume = drainVolume;
        tempSource.spatialBlend = 0f;
        tempSource.Play();

        Destroy(tempAudio, drainClip.length);
    }
}