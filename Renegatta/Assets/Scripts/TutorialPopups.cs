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

    [Header("Game Refs")]
    [SerializeField] private WindPushNew wind;   // Optional manual assignment
    [SerializeField] private MusicController musicController;
    [SerializeField] private GameObject[] NonBossTentacles;

    private bool active;
    private float timer;
    private float completionTimer;
    private bool completionSatisfied;
    private System.Action completionCheck;
    private float panelStartAlpha;
    private float textStartAlpha;

    private const string PPKey = "TutorialSeenCount";

    void Awake()
    {
        if (wind == null)
            wind = GetComponent<WindPushNew>();

        // Cache actual Inspector alpha
        panelStartAlpha = panelImage != null ? panelImage.color.a : 1f;
        textStartAlpha = textElement != null ? textElement.color.a : 1f;

        // Start hidden, but preserve the original alpha for fade
        panel.localScale = Vector3.zero;
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        TutorialTrigger.OnEntered += HandleTriggerEntered;
    }

    private void OnDisable()
    {
        TutorialTrigger.OnEntered -= HandleTriggerEntered;
    }

    private void HandleTriggerEntered(TutorialTrigger trigger)
    {
        if (PlayerPrefs.GetInt(PPKey, 0) >= 2) return;

        switch (trigger.kind)
        {
            case TutorialTrigger.TriggerKind.HowToSail:
                Debug.Log("triggerHowToSail");
                Show("Press <sprite name=q> and <sprite name=e> to rotate your sails so that they fill with wind. Remember to check your sail trim regularly." + System.Environment.NewLine + "Go Explore The Archipelago.", CheckSailInput);
                break;

            case TutorialTrigger.TriggerKind.HowToTack:
                Show("To sail upwind, steer to a slight angle off the direction of the wind's origin. Turn sails to catch wind best as possible. Repeat as necessary, passing the bow of the ship through the wind. Trim sails accordingly.", CheckTackAlignment);
                break;

            case TutorialTrigger.TriggerKind.CompletionSave:
                int count = PlayerPrefs.GetInt(PPKey, 0);
                PlayerPrefs.SetInt(PPKey, count + 1);
                PlayerPrefs.Save();
                break;
        }
    }

    void Update()
    {
        //if I press T, reset PlayerPrefs
        if (Input.GetKeyDown(KeyCode.T))
            PlayerPrefs.DeleteAll();


        if (!active) return;

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

    // SHOW / HIDE (unchanged)...
    private void Show(string text, System.Action completionCondition)
    {
        panel.gameObject.SetActive(true);
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

    // COMPLETION CONDITIONS
    private void CheckSailInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            completionSatisfied = true;
    }

    private void CheckTackAlignment()
    {
        if (wind != null && wind.AlignmentCategory == "Close Reach")
            completionSatisfied = true;
    }

}
