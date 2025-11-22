using UnityEngine;
using System.Collections.Generic;

public class BigFishManager : MonoBehaviour
{
    [Header("Settings")]
    public Transform playerShip;
    public float minInterval = 20f;
    public float maxInterval = 40f;
    private int minCount = 1;
    private int maxCount = 3;

    [Header("Prefabs")]
    public List<BigFishUnit> fishPrefabs = new List<BigFishUnit>();

    [Header("Requirements")]
    public PlayerHealth playerHealth;

    private float timer;
    private bool eventActive;
    private int unitsRemaining;

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        if (eventActive) return;

        if (playerHealth.GetHealthValue() < playerHealth.healthMax - 1f)
        {
            Debug.Log("Player health too low to spawn big fish");
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            TrySpawnEvent();
            ResetTimer();
        }
    }

    void ResetTimer()
    {
        timer = Random.Range(minInterval, maxInterval);
    }

    void TrySpawnEvent()
    {
        if (fishPrefabs.Count == 0) return;

        var prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        int count = Random.Range(minCount, maxCount + 1);

        Vector3 spawnPos =
            playerShip.position
            + playerShip.right * Random.Range(40f, 70f)
            + Vector3.down * 15f;

        eventActive = true;
        unitsRemaining = count;

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 16f;
            //rotated 90 degrees around y axis
            var unit = Instantiate(prefab, spawnPos + new Vector3(offset.x, 0f, offset.z), Quaternion.Euler(0f, 90f, 0f));
            unit.manager = this;
            unit.player = playerShip;
            //set this as parent.
            unit.transform.parent = transform;
        }
    }

    public void NotifyUnitFinished()
    {
        unitsRemaining--;

        if (unitsRemaining <= 0)
            eventActive = false;
    }
}
