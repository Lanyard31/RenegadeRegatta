using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Slider ambientSlider;
    public Toggle timerToggle;
    public Toggle controlsToggle;

    [Header("UI Elements To Toggle")]
    public GameObject timerUI;
    public GameObject controlsUI;

    [Header("Audio")]
    public AudioMixer mixer; // Exposed params: MasterVol, MusicVol, SFXVol, AmbientVol

    [Header("Gameplay Objects")]
    public MonoBehaviour shipController;
    public MonoBehaviour cannonController;

    private bool isPaused;

    private const string MASTER_KEY = "vol_master";
    private const string MUSIC_KEY = "vol_music";
    private const string SFX_KEY = "vol_sfx";
    private const string AMB_KEY = "vol_amb";
    private const string TIMER_KEY = "show_timer";
    private const string CONTROLS_KEY = "show_controls";

    void Start()
    {
        LoadPrefs();
        ApplyVolume();
        ApplyToggles();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        pausePanel.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;

        shipController.enabled = !isPaused;
        cannonController.enabled = !isPaused;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UnpauseButton()
    {
        if (isPaused)
            TogglePause();
    }

    // Volume sliders -------------------------------------------------------

    public void OnMasterVolume(float value)
    {
        mixer.SetFloat("MasterVol", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(MASTER_KEY, value);
    }

    public void OnMusicVolume(float value)
    {
        mixer.SetFloat("MusicVol", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(MUSIC_KEY, value);
    }

    public void OnSFXVolume(float value)
    {
        mixer.SetFloat("SFXVol", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(SFX_KEY, value);
    }

    public void OnAmbientVolume(float value)
    {
        mixer.SetFloat("AmbientVol", Mathf.Log10(value) * 20f);
        PlayerPrefs.SetFloat(AMB_KEY, value);
    }

    // Toggles --------------------------------------------------------------

    public void OnTimerToggle(bool value)
    {
        PlayerPrefs.SetInt(TIMER_KEY, value ? 1 : 0);
        if (timerUI != null)
            timerUI.SetActive(value);
    }

    public void OnControlsToggle(bool value)
    {
        PlayerPrefs.SetInt(CONTROLS_KEY, value ? 1 : 0);
        if (controlsUI != null)
            controlsUI.SetActive(value);
    }

    // Helpers --------------------------------------------------------------

    private void LoadPrefs()
    {
        masterSlider.value = PlayerPrefs.GetFloat(MASTER_KEY, 0.8f);
        musicSlider.value = PlayerPrefs.GetFloat(MUSIC_KEY, 0.8f);
        sfxSlider.value = PlayerPrefs.GetFloat(SFX_KEY, 0.8f);
        ambientSlider.value = PlayerPrefs.GetFloat(AMB_KEY, 0.8f);

        timerToggle.isOn = PlayerPrefs.GetInt(TIMER_KEY, 1) == 1;
        controlsToggle.isOn = PlayerPrefs.GetInt(CONTROLS_KEY, 1) == 1;
    }

    private void ApplyVolume()
    {
        OnMasterVolume(masterSlider.value);
        OnMusicVolume(musicSlider.value);
        OnSFXVolume(sfxSlider.value);
        OnAmbientVolume(ambientSlider.value);
    }

    private void ApplyToggles()
    {
        OnTimerToggle(timerToggle.isOn);
        OnControlsToggle(controlsToggle.isOn);
    }
}
