using UnityEngine;
using TMPro;
using EasyTransition;
using System.Collections;

public class IntroTextSequence : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI textElement;

    [Header("Transition")]
    public TransitionManager transitionManager;
    public TransitionSettings transitionSettings;
    public float transitionDuration = 1f;
    public string nextSceneName = "SampleScene";

    [Header("Timings")]
    public float fadeInDuration = 2f;
    public float holdDuration = 3f;

    bool isSkipping = false;

    void Start()
    {
        // Start fully transparent
        var c = textElement.color;
        c.a = 0f;
        textElement.color = c;

        StartCoroutine(RunSequence());
    }

    IEnumerator RunSequence()
    {
        // Fade in
        float t = 0f;
        while (t < fadeInDuration)
        {
            if (CheckSkip()) yield break;

            t += Time.deltaTime;
            var c = textElement.color;
            c.a = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            textElement.color = c;

            yield return null;
        }

        // Hold
        float hold = 0f;
        while (hold < holdDuration)
        {
            if (CheckSkip()) yield break;

            hold += Time.deltaTime;
            yield return null;
        }

        // Auto transition
        TriggerTransition();
    }

    bool CheckSkip()
    {
        if (isSkipping) return true;

        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            isSkipping = true;
            TriggerTransition();
            return true;
        }

        return false;
    }

    public void TriggerTransition()
    {
        transitionManager.Transition(nextSceneName, transitionSettings, transitionDuration);
    }
}
