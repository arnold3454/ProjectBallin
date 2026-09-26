using UnityEngine;
using System.Collections.Generic;

public class SFXcollide : MonoBehaviour
{
    [System.Serializable]
    public class CollisionSFX
    {
        public string tag;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public float minCollisionForce = 0.1f;
        public float cooldown = 0.05f;

        [HideInInspector] public float lastPlayedTime = -999f;
    }

    [Header("Collision Sound Profiles")]
    [SerializeField] private List<CollisionSFX> sfxProfiles = new List<CollisionSFX>();

    [Header("Shared Settings")]
    [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.1f;

    private AudioSource audioSource;
    private Rigidbody rb;
    private Dictionary<string, CollisionSFX> sfxLookup;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        rb = GetComponent<Rigidbody>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        sfxLookup = new Dictionary<string, CollisionSFX>();
        foreach (var profile in sfxProfiles)
        {
            if (!string.IsNullOrEmpty(profile.tag) && !sfxLookup.ContainsKey(profile.tag))
            {
                sfxLookup.Add(profile.tag, profile);
            }
        }
    }

    // Handles bumpers/triggers
    private void OnTriggerEnter(Collider other)
    {
        TryPlaySFX(other.gameObject, rb != null ? rb.linearVelocity.magnitude : 0f);
    }

    // Handles walls/flippers/anything using normal physical collision
    private void OnCollisionEnter(Collision collision)
    {
        TryPlaySFX(collision.gameObject, collision.relativeVelocity.magnitude);
    }
    private void TryPlaySFX(GameObject other, float speed)
    {
        string tag = other.tag;

        if (!sfxLookup.TryGetValue(tag, out CollisionSFX profile)) return;
        if (profile.clip == null) return;
        if (speed < profile.minCollisionForce) return;
        if (Time.time - profile.lastPlayedTime < profile.cooldown) return;

        profile.lastPlayedTime = Time.time;

        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(profile.clip, profile.volume);
    }
}