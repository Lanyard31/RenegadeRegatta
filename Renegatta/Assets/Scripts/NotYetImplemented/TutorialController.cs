// TutorialController.cs
// Drop onto a scene GameObject (e.g. UI/TutorialController).
// Create the UI layout described, assign references in inspector.
// Uses CanvasGroup fades for smooth in/out and PlayerPrefs to remember completion.

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialController : MonoBehaviour
{
    [Header("PlayerPrefs")]
    [Tooltip("Key used to store tutorial completion")]
    public string prefsKey = "Tutorial_Completed_v1";

    [Header("Timing")]
    public float fadeDuration = 0.35f;
    public float explanationVisibleDelay = 0.25f; // time explanation stays before fade when step completed
    public float betweenStepsDelay = 0.4f;
    public float holdThresholdSeconds = 0.6f; // for Ready/Aim hold

    [Header("References - Panels / CanvasGroups")]
    public CanvasGroup controlsPanelGroup;      // container for bottom-left controls visuals
    public CanvasGroup explanationGroup;        // explanation text panel (middle bottom)
    public TextMeshProUGUI explanationText;     // actual explanation text
    public CanvasGroup controlsStaticGroup;     // optionally used to show static controls once tutorial finished

    [Header("Controls UI - optional granular elements")]
    public GameObject controlSteerGO;   // A / D steer visuals
    public GameObject controlTrimGO;    // Q / E trim visuals
    public GameObject controlFireGO;    // X fire visuals
    public GameObject controlAimGO;     // Z / C aim visuals

    [Header("Input keys (editable for testing)")]
    public KeyCode steerLeft = KeyCode.A;
    public KeyCode steerRight = KeyCode.D;
    public KeyCode trimLeft = KeyCode.Q;
    public KeyCode trimRight = KeyCode.E;
    public KeyCode fire = KeyCode.X;
    public KeyCode aimLeft = KeyCode.Z;
    public KeyCode aimRight = KeyCode.C;

    [Header("Optional: debug / skip")]
    public KeyCode skipKey = KeyCode.Escape;

    // Internal state
    bool tutorialCompleted = false;
    bool waitingForHoldRelease = false;

    // Hooks/Callbacks you can set from other systems:
    // If you have a better way to detect "trim success" (e.g. Broad Reach), set this to return true when appropriate.
    public Func<bool> CheckTrimSuccess = null;        // optional: return true when sails are aligned / broad reach
    public Func<bool> CheckSteerAction = null;        // optional: return true if player has "steered" (helps with custom controllers)
    // Health/misalignment events - call these from your ship/health systems:
    // Example: tutorialController.OnDamageTaken(currentHealth, maxHealth);
    // Example: tutorialController.OnMisalignedTooLong();
    
    void Start()
    {
        tutorialCompleted = PlayerPrefs.GetInt(prefsKey, 0) == 1;

        // Ensure CanvasGroups start hidden or visible depending on completion
        if (tutorialCompleted)
        {
            if (controlsPanelGroup) controlsPanelGroup.alpha = 1f;
            if (explanationGroup) explanationGroup.alpha = 0f;
            if (controlsStaticGroup) controlsStaticGroup.alpha = 1f;
            EnableAllControlVisuals(true);
            // Nothing else - keep static UI
        }
        else
        {
            if (controlsPanelGroup) controlsPanelGroup.alpha = 0f;
            if (explanationGroup) explanationGroup.alpha = 0f;
            if (controlsStaticGroup) controlsStaticGroup.alpha = 0f;
            EnableAllControlVisuals(false);
            StartCoroutine(TutorialSequence());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(skipKey))
        {
            CompleteTutorial();
        }
    }

    IEnumerator TutorialSequence()
    {
        // Step 0: Show basic steer controls A/D and explanation
        yield return FadeInControls(); // reveals container for controls
        RevealControl(controlSteerGO, true);
        yield return FadeInExplanation("Steer the ship using A and D");

        // Wait until user steers (keyboard or custom hook)
        yield return WaitForSteer();

        // Fade out explanation, pause, reveal next controls
        yield return FadeOutExplanation();
        yield return new WaitForSeconds(betweenStepsDelay);

        // Step 1: Trim sails Q/E
        RevealControl(controlTrimGO, true);
        yield return FadeInExplanation("After steering, always trim sails to catch wind using Q or E.");

        // Wait for trim success (hook) or simple Q/E press fallback
        yield return WaitForTrim();

        yield return FadeOutExplanation();
        yield return new WaitForSeconds(betweenStepsDelay);

        // Step 2: Fire cannons X
        RevealControl(controlFireGO, true);
        yield return FadeInExplanation("Fire all cannons by pressing X.");

        // Wait for a press of X
        yield return WaitForKeyPress(fire);

        // Show hold behavior explanation
        yield return FadeOutExplanation();
        yield return new WaitForSeconds(betweenStepsDelay);
        yield return FadeInExplanation("Hold X to Ready and Aim, release X to fire.");

        // Wait for hold+release (detect hold longer than threshold)
        yield return WaitForHoldAndRelease(fire, holdThresholdSeconds);

        yield return FadeOutExplanation();
        yield return new WaitForSeconds(betweenStepsDelay);

        // Step 3: Aim adjustments (Z/C)
        RevealControl(controlAimGO, true);
        yield return FadeInExplanation("Fine tune the aim of your cannons with Z and C. Mind the wave motion.");

        // Wait for Z and C presses or either
        yield return WaitForKeys(new KeyCode[] { aimLeft, aimRight });

        yield return FadeOutExplanation();
        yield return new WaitForSeconds(betweenStepsDelay);

        // Final explanation
        yield return FadeInExplanation("Cross the Finish Line as swiftly as you can");
        yield return new WaitForSeconds(2f);
        yield return FadeOutExplanation();

        // Mark tutorial completed and show static controls (no more explanations)
        CompleteTutorial();
    }

    #region UI helpers

    IEnumerator FadeInControls()
    {
        if (controlsPanelGroup == null) yield break;
        yield return StartCoroutine(FadeCanvasGroup(controlsPanelGroup, controlsPanelGroup.alpha, 1f, fadeDuration));
    }

    IEnumerator FadeInExplanation(string text)
    {
        if (explanationGroup == null || explanationText == null)
        {
            yield break;
        }

        explanationText.text = text;
        yield return StartCoroutine(FadeCanvasGroup(explanationGroup, explanationGroup.alpha, 1f, fadeDuration));
    }

    IEnumerator FadeOutExplanation()
    {
        if (explanationGroup == null) yield break;
        yield return StartCoroutine(FadeCanvasGroup(explanationGroup, explanationGroup.alpha, 0f, fadeDuration));
        yield return new WaitForSeconds(explanationVisibleDelay);
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
            yield break;

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, duration <= 0f ? 1f : t / duration);
            cg.alpha = a;
            cg.interactable = a > 0.9f;
            cg.blocksRaycasts = a > 0.9f;
            yield return null;
        }

        cg.alpha = to;
        cg.interactable = to > 0.9f;
        cg.blocksRaycasts = to > 0.9f;
    }

    void RevealControl(GameObject go, bool reveal)
    {
        if (go == null) return;
        go.SetActive(reveal);
    }

    void EnableAllControlVisuals(bool enable)
    {
        RevealControl(controlSteerGO, enable);
        RevealControl(controlTrimGO, enable);
        RevealControl(controlFireGO, enable);
        RevealControl(controlAimGO, enable);
    }

    #endregion

    #region Waiters / Input detectors

    IEnumerator WaitForSteer()
    {
        // First check: custom hook
        if (CheckSteerAction != null)
        {
            while (!CheckSteerAction())
            {
                if (Input.GetKeyDown(skipKey)) yield break;
                yield return null;
            }
            yield break;
        }

        // Fallback: detect A/D key press movement for at least one press
        bool sawLeft = false, sawRight = false;
        while (!sawLeft && !sawRight)
        {
            if (Input.GetKeyDown(steerLeft)) sawLeft = true;
            if (Input.GetKeyDown(steerRight)) sawRight = true;
            if (Input.GetKeyDown(skipKey)) yield break;
            yield return null;
        }
    }

    IEnumerator WaitForTrim()
    {
        // If the game provides a 'trim success' function (broad reach/perfect alignment), use it.
        if (CheckTrimSuccess != null)
        {
            // allow some fallback time: if hook never returns true, let Q/E presses finish it after 20 seconds
            float timeout = 20f;
            float t = 0f;
            while (t < timeout)
            {
                if (CheckTrimSuccess()) yield break;
                if (Input.GetKeyDown(trimLeft) || Input.GetKeyDown(trimRight)) yield break;
                if (Input.GetKeyDown(skipKey)) yield break;
                t += Time.deltaTime;
                yield return null;
            }

            // timeout: return anyway
            yield break;
        }

        // fallback: wait for Q or E press
        while (!Input.GetKeyDown(trimLeft) && !Input.GetKeyDown(trimRight))
        {
            if (Input.GetKeyDown(skipKey)) yield break;
            yield return null;
        }
    }

    IEnumerator WaitForKeyPress(KeyCode k)
    {
        while (!Input.GetKeyDown(k))
        {
            if (Input.GetKeyDown(skipKey)) yield break;
            yield return null;
        }
    }

    IEnumerator WaitForHoldAndRelease(KeyCode k, float minHold)
    {
        // wait for press
        while (!Input.GetKeyDown(k))
        {
            if (Input.GetKeyDown(skipKey)) yield break;
            yield return null;
        }

        float pressTime = Time.time;
        // wait until release
        while (Input.GetKey(k))
        {
            yield return null;
        }
        float held = Time.time - pressTime;
        if (held >= minHold)
        {
            // done - they held and released
            yield break;
        }
        else
        {
            // They tapped short - we can either loop and ask again, or accept a tap.
            // The spec wanted a hold+release detection, so retry until they hold enough.
            yield return WaitForHoldAndRelease(k, minHold);
        }
    }

    IEnumerator WaitForKeys(KeyCode[] keys)
    {
        // Wait for any of the given keys to be pressed at least once
        bool any = false;
        while (!any)
        {
            foreach (var k in keys)
            {
                if (Input.GetKeyDown(k)) { any = true; break; }
            }
            if (Input.GetKeyDown(skipKey)) yield break;
            yield return null;
        }
    }

    #endregion

    #region External hooks - call these from your ship/game systems

    // Call this when ship takes damage; tutorial will show a recurring explanation when first time below 80%
    public void OnDamageTaken(float currentHealth, float maxHealth)
    {
        if (tutorialCompleted) return;

        if (maxHealth <= 0) return;
        float pct = currentHealth / maxHealth;
        if (pct <= 0.8f)
        {
            // show a one-off explanation (doesn't affect saved state)
            StopAllCoroutines(); // interrupt tutorial so the player sees the damage explanation
            StartCoroutine(ShowTransientExplanation("Damage is repaired automatically over time, but the ship moves slower while repairs are underway", 3f, resumeTutorial: true));
        }
    }

    // Call when player stays misaligned too long (non-persistent hint)
    public void OnMisalignedTooLong()
    {
        if (tutorialCompleted) return;
        StartCoroutine(ShowTransientExplanation("Trim sails to catch the wind with Q and E", 2.5f, resumeTutorial: false));
    }

    IEnumerator ShowTransientExplanation(string text, float duration, bool resumeTutorial)
    {
        // Fade in explanation, wait, fade out. If resumeTutorial==true, restart the main sequence where it left off.
        if (explanationGroup == null || explanationText == null)
        {
            yield break;
        }

        explanationText.text = text;
        yield return StartCoroutine(FadeCanvasGroup(explanationGroup, explanationGroup.alpha, 1f, fadeDuration));
        yield return new WaitForSeconds(duration);
        yield return StartCoroutine(FadeCanvasGroup(explanationGroup, explanationGroup.alpha, 0f, fadeDuration));
        // If tutorial not completed and resume requested, resume main sequence from start of whichever step makes sense
        if (resumeTutorial && !tutorialCompleted)
        {
            // naive approach: restart the main coroutine. This keeps logic simple and robust.
            StartCoroutine(TutorialSequence());
        }
    }

    #endregion

    void CompleteTutorial()
    {
        tutorialCompleted = true;
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();

        // Make sure everything visible & static
        if (controlsPanelGroup) controlsPanelGroup.alpha = 1f;
        if (explanationGroup) explanationGroup.alpha = 0f;
        if (controlsStaticGroup) controlsStaticGroup.alpha = 1f;
        EnableAllControlVisuals(true);

        StopAllCoroutines();
    }

    #region Editor safety
#if UNITY_EDITOR
    void OnValidate()
    {
        // Simple safety: make sure CanvasGroups exist for sensible default values
        if (fadeDuration < 0.05f) fadeDuration = 0.05f;
        if (holdThresholdSeconds < 0f) holdThresholdSeconds = 0.1f;
    }
#endif
    #endregion
}
