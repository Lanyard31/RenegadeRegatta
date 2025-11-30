using UnityEngine;
using TMPro;
using System.Collections;

public class VictoryGradeDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VictoryHandler victoryHandler;
    [SerializeField] private TMP_Text gradeText;

    [Header("Animation")]
    [SerializeField] private float popDuration = 0.35f;
    [SerializeField] private float popOvershoot = 1.25f;

    private void OnEnable()
    {
        transform.localScale = Vector3.zero;
        StartCoroutine(RevealRoutine());
    }

    private IEnumerator RevealRoutine()
    {
        yield return null; // One frame so the panel can finish enabling

        float time = victoryHandler.Timer;
        gradeText.text = GetLetterGrade(time);

        // Scale pop animation
        float t = 0f;
        Vector3 start = Vector3.zero;
        Vector3 over = Vector3.one * popOvershoot;
        Vector3 final = Vector3.one;

        // Scale up to overshoot
        while (t < popDuration)
        {
            float pct = t / popDuration;
            transform.localScale = Vector3.Lerp(start, over, pct);
            t += Time.deltaTime;
            yield return null;
        }

        // Then settle back down to 1
        t = 0f;
        float settleTime = popDuration * 0.5f;

        while (t < settleTime)
        {
            float pct = t / settleTime;
            transform.localScale = Vector3.Lerp(over, final, pct);
            t += Time.deltaTime;
            yield return null;
        }

        transform.localScale = final;
    }

    private string GetLetterGrade(float time)
    {
        float minutes = time / 60f;

        if (minutes > 12f) return "C";
        if (minutes > 6f) return "B";
        if (minutes > 5f) return "A";
        if (minutes > 4.5f) return "A+";
        if (minutes > 4f) return "S";
        return "S+";
    }
}
