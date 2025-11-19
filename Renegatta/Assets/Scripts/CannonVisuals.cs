using UnityEngine;
using System;
using System.Collections;

public class CannonVisuals : MonoBehaviour
{
    [Header("External")]
    public ShipCannonController controller;

    [Header("Transforms")]
    public Transform barrel;
    public Transform legs;

    [Header("Recoil Settings")]
    public float recoilDistance = 0.25f;
    public float recoilDuration = 0.08f;
    public float returnPause = 0.15f;
    public float returnDuration = 0.15f;
    public float barrelDownAngle = 12f;

    [Header("Barrel Boom Scaling")]
    public float scaleBoomAmount = 1.15f;
    public float scaleBoomDuration = 0.1f;

    private ParticleSystem smokeParticles;

    [Header("Material Swap")]
    public Material firedMaterial; // the emissive one
    private Material originalMaterial;
    private Renderer barrelRenderer;
    public float emissionFadeDuration = 1.5f;
    bool _isRecoiling = false;

    void Awake()
    {
        barrelRenderer = barrel.GetComponent<Renderer>();
        originalMaterial = barrelRenderer.material;
        controller.OnCannonVisualSignal += HandleVisualSignal;

        //get the particle system from your children
        smokeParticles = transform.GetChild(0).GetComponent<ParticleSystem>();
    }

    void OnDestroy()
    {
        if (controller != null)
            controller.OnCannonVisualSignal -= HandleVisualSignal;
    }

    // -------------------------------------------------------
    // MAIN VISUAL DECISION
    // -------------------------------------------------------
    void HandleVisualSignal(float timerRatio)
    {
        // 0 → fired
        if (timerRatio <= 0.001f)
        {
            TriggerRecoil();
            TriggerMaterialSwap();
            TriggerBoomScale();
            StartSmoke();
            return;
        }
    }

    // -------------------------------------------------------
    // RECOIL
    // -------------------------------------------------------
    void TriggerRecoil()
    {
        if (!_isRecoiling)
            StartCoroutine(RecoilRoutine());
    }

    IEnumerator RecoilRoutine()
    {
        _isRecoiling = true;

        Vector3 barrelStartPos = barrel.localPosition;
        Vector3 legsStartPos = legs.localPosition;

        Vector3 offset = Vector3.up * recoilDistance * UnityEngine.Random.Range(0.8f, 1.1f);

        Vector3 barrelEndPos = barrelStartPos + offset;
        Vector3 legsEndPos = legsStartPos + offset;

        Quaternion barrelStartRot = barrel.localRotation;
        Quaternion barrelDownRot = barrelStartRot * Quaternion.Euler(barrelDownAngle, 0f, UnityEngine.Random.Range(-20f, 20f));

        float t = 0f;
        while (t < recoilDuration)
        {
            float f = t / recoilDuration;
            barrel.localPosition = Vector3.Lerp(barrelStartPos, barrelEndPos, f);
            legs.localPosition = Vector3.Lerp(legsStartPos, legsEndPos, f);
            barrel.localRotation = Quaternion.Slerp(barrelStartRot, barrelDownRot, f);
            t += Time.deltaTime;
            yield return null;
        }

        barrel.localPosition = barrelEndPos;
        legs.localPosition = legsEndPos;
        barrel.localRotation = barrelDownRot;

        yield return new WaitForSeconds(returnPause);
        StopSmoke();

        t = 0f;
        while (t < returnDuration)
        {
            float f = t / returnDuration;
            barrel.localPosition = Vector3.Lerp(barrelEndPos, barrelStartPos, f);
            legs.localPosition = Vector3.Lerp(legsEndPos, legsStartPos, f);
            barrel.localRotation = Quaternion.Slerp(barrelDownRot, barrelStartRot, f);
            t += Time.deltaTime;
            yield return null;
        }

        barrel.localPosition = barrelStartPos;
        legs.localPosition = legsStartPos;
        barrel.localRotation = barrelStartRot;

        _isRecoiling = false;
    }

    // -------------------------------------------------------
    // BOOM SCALE
    // -------------------------------------------------------
    void TriggerBoomScale()
    {
        if (barrel != null)
            StartCoroutine(ScaleBoomRoutine());
    }

    IEnumerator ScaleBoomRoutine()
    {
        Vector3 original = barrel.localScale;
        Vector3 target = original * scaleBoomAmount;

        float t = 0f;
        while (t < scaleBoomDuration)
        {
            float f = t / scaleBoomDuration;
            barrel.localScale = Vector3.Lerp(original, target, f);
            t += Time.deltaTime;
            yield return null;
        }

        barrel.localScale = target;

        t = 0f;
        while (t < scaleBoomDuration)
        {
            float f = t / scaleBoomDuration;
            barrel.localScale = Vector3.Lerp(target, original, f);
            t += Time.deltaTime;
            yield return null;
        }

        barrel.localScale = original;
    }

    void TriggerMaterialSwap()
    {
        if (barrelRenderer == null || firedMaterial == null || originalMaterial == null)
            return;

        StartCoroutine(MaterialSwapRoutine());
    }

    IEnumerator MaterialSwapRoutine()
    {
        barrelRenderer.material = firedMaterial;

        yield return new WaitForSeconds(emissionFadeDuration); // how long the cannon looks “hot”

        barrelRenderer.material = originalMaterial;
    }

    // -------------------------------------------------------
    // SMOKE
    // -------------------------------------------------------
    void StartSmoke()
    {
        if (smokeParticles != null)
            smokeParticles.Play();
    }

    void StopSmoke()
    {
        if (smokeParticles != null)
            smokeParticles.Stop();
    }
}
