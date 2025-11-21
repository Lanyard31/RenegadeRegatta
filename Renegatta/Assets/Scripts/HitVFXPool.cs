using UnityEngine;
using System.Collections.Generic;

public class HitVFXPool : MonoBehaviour
{
    public HitVFX prefab;
    public int poolSize = 10;

    private List<HitVFX> pool = new List<HitVFX>();

    void Awake()
    {
        for (int i = 0; i < poolSize; i++)
        {
            HitVFX instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            //set parent to be the player
            instance.transform.parent = transform;
            pool.Add(instance);
        }
    }

    public HitVFX Get(Vector3 position)
    {
        foreach (var vfx in pool)
        {
            if (!vfx.gameObject.activeInHierarchy)
            {
                vfx.transform.position = position;
                vfx.transform.rotation = Quaternion.identity;
                vfx.gameObject.SetActive(true);
                return vfx;
            }
        }

        // Optional: expand pool if needed
        HitVFX newInstance = Instantiate(prefab, position, Quaternion.identity, transform);
        pool.Add(newInstance);
        return newInstance;
    }
}
