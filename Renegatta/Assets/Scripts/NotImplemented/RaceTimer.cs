using UnityEngine;
using UnityEngine.UI;

public class RaceTimer : MonoBehaviour
{
    public Text timerText;
    public AudioSource tickSource;
    public AudioClip tickClip;

    private float elapsed;
    private bool running;
    private bool playTickSound = true;

    private float nextTickTime;

    void Update()
    {
        if (!running) return;

        elapsed += Time.deltaTime;

        if (playTickSound && elapsed >= nextTickTime)
        {
            nextTickTime = Mathf.Floor(elapsed) + 1f;
            PlayTick();
        }

        timerText.text = FormatTime(elapsed);
    }

    public void StartTimer()
    {
        running = true;
        elapsed = 0f;
        nextTickTime = 1f;
    }

    public void StopTimer()
    {
        running = false;
    }

    public float GetFinalTime() => elapsed;

    public void EnableTickSound(bool enabled)
    {
        playTickSound = enabled;
    }

    private void PlayTick()
    {
        if (tickSource && tickClip)
            tickSource.PlayOneShot(tickClip);
    }

    public static string FormatTime(float t)
    {
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
