using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI buttonText;

    [Header("Leaderboard")]
    [SerializeField] private Leaderboard leaderboard;

    private bool hasBeenClicked = false;

    // username, score
    public UnityEvent<string, int> submitScoreEvent;

    public void SubmitScore()
    {
        if (hasBeenClicked)
            return;

        hasBeenClicked = true;

        string playerName = nameInput != null ? nameInput.text : "Unnamed";
        int scoreValue = int.Parse(scoreText.text);

        submitScoreEvent.Invoke(playerName, scoreValue);

        buttonText.text = "Submitted!";
        leaderboard.GetLeaderboard();

        Invoke(nameof(Hide), 2f);
    }

    private void Hide()
    {
        leaderboard.GetLeaderboard();
        gameObject.SetActive(false);
    }
}
