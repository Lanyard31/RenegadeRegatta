using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutorialPopups : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private Image panelImage;
    [SerializeField] private TMP_Text textElement;

    [Header("Animation")]
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [SerializeField] private float animDuration = 0.4f;

    [Header("Tutorial Settings")]
    [SerializeField] private float minDisplayTime = 2.5f;
    [SerializeField] private float completionDelay = 6f;

    [Header("Triggers")]
    [SerializeField] private Collider triggerHowToSail;
    [SerializeField] private Collider triggerHowToTack;
    [SerializeField] private Collider triggerCompletionSave;

    [Header("Game Refs")]
    [SerializeField] private WindPushNew wind;   // Optional manual assignment

    private bool active;
    private float timer;
    private float completionTimer;
    private bool completionSatisfied;
    private System.Action completionCheck;

    private const string PPKey = "TutorialSeenCount";

    void Awake()
    {
        if (wind == null)
            wind = GetComponent<WindPushNew>();

        panel.localScale = Vector3.zero;
        SetAlpha(0);
    }

    void Update()
    {
        if (!active)
            return;

        timer += Time.deltaTime;

        if (!completionSatisfied && completionCheck != null)
            completionCheck.Invoke();

        if (completionSatisfied)
        {
            completionTimer += Time.deltaTime;

            if (completionTimer >= completionDelay && timer >= minDisplayTime)
                Hide();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (PlayerPrefs.GetInt(PPKey, 0) >= 2)
            return;

        if (other == triggerHowToSail)
        {
            Show(
                "HOW TO SAIL\nPress Q or E to rotate your sails so that they fill with wind. Check your sail trim continuously.",
                CheckSailInput);
        }
        else if (other == triggerHowToTack)
        {
            Show(
                "HOW TO TACK\nTo sail towards the wind, first turn the ship to be only slightly angled away from the direction of the wind's origin. Then rotate your sails to catch the wind as best you can until they can't go further. If you're about to hit the rocks, turn the ship and pass the bow of the ship through the direction the wind is coming from. At the same time, rotate your sails to the opposite side all the way.",
                CheckTackAlignment);
        }
        else if (other == triggerCompletionSave)
        {
            int count = PlayerPrefs.GetInt(PPKey, 0);
            PlayerPrefs.SetInt(PPKey, count + 1);
            PlayerPrefs.Save();
        }
    }

    // SHOW -----------------------------------------------------------
    private void Show(string text, System.Action completionCondition)
    {
        textElement.text = text;
        timer = 0;
        completionTimer = 0;
        completionSatisfied = false;
        completionCheck = completionCondition;

        active = true;
        StopAllCoroutines();
        StartCoroutine(AnimateShow());
    }

    private System.Collections.IEnumerator AnimateShow()
    {
        float t = 0;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float x = showCurve.Evaluate(t / animDuration);
            panel.localScale = Vector3.one * x;
            SetAlpha(x);
            yield return null;
        }

        panel.localScale = Vector3.one;
        SetAlpha(1);
    }

    // HIDE -----------------------------------------------------------
    private void Hide()
    {
        active = false;
        StopAllCoroutines();
        StartCoroutine(AnimateHide());
    }

    private System.Collections.IEnumerator AnimateHide()
    {
        float t = 0;
        while (t < animDuration)
        {
            t += Time.deltaTime;
            float x = hideCurve.Evaluate(t / animDuration);
            panel.localScale = Vector3.one * x;
            SetAlpha(x);
            yield return null;
        }

        panel.localScale = Vector3.zero;
        SetAlpha(0);
        completionCheck = null;
    }

    private void SetAlpha(float a)
    {
        var c = panelImage.color;
        c.a = a;
        panelImage.color = c;

        var tc = textElement.color;
        tc.a = a;
        textElement.color = tc;
    }

    // COMPLETION CONDITIONS -------------------------------------------
    private void CheckSailInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            completionSatisfied = true;
    }

    private void CheckTackAlignment()
    {
        // Your WindPushNew has a private getter returning a string category
        // e.g. wind.AlignmentCategory
        if (wind.AlignmentCategory == "Close Reach")
            completionSatisfied = true;
    }
}
