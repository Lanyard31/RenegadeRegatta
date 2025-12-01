using UnityEngine;
using System.Collections.Generic;

public class WaterRushController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WindPushNew wind;
    [SerializeField] private AudioSource waterAudio;

    [Header("Volume Settings")]
    [SerializeField] private float volumeLerpSpeed = 3f;

    [Tooltip("The minimum efficiency before particles fully shut off.")]
    [SerializeField] private float particleCutoff = 0.15f;

    [Header("Water Particles & VFX")]
    [SerializeField] private List<GameObject> waterParticles;

    private float originalVolume;

    [SerializeField] private float particleResetTimer = 1.5f;
    private float particleTimerRemaining = 0f;

    private bool particlesActive = true;

    void Awake()
    {
        if (waterAudio == null)
            waterAudio = GetComponent<AudioSource>();

        originalVolume = waterAudio.volume;
    }

    void Update()
    {
        float eff = Mathf.Clamp(ComputeEfficiency(), 0.001f, 1f);

        // Volume control
        float targetVolume = Mathf.Clamp(originalVolume * eff, 0.001f, originalVolume);
        waterAudio.volume = Mathf.Lerp(waterAudio.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);

        // --- 1. Cooldown state ---
        if (particleTimerRemaining > 0f)
        {
            particleTimerRemaining -= Time.deltaTime;

            if (particlesActive)
                SetParticles(false);

            return;
        }

        bool shouldBeActive = eff > particleCutoff;

        // --- 2. Reactivate ---
        if (shouldBeActive && !particlesActive)
        {
            SetParticles(true);
            return;
        }

        // --- 3. Deactivate ---
        if (!shouldBeActive && particlesActive)
        {
            SetParticles(false);
        }
    }

    // Efficiency from wind categories
    private float ComputeEfficiency()
    {
        switch (wind.AlignmentCategory)
        {
            case "Perfect": return 1f;
            case "Broad Reach": return 0.85f;
            case "Beam Reach": return 0.75f;
            case "Close Reach": return 0.65f;
            default: return 0.001f;
        }
    }

    // Unified activation handler
    private void SetParticles(bool on)
    {
        particlesActive = on;

        for (int i = 0; i < waterParticles.Count; i++)
        {
            var go = waterParticles[i];
            if (go == null) continue;

            go.SetActive(on);

            if (on)
            {
                // ParticleSystem
                var ps = go.GetComponent<ParticleSystem>();
                if (ps != null) ps.Play(true);

                // VFX Graph
                var vfx = go.GetComponent<UnityEngine.VFX.VisualEffect>();
                if (vfx != null) vfx.Play();

                // TrailRenderer
                var trail = go.GetComponent<TrailRenderer>();
                if (trail != null) trail.Clear();
            }
        }
    }

    // External bounce trigger
    public void OnGroundedBounce()
    {
        SetParticles(false);
        particleTimerRemaining = particleResetTimer;
    }
}
