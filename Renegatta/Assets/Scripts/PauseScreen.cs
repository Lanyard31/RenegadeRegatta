using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Audio;

public class PauseScreen : MonoBehaviour
{
    [Header("UI")]
    public PlayerHealth playerHealth;
    public GameObject pausePanel;
    

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;
    public Slider ambientSlider;

    [Header("Audio")]
    public AudioMixer audioMixer; // assign your AudioMixer here

    private bool isPaused = false;
    private bool canPause = true;

    private void OnEnable()
    {
        playerHealth.OnDeath += OnPlayerDeath;
        SkullBoss.OnSkullBossDefeated += OnSkullBossDefeated;
    }

    private void OnDisable()
    {
        playerHealth.OnDeath -= OnPlayerDeath;
        SkullBoss.OnSkullBossDefeated -= OnSkullBossDefeated;
    }

    private void Start()
    {
        pausePanel.SetActive(false);
        LoadAudioSettings();

        // Add listeners for sliders
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        ambientSlider.onValueChanged.AddListener(SetAmbientVolume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) && canPause)
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    private void OnPlayerDeath()
    {
        canPause = false;
        if (isPaused) TogglePause(); // unpause if already paused
    }

    private void OnSkullBossDefeated()
    {
        canPause = false;
        if (isPaused) TogglePause();
    }

    #region Audio
    private void LoadAudioSettings()
    {
        masterSlider.value = PlayerPrefs.GetFloat("MasterVolume", 0.9f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.9f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 0.9f);
        ambientSlider.value = PlayerPrefs.GetFloat("AmbientVolume", 0.9f);

        SetMasterVolume(masterSlider.value);
        SetSFXVolume(sfxSlider.value);
        SetMusicVolume(musicSlider.value);
        SetAmbientVolume(ambientSlider.value);
    }

    private void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("MasterVolume", value);
    }

    private void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    private void SetAmbientVolume(float value)
    {
        audioMixer.SetFloat("AmbientVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat("AmbientVolume", value);
    }

    private float LinearToDecibel(float linear)
    {
        // Prevent log(0)
        linear = Mathf.Clamp(linear, 0.0001f, 1f);
        return 20f * Mathf.Log10(linear);
    }
    #endregion
}
