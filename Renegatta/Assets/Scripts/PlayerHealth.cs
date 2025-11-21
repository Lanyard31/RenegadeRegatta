using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float healthMax = 100f;
    [SerializeField] private float health;

    [Header("Fires (assign particle GameObjects)")]
    public GameObject fire80;
    public GameObject fire50;
    public GameObject fire30;

    [Header("Invulnerability / Hitflash")]
    public float invulnerabilityDuration = 0.6f; // during which further damage is ignored
    public float regenDelayAfterInvuln = 0.8f;    // wait after invuln then start regen
    public float regenRatePerSecond = 8f;        // health per second while repairing

    [Header("Penalty to WindPush (0..1)")]
    public AnimationCurve penaltyCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Audio / SFX")]
    public AudioSource repairAudioSource; // looping carpenters sound, will be played while regenerating

    [Header("Death")]
    public GameObject shipwreckedUIPanel; // assign a panel that says "Shipwrecked!"
    public float deathTiltZ = 20f;
    public float deathTiltX = 10f;
    public float sinkDistance = 5f;
    public float sinkDuration = 3f;
    public float deathDelayBeforeReload = 2f;

    [Header("Disable on death")]
    public Behaviour[] componentsToDisable; // e.g. player input, WindPushNew, other controllers

    [Header("References")]
    public MonoBehaviour hitFlash; // script that provides a Flash() method (call accordingly)

    // Events
    public event Action<float> OnHealthChanged; // current health (0..healthMax)
    public event Action<float> OnHealthPenaltyChanged; // 0..1
    public event Action OnDeath;

    // Internal state
    private bool isDead = false;
    private bool isInvulnerable = false;
    private Coroutine regenCoroutine;
    private Coroutine invulnCoroutine;

    void Awake()
    {
        health = healthMax;
        UpdateFires();
        PushPenalty();
    }

    #region Public API
    /// <summary>
    /// Apply damage to the ship. Will be ignored if currently invulnerable or dead.
    /// </summary>
    public void ApplyDamage(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;
        if (isInvulnerable) return;

        health = Mathf.Max(0f, health - amount);
        OnHealthChanged?.Invoke(health);
        UpdateFires();
        PushPenalty();

        // Trigger hit flash if provided
        TryInvokeHitFlash();

        // stop any active regen and start invulnerability+delayed regen
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }

        if (invulnCoroutine != null)
            StopCoroutine(invulnCoroutine);
        invulnCoroutine = StartCoroutine(InvulnThenRegen());

        if (health <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Heal the ship instantly (e.g. repair item). This will also update fires and penalty.
    /// </summary>
    public void HealInstant(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;
        health = Mathf.Min(healthMax, health + amount);
        OnHealthChanged?.Invoke(health);
        UpdateFires();
        PushPenalty();
    }

    /// <summary>
    /// Force-start regeneration (useful for debugging).
    /// </summary>
    public void StartRegenNow()
    {
        if (isDead) return;
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        regenCoroutine = StartCoroutine(RegenRoutine());
    }
    #endregion

    #region Internals
    private IEnumerator InvulnThenRegen()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(invulnerabilityDuration);
        isInvulnerable = false;

        // small delay before repair starts
        yield return new WaitForSeconds(regenDelayAfterInvuln);

        // start regen
        //only start regen if health is below 80
        if (health < healthMax * 0.8f)
        {
            if (regenCoroutine != null) StopCoroutine(regenCoroutine);
            regenCoroutine = StartCoroutine(RegenRoutine());
        }
    }

    private IEnumerator RegenRoutine()
    {
        // play repair audio if assigned
        if (repairAudioSource != null)
        {
            repairAudioSource.loop = true;
            if (!repairAudioSource.isPlaying) repairAudioSource.Play();
        }

        while (!isDead && health < healthMax)
        {
            health = Mathf.Min(healthMax, health + regenRatePerSecond * Time.deltaTime);
            OnHealthChanged?.Invoke(health);
            UpdateFires();
            PushPenalty();
            yield return null;
        }

        // stop audio when finished
        if (repairAudioSource != null && repairAudioSource.isPlaying)
            repairAudioSource.Stop();

        regenCoroutine = null;
    }

    private void TryInvokeHitFlash()
    {
        if (hitFlash == null) return;

        // Try common method names by reflection, in case your hitFlash class has a different signature.
        var type = hitFlash.GetType();
        var method = type.GetMethod("Flash");
        if (method != null)
        {
            method.Invoke(hitFlash, null);
            return;
        }

        method = type.GetMethod("DoFlash");
        if (method != null) method.Invoke(hitFlash, null);
    }

    private void UpdateFires()
    {
        // thresholds
        float t80 = healthMax * 0.8f;
        float t50 = healthMax * 0.5f;
        float t30 = healthMax * 0.3f;


        // fire80: ignite at <=80, stay on until fully repaired (health == healthMax)
        if (fire80 != null)
        {
            bool shouldBeOn = (health <= t80) || (fire80.activeSelf && health < healthMax);
            fire80.SetActive(shouldBeOn);
        }


        // fire50: ignite at <=50, stay on until health rises to 80 or above
        if (fire50 != null)
        {
            bool shouldBeOn = (health <= t50) || (fire50.activeSelf && health < t80);
            fire50.SetActive(shouldBeOn);
        }


        // fire30: ignite at <=30, stay on until health rises to 50 or above
        if (fire30 != null)
        {
            bool shouldBeOn = (health <= t30) || (fire30.activeSelf && health < t50);
            fire30.SetActive(shouldBeOn);
        }
    }

    /// <summary>
    /// Calculate penalty (0..1) and broadcast it. By default it's linear but the AnimationCurve lets you tweak how severe.
    /// </summary>
    private void PushPenalty()
    {
        float t = 1f - (health / healthMax); // 0 when full, 1 when empty
        float penalty = Mathf.Clamp01(penaltyCurve.Evaluate(t));
        OnHealthPenaltyChanged?.Invoke(penalty);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // stop coroutines and audio
        if (regenCoroutine != null) StopCoroutine(regenCoroutine);
        if (invulnCoroutine != null) StopCoroutine(invulnCoroutine);
        if (repairAudioSource != null && repairAudioSource.isPlaying) repairAudioSource.Stop();

        // disable specified components immediately
        foreach (var c in componentsToDisable)
        {
            if (c != null) c.enabled = false;
        }

        // deactivate fires
        if (fire80 != null) fire80.SetActive(false);
        if (fire50 != null) fire50.SetActive(false);
        if (fire30 != null) fire30.SetActive(false);

        OnDeath?.Invoke();

        // start the death animation and scene reload
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        // Show UI panel if assigned
        if (shipwreckedUIPanel != null) shipwreckedUIPanel.SetActive(true);

        // Simple tilt+sink animation applied in local space
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = Quaternion.Euler(deathTiltX, startRot.eulerAngles.y, startRot.eulerAngles.z + deathTiltZ);
        Vector3 targetPos = startPos - Vector3.up * sinkDistance;

        float elapsed = 0f;
        while (elapsed < sinkDuration)
        {
            float t = elapsed / sinkDuration;
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = targetRot;
        transform.localPosition = targetPos;

        yield return new WaitForSeconds(deathDelayBeforeReload);

        // Reload active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    #endregion

    // Editor helper: keep fires and penalty correct if values are changed in edit mode
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            health = Mathf.Clamp(health, 0f, healthMax);
            UpdateFires();
        }
    }
#endif
}
