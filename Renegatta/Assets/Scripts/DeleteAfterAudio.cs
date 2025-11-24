using UnityEngine;

public class DeleteAfterAudio : MonoBehaviour
{
    private AudioSource audioSource;
    float originalVolume;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;
        //randomize pitch and volume
        audioSource.pitch = Random.Range(0.8f, 1.2f);
        audioSource.volume = Random.Range(0.8f, 1.2f) * originalVolume;
        audioSource.Play();
        Invoke(nameof(DisableSelf), audioSource.clip.length + 0.1f);
    }

    private void DisableSelf()
    {
        //delete it
        Destroy(gameObject);
    }
}
