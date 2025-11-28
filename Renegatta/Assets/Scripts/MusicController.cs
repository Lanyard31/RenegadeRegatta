using UnityEngine;
using System.Collections;

public class MusicController : MonoBehaviour
{
    public bool dontDestroyOnLoad = true;
    AudioSource audioSource;
    [SerializeField] AudioClip levelMusic;
    [SerializeField] AudioClip bossMusic;
    [SerializeField] AudioClip victoryMusic;
    float originalVolume;

    public static MusicController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;
    }

    public void ChangeToBossMusic()
    {
        StartCoroutine(ChangeMusicCoroutine(bossMusic));
    }

    public void ChangeToVictoryMusic()
    {
        StartCoroutine(ChangeMusicCoroutine(victoryMusic));
    }

    public void ChangeToLevelMusic()
    {
        StartCoroutine(ChangeMusicCoroutine(levelMusic));
    }

    IEnumerator ChangeMusicCoroutine(AudioClip clip)
    {
        //ignore if clip is already playing
        if (audioSource.clip == clip) yield break;
        while (audioSource.volume > 0)
        {
            audioSource.volume -= 0.005f;
            yield return new WaitForSeconds(0.05f);
        }
        yield return new WaitForSeconds(2f);
        audioSource.clip = clip;
        audioSource.Play();
        while (audioSource.volume < originalVolume)
        {
            audioSource.volume += 0.005f;
            yield return new WaitForSeconds(0.05f);
        }
    }
}
