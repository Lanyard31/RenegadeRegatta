using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private Material hitMaterial;
    [SerializeField] private float flashDuration = 0.05f;

    private Renderer[] renderers;
    private Material[][] originalMaterials;

    void Awake()
    {
        // Grab all renderers (MeshRenderer + SkinnedMeshRenderer)
        renderers = GetComponentsInChildren<Renderer>(true);

        // Store original materials
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
    }

    /// <summary>
    /// Called by PlayerHealth via reflection (Flash() or DoFlash()).
    /// </summary>
    public void Flash()
    {
        StartCoroutine(FlashRoutine());
    }

    /// <summary>
    /// Enemy and other scripts can call this directly too.
    /// </summary>
    public void EnemyHitFlash()
    {
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // Build the temp arrays once
        Material[] tempMaterials = null;

        // Swap to hit materials
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            int count = originalMaterials[i].Length;

            // Create a fresh array per renderer
            tempMaterials = new Material[count];
            for (int m = 0; m < count; m++)
            {
                tempMaterials[m] = hitMaterial;
            }

            r.materials = tempMaterials;
        }

        yield return new WaitForSeconds(flashDuration);

        // Restore original materials
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].materials = originalMaterials[i];
        }
    }
}
