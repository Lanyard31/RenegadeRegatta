using UnityEngine;
using System.Collections;

public class BigFishManager : MonoBehaviour
{
    [Header("Refs")]
    public Transform playerShip;
    public SimpleWaveDeformer water;

    [Header("Prefabs")]
    public BigFishPod podPrefab;

    [Header("Spawn Settings")]
    public float spawnDistance = 80f; 
    public float despawnDistance = 120f;
    public float eventChance = 0.25f; // maybe?

    private BigFishPod activePod;
    private float checkTimer;

    void Update()
    {
        if (!playerShip) return;

        // one pod at a time
        if (activePod)
        {
            // despawn if it drifted way out of view
            float dist = Vector3.Distance(activePod.transform.position, playerShip.position);
            if (dist > despawnDistance)
            {
                Destroy(activePod.gameObject);
                activePod = null;
            }
            return;
        }

        checkTimer += Time.deltaTime;
        if (checkTimer < 5f) return;
        checkTimer = 0f;

        if (Random.value > eventChance) return;

        SpawnPod();
    }

    void SpawnPod()
    {
        Vector3 dir = Random.insideUnitCircle.normalized;
        Vector3 spawnPos = playerShip.position + new Vector3(dir.x, 0, dir.y) * spawnDistance;

        activePod = Instantiate(podPrefab, spawnPos, Quaternion.identity);
        activePod.Init(playerShip, water);
    }
}
