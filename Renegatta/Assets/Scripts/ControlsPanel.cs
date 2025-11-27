using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ControlsPanel : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Image panelImage;
    [SerializeField] private TMP_Text textElement;

    [Header("Show Animation")]
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float scaleTime = 0.4f;

    [Header("Lifetime")]
    [SerializeField] private float visibleTime = 30f;
    [SerializeField] private float fadeTime = 1f;

    [Header("Settings")]
    public bool AlwaysShow;
    public bool NeverShow;

    private float panelStartAlpha;
    private float textStartAlpha;

    private Coroutine runningRoutine;

    void OnEnable()
    {
        panelStartAlpha = panelImage != null ? panelImage.color.a : 1f;
        textStartAlpha = textElement != null ? textElement.color.a : 1f;

        ApplySettingsImmediate();

        if (NeverShow) return;
        if (AlwaysShow)
        {
            StartPopInOnly();
            return;
        }

        StartNormalRoutine();
    }

    void StartNormalRoutine()
    {
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        runningRoutine = StartCoroutine(ShowRoutine());
    }

    void StartPopInOnly()
    {
        if (runningRoutine != null) StopCoroutine(runningRoutine);
        runningRoutine = StartCoroutine(PopInOnlyRoutine());
    }

    void ApplySettingsImmediate()
    {
        if (NeverShow)
        {
            panel.localScale = Vector3.zero;
            SetAlphaInstant(0f);
            SetChildActive(false);
            return;
        }

        if (AlwaysShow)
        {
            SetChildActive(true);
            SetAlphaInstant(panelStartAlpha);
            panel.localScale = Vector3.one;
        }
    }

    void Update()
    {
        if (NeverShow)
        {
            if (runningRoutine != null) StopCoroutine(runningRoutine);
            SetAlphaInstant(0f);
            panel.localScale = Vector3.zero;
            SetChildActive(false);
        }
        else if (AlwaysShow)
        {
            SetChildActive(true);
            if (panel.localScale == Vector3.zero)
                panel.localScale = Vector3.one;

            SetAlphaInstant(panelStartAlpha);
        }
    }

    private System.Collections.IEnumerator ShowRoutine()
    {
        float t = 0f;
        panel.localScale = Vector3.zero;
        SetChildActive(true);

        while (t < scaleTime)
        {
            if (ShouldInterrupt()) yield break;

            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / scaleTime);
            panel.localScale = Vector3.one * showCurve.Evaluate(f);
            yield return null;
        }

        yield return new WaitForSeconds(visibleTime);

        float ft = 0f;
        while (ft < fadeTime)
        {
            if (ShouldInterrupt()) yield break;

            ft += Time.deltaTime;
            float fade = 1f - (ft / fadeTime);
            SetAlpha(fade);
            yield return null;
        }

        SetChildActive(false);
    }

    private System.Collections.IEnumerator PopInOnlyRoutine()
    {
        float t = 0f;
        panel.localScale = Vector3.zero;
        SetChildActive(true);

        while (t < scaleTime)
        {
            if (ShouldInterrupt()) yield break;

            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / scaleTime);
            panel.localScale = Vector3.one * showCurve.Evaluate(f);
            yield return null;
        }
        // stay visible indefinitely
    }

    private bool ShouldInterrupt()
    {
        return NeverShow || AlwaysShow;
    }

    private void SetAlpha(float factor)
    {
        if (panelImage != null)
        {
            var c = panelImage.color;
            c.a = panelStartAlpha * factor;
            panelImage.color = c;
        }

        if (textElement != null)
        {
            var tc = textElement.color;
            tc.a = textStartAlpha * factor;
            textElement.color = tc;
        }
    }

    private void SetAlphaInstant(float value)
    {
        if (panelImage != null)
        {
            var c = panelImage.color;
            c.a = value;
            panelImage.color = c;
        }

        if (textElement != null)
        {
            var tc = textElement.color;
            tc.a = value;
            textElement.color = tc;
        }
    }

    private void SetChildActive(bool active)
    {
        if (panel != null) panel.gameObject.SetActive(active);
        if (panelImage != null) panelImage.gameObject.SetActive(active);
        if (textElement != null) textElement.gameObject.SetActive(active);
    }
}
