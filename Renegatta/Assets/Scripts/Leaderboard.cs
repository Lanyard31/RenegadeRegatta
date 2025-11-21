using System.Collections.Generic;
using UnityEngine;
using TMPro;
//using Dan.Main;

public class Leaderboard : MonoBehaviour
{
    /*
    [SerializeField] private List<TextMeshProUGUI> names;
    [SerializeField] private List<TextMeshProUGUI> scores;

    private string publicLeaderboardKey =
        "5b974e95d72d3fca3e4d9d537afe4a904af2082e77cd3be835c21c48c2af34b4";

    private void Start()
    {
        GetLeaderboard();
    }

    public void GetLeaderboard()
    {
        LeaderboardCreator.GetLeaderboard(publicLeaderboardKey, ((msg) =>
        {
            int loopLength = (msg.Length < names.Count ? msg.Length : names.Count);
            for (int i = 0; i < loopLength; i++)
            {
                names[i].text = msg[i].Username;
                scores[i].text = msg[i].Score.ToString();
            }
        }));
    }

    public void SetLeaderboardEntry(string username, int score)
    {
        LeaderboardCreator.UploadNewEntry(publicLeaderboardKey, username, score, ((msg) =>
        {
            LeaderboardCreator.ResetPlayer();
            GetLeaderboard();
        }));
    }
    */
}