// PlayerHealth.cs (cleaned, death removed and extras commented at bottom)
using System;
using System.Collections;
using TrajectoryExample;
using UnityEngine;

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
    public float invulnerabilityDuration = 0.6f;
    public float regenDelayAfterInvuln = 0.8f;
    public float regenRatePerSecond = 8f;

    [Header("Penalty to WindPush (0..1)")]
    public AnimationCurve penaltyCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Audio / SFX")]    
    public AudioSource repairAudioSource;

    [Header("References")]    
    public MonoBehaviour hitFlash;

    // Events
    public event Action<float> OnHealthChanged;
    public event Action<float> OnHealthPenaltyChanged;
    public event Action OnDeath;

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
    public void ApplyDamage(float amount)
    {
        if (amount <= 0f) return;
        if (isInvulnerable) return;

        health = Mathf.Max(0f, health - amount);
        OnHealthChanged?.Invoke(health);
        UpdateFires();
        PushPenalty();
        TryInvokeHitFlash();

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
            OnDeath?.Invoke();
        }
    }

    public void HealInstant(float amount)
    {
        if (amount <= 0f) return;

        health = Mathf.Min(healthMax, health + amount);
        OnHealthChanged?.Invoke(health);
        UpdateFires();
        PushPenalty();
    }

    public void StartRegenNow()
    {
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

        yield return new WaitForSeconds(regenDelayAfterInvuln);

        if (health < healthMax * 0.8f)
        {
            if (regenCoroutine != null) StopCoroutine(regenCoroutine);
            regenCoroutine = StartCoroutine(RegenRoutine());
        }
    }

    private IEnumerator RegenRoutine()
    {
        if (repairAudioSource != null)
        {
            repairAudioSource.loop = true;
            if (!repairAudioSource.isPlaying) repairAudioSource.Play();
        }

        while (health < healthMax)
        {
            health = Mathf.Min(healthMax, health + regenRatePerSecond * Time.deltaTime);
            OnHealthChanged?.Invoke(health);
            UpdateFires();
            PushPenalty();
            yield return null;
        }

        if (repairAudioSource != null && repairAudioSource.isPlaying)
            repairAudioSource.Stop();

        regenCoroutine = null;
    }

    private void TryInvokeHitFlash()
    {
        if (hitFlash == null) return;
        var type = hitFlash.GetType();
        var method = type.GetMethod("Flash") ?? type.GetMethod("DoFlash");
        if (method != null) method.Invoke(hitFlash, null);
    }

    private void UpdateFires()
    {
        float t80 = healthMax * 0.8f;
        float t50 = healthMax * 0.5f;
        float t30 = healthMax * 0.3f;

        if (fire80 != null)
        {
            bool on = (health <= t80) || (fire80.activeSelf && health < healthMax);
            fire80.SetActive(on);
        }

        if (fire50 != null)
        {
            bool on = (health <= t50) || (fire50.activeSelf && health < t80);
            fire50.SetActive(on);
        }

        if (fire30 != null)
        {
            bool on = (health <= t30) || (fire30.activeSelf && health < t50);
            fire30.SetActive(on);
        }

    }

    private void PushPenalty()
    {
        float t = 1f - (health / healthMax);
        float penalty = Mathf.Clamp01(penaltyCurve.Evaluate(t));
        OnHealthPenaltyChanged?.Invoke(penalty);
    }

    //lambda expression to get current health 
    public float GetHealthValue() => health;

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

    #endregion

    // ---------------------------------------------------------------------
    // PARKED EXTRA FUNCTIONS (commented out)
// ---------------------------------------------------------------------
/*
$1*/
}
