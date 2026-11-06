using UnityEngine;

public class DeleteBall : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("Played when the ball is destroyed after entering the killzone.")]
    [SerializeField] private AudioClip drainClip;
    [SerializeField, Range(0f, 1f)] private float drainVolume = 1f;

private void OnTriggerEnter(Collider other)
    {
if (other.gameObject.CompareTag("Killzone"))
        {
PlayDrainSFX();
Destroy(gameObject);
        }
    }

private void PlayDrainSFX()
    {
if (drainClip == null)
return;

GameObject tempAudio = new GameObject("DrainSFX_Temp");
AudioSource tempSource = tempAudio.AddComponent<AudioSource>();
tempSource.clip = drainClip;
tempSource.volume = drainVolume;
tempSource.spatialBlend = 0f;
tempSource.Play();

Destroy(tempAudio, drainClip.length);
    }
}