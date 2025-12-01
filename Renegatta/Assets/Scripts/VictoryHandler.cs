using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using EasyTransition;
using Unity.VisualScripting;

public class VictoryHandler : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Behaviour[] componentsToDisable;
    [SerializeField] private ParticleSystem[] particlesToStop;

    [Header("Victory UI")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private VictoryGradeDisplay victoryGradeDisplay;

    [Header("Fireworks")]
    [SerializeField] private ParticleSystem fireworks;
    [SerializeField] private float fireworksInterval = 6f;

    [Header("Transition")]
    [SerializeField] private TransitionSettings transitionSettings;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private TransitionManager transitionManager;

    private float timer;
    private bool victoryTriggered;

    private void OnEnable()
    {
        // Hook into the boss defeat event
        SkullBoss.OnSkullBossDefeated += HandleVictory;
    }

    private void OnDestroy()
    {
        SkullBoss.OnSkullBossDefeated -= HandleVictory;
    }

    private void Update()
    {
        if (!victoryTriggered)
            timer += Time.deltaTime;
    }

    private void HandleVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;

        Invoke("PanelPopup", 6f);
    }

    private void PanelPopup()
    {
        // Disable movement etc.
        foreach (var c in componentsToDisable)
        {
            if (c != null)
                c.enabled = false;
        }

        foreach (var p in particlesToStop)
        {
            if (p != null)
                p.Stop();
                p.gameObject.SetActive(false);
        }

        //Stop the player's rigidBody movement
        player.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        player.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        // UI
        victoryPanel.SetActive(true);
        UpdateTimerDisplay();

        // Fireworks loop
        if (fireworks != null)
            StartCoroutine(FireworksRoutine());

        victoryGradeDisplay.DisplayGrade();
    }

    private void UpdateTimerDisplay()
    {
        int minutes = Mathf.FloorToInt(timer / 60f);
        int seconds = Mathf.FloorToInt(timer % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private IEnumerator FireworksRoutine()
    {
        //activate the gameObject
        fireworks.gameObject.SetActive(true);
        while (true)
        {
            fireworks.Play();
            yield return new WaitForSeconds(fireworksInterval);
        }
    }

    // Button methods -------------------

    public void Retry()
    {
        MusicController.Instance.ChangeToLevelMusic();
        Scene current = SceneManager.GetActiveScene();
        transitionManager.Transition(
            current.name,
            transitionSettings,
            transitionDuration
        );
    }

    public void ReturnToMenu()
    {
        MusicController.Instance.dontDestroyOnLoad = false;
        //destroy the music controller
        Destroy(MusicController.Instance.gameObject);

        transitionManager.Transition(
            "StartScene",
            transitionSettings,
            transitionDuration
        );
    }

    public float Timer => timer;
}
