using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FinishLine : MonoBehaviour
{
    public RaceTimer timer;
    public GameObject fireworkPrefab;
    public Transform fireworkPointA;
    public Transform fireworkPointB;

    public GameObject objectToHide;

    public GameObject resultsPanel;
    public Text finalTimeText;
    public Text bestTimeText;
    public Button retryButton;

    public MonoBehaviour[] playerControlComponents;

    private bool triggered;

    void Start()
    {
        retryButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        timer.StopTimer();

        Instantiate(fireworkPrefab, fireworkPointA.position, Quaternion.identity);
        Instantiate(fireworkPrefab, fireworkPointB.position, Quaternion.identity);

        if (objectToHide) objectToHide.SetActive(false);

        foreach (var comp in playerControlComponents)
            comp.enabled = false;

        float finalTime = timer.GetFinalTime();
        finalTimeText.text = $"Final Time: {RaceTimer.FormatTime(finalTime)}";

        if (PersonalBest.TrySetBest(finalTime, out float previous))
        {
            bestTimeText.text = 
                $"New Personal Best! {RaceTimer.FormatTime(finalTime)}\nPrevious: {RaceTimer.FormatTime(previous)}";
        }
        else if (PersonalBest.HasBest())
        {
            bestTimeText.text = $"Personal Best: {RaceTimer.FormatTime(PersonalBest.GetBest())}";
        }
        else
        {
            bestTimeText.text = "First Run!";
        }

        resultsPanel.SetActive(true);
    }
}
