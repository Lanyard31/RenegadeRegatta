using UnityEngine;
using System.Collections;

public class ShipHullSquash : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float duration = 0.06f;
    [SerializeField] private float amount = 0.1f; 
    // 0.1 = 10% deformation in each direction.

    private Vector3 originalScale;
    private Coroutine routine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void TriggerSquash()
    {
        if (routine != null)
            StopCoroutine(routine);
        routine = StartCoroutine(SquashAndSquish());
    }

    private IEnumerator SquashAndSquish()
    {
        float half = duration * 0.5f;

        // Phase 1: Squash (Y smaller, XZ bigger)
        Vector3 squashScale = new Vector3(
            originalScale.x * (1f + amount),
            originalScale.y * (1f - amount),
            originalScale.z * (1f + amount)
        );

        // Phase 2: Squish (Y bigger, XZ smaller)
        Vector3 squishScale = new Vector3(
            originalScale.x * (1f - amount),
            originalScale.y * (1f + amount),
            originalScale.z * (1f - amount)
        );

        float t = 0f;

        // Squash
        while (t < half)
        {
            float k = t / half;
            transform.localScale = Vector3.Lerp(originalScale, squashScale, k);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = squashScale;

        // Squish
        t = 0f;
        while (t < half)
        {
            float k = t / half;
            transform.localScale = Vector3.Lerp(squashScale, squishScale, k);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = squishScale;

        // Return to original
        t = 0f;
        while (t < half)
        {
            float k = t / half;
            transform.localScale = Vector3.Lerp(squishScale, originalScale, k);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localScale = originalScale;

        routine = null;
    }
}
