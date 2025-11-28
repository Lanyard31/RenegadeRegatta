using UnityEngine;
using System.Collections.Generic;

public class WaterRushController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WindPushNew wind;
    [SerializeField] private AudioSource waterAudio;

    [Header("Volume Settings")]
    [Tooltip("How fast the volume interpolates to its target.")]
    [SerializeField] private float volumeLerpSpeed = 3f;

    [Tooltip("The minimum efficiency before particles fully shut off.")]
    [SerializeField] private float particleCutoff = 0.15f;

    [Header("Water Particles")]
    [Tooltip("Bow spray, wake spray, anything visually implying speed.")]
    [SerializeField] private List<GameObject> waterParticles;

    private float originalVolume;

    // Tracks whether particles are currently enabled to avoid spam toggling
    private bool particlesActive = true;

    void Awake()
    {
        if (waterAudio == null)
        {
            waterAudio = GetComponent<AudioSource>();
        }

        originalVolume = waterAudio.volume;
    }

    void Update()
    {
        float eff = ComputeEfficiency();

        // -------- Volume Lerp --------
        float targetVolume = originalVolume * eff;
        waterAudio.volume = Mathf.Lerp(waterAudio.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);

        // -------- Particle Control --------
        bool shouldBeActive = eff > particleCutoff;

        if (shouldBeActive != particlesActive)
        {
            SetParticlesActive(shouldBeActive);
            particlesActive = shouldBeActive;
        }
    }

    private float ComputeEfficiency()
    {
        // Using WindPushNew’s alignment categories for scaling.
        // This gives us a consistent max-speed curve regardless of hull physics weirdness.
        switch (wind.AlignmentCategory)
        {
            case "Perfect": return 1f;
            case "Broad Reach": return 0.85f;
            case "Beam Reach": return 0.75f;
            case "Close Reach": return 0.65f;
            default: return 0f; // Misaligned, stalled, etc.
        }
    }

    private void SetParticlesActive(bool state)
    {
        for (int i = 0; i < waterParticles.Count; i++)
        {
            if (waterParticles[i] != null)
                waterParticles[i].SetActive(state);
        }
    }

    // Called externally when the boat is launched upward during a ground hit
    public void OnGroundedBounce()
    {
        // Immediately shut particles off since the boat isn't cutting water
        SetParticlesActive(false);
        particlesActive = false;
    }
}
