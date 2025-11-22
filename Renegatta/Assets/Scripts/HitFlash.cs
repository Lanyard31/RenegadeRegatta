using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [Header("Flash Materials")]
    [SerializeField] private Material flashMaterialA;  // e.g. red
    [SerializeField] private Material flashMaterialB;  // e.g. white

    [Header("Settings")]
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private int flashCount = 3;

    [Header("Ignore These")]
    [SerializeField] private List<GameObject> skipObjects = new();

    private Renderer[] renderers;
    private Material[][] originalMaterials;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);

        originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
    }

    public void Flash()
    {
        StartCoroutine(FlashRoutine());
    }

    public void EnemyHitFlash()
    {
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Pre-build temp arrays for both flash materials
        Material[][] flashA = new Material[renderers.Length][];
        Material[][] flashB = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            // Renderer destroyed or missing? Just mirror original.
            if (r == null || r.gameObject == null)
            {
                flashA[i] = originalMaterials[i];
                flashB[i] = originalMaterials[i];
                continue;
            }

            // Skip if this renderer is on or inside a skipped object
            if (IsSkipped(r.gameObject))
            {
                flashA[i] = originalMaterials[i];
                flashB[i] = originalMaterials[i];
                continue;
            }

            int count = originalMaterials[i].Length;

            flashA[i] = new Material[count];
            flashB[i] = new Material[count];

            for (int m = 0; m < count; m++)
            {
                flashA[i][m] = flashMaterialA;
                flashB[i][m] = flashMaterialB;
            }
        }

        for (int f = 0; f < flashCount; f++)
        {
            ApplyMaterials(flashA);
            yield return new WaitForSeconds(flashDuration);

            ApplyMaterials(flashB);
            yield return new WaitForSeconds(flashDuration);
        }

        ApplyMaterials(originalMaterials);
    }

    private void ApplyMaterials(Material[][] mats)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];

            if (r == null || r.gameObject == null)
                continue;

            r.materials = mats[i];
        }
    }


    private bool IsSkipped(GameObject obj)
    {
        foreach (GameObject skipped in skipObjects)
        {
            if (skipped == null) continue;
            if (obj == skipped || obj.transform.IsChildOf(skipped.transform))
                return true;
        }
        return false;
    }
}
