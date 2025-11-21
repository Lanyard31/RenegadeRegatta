using UnityEngine;

public class HitVFX : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip[] clips;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;
    public float volumeMin = 0.7f;
    public float volumeMax = 1.0f;
    private float originalVolume;

    [Header("Particle Settings")]
    public ParticleSystem particles;

    private void Awake()
    {
        originalVolume = audioSource.volume;
    }

    private void OnEnable()
    {
        // Play particle effect
        if (particles != null)
            particles.Play();

        // Randomize and play audio
        if (audioSource != null && clips.Length > 0)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = Random.Range(pitchMin, pitchMax);
            //be sure to reference original volume
            audioSource.PlayOneShot(clip, Random.Range(volumeMin, volumeMax) * originalVolume);
            //disable self after sound is player
            Invoke(nameof(DisableSelf), clip.length);
        }

/*
        // Optional: auto-disable after particle finishes
        if (particles != null)
            Invoke(nameof(DisableSelf), particles.main.duration + particles.main.startLifetime.constantMax + 2f);
            */
    }

    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}
