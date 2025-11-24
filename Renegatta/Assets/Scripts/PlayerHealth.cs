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
    public ParticleSystem[] fire85;
    public ParticleSystem[] fire50;
    public ParticleSystem[] fire30;

    [Header("Invulnerability / Hitflash")]
    public float invulnerabilityDuration = 0.6f;
    public float regenDelayAfterInvuln = 0.8f;
    public float regenRatePerSecond = 8f;

    [Header("Penalty to WindPush (0..1)")]
    public AnimationCurve penaltyCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Audio / SFX")]
    public AudioSource repairAudioSource;
    public AudioSource fireAudioSource;
    public AudioSource repairInterruptAudioSource;
    float fireSFXvolumeOriginal;
    float repairInterruptAudioSourceVolumeOriginal;

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
        fireSFXvolumeOriginal = fireAudioSource.volume;
        repairInterruptAudioSourceVolumeOriginal = repairInterruptAudioSource.volume;
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
            repairAudioSource.Stop();
            repairInterruptAudioSource.volume = UnityEngine.Random.Range(0.9f, 1.1f) * repairInterruptAudioSourceVolumeOriginal;
            repairInterruptAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            repairInterruptAudioSource.Play();
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

        if (health < healthMax * 0.85f)
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
        float t85 = healthMax * 0.85f;
        float t50 = healthMax * 0.50f;
        float t30 = healthMax * 0.30f;

        // 85% fires (turn off only when fully healed)
        UpdateFireGroup(fire85, t85, healthMax);

        // 50% fires (turn off when healed above 85%)
        UpdateFireGroup(fire50, t50, t85);

        // 30% fires (turn off when healed above 50%)
        UpdateFireGroup(fire30, t30, t50);

        // Fire audio
        if (fireAudioSource != null)
        {
            float damage = 1f - (health / Mathf.Max(healthMax, 0.001f));
            float target = Mathf.Lerp(0f, fireSFXvolumeOriginal, damage);
            fireAudioSource.volume = Mathf.Lerp(fireAudioSource.volume, target, Time.deltaTime * 5f);
        }
    }

    private void UpdateFireGroup(ParticleSystem[] systems, float triggerThreshold, float resetThreshold)
    {
        if (systems == null || systems.Length == 0) return;

        // Evaluate whether any fire in this group is “on”
        bool anyActive = false;
        foreach (var ps in systems)
        {
            if (ps != null && ps.gameObject.activeSelf)
            {
                anyActive = true;
                break;
            }
        }

        bool shouldTurnOn = health <= triggerThreshold;
        bool shouldTurnOff = anyActive && health >= resetThreshold;

        foreach (var ps in systems)
        {
            if (ps == null) continue;
            var go = ps.gameObject;

            if (shouldTurnOn)
            {
                go.SetActive(true);
                ps.Play();
            }
            else if (go.activeSelf && shouldTurnOff)
            {
                ps.Stop();
                //go.SetActive(false);
            }
            else if (go.activeSelf)
            {
                // keep it playing; avoids restart if looping is disabled
                if (!ps.isPlaying) ps.Play();
            }
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
}
