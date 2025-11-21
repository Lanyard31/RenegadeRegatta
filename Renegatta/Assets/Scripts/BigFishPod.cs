using UnityEngine;

public enum BigFishBehavior
{
    CrossingBow,
    FollowWake,
    RideBowWave
}

public class BigFishPod : MonoBehaviour
{
    [Header("Behavior")]
    public BigFishBehavior behavior;
    public int minCount = 2;
    public int maxCount = 5;

    [Header("Movement")]
    public float speed = 6f;
    public float turnRate = 2f;
    public float groupTightness = 3f;

    [Header("Refs")]
    public BigFishUnit fishUnitPrefab;

    private Transform playerShip;
    private SimpleWaveDeformer water;
    private BigFishUnit[] fish;

    private bool scatterMode;

    public void Init(Transform ship, SimpleWaveDeformer wave)
    {
        playerShip = ship;
        water = wave;

        int count = Random.Range(minCount, maxCount + 1);
        fish = new BigFishUnit[count];

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 4f;
            offset.y = 0;

            fish[i] = Instantiate(fishUnitPrefab, transform.position + offset, Quaternion.identity, transform);
            fish[i].Init(this, wave);
        }

        // randomly pick behavior if not assigned
        if (!System.Enum.IsDefined(typeof(BigFishBehavior), behavior))
            behavior = (BigFishBehavior)Random.Range(0, 3);
    }

    void Update()
    {
        if (!playerShip || scatterMode) return;

        switch (behavior)
        {
            case BigFishBehavior.CrossingBow:
                UpdateCrossingBow();
                break;
            case BigFishBehavior.FollowWake:
                UpdateFollowWake();
                break;
            case BigFishBehavior.RideBowWave:
                UpdateRideBowWave();
                break;
        }
    }

    public void Scatter()
    {
        if (scatterMode) return;
        scatterMode = true;

        foreach (var f in fish)
            f.Scatter();
    }

    void UpdateCrossingBow()
    {
        Vector3 bow = playerShip.position + playerShip.forward * 20f;
        Vector3 crossDir = (playerShip.right * 60f); // right or left depending on spawn

        Vector3 target = bow + crossDir;
        MovePod(target);

        // if we've passed the bow by enough distance, remove
        if (Vector3.Dot(transform.position - bow, playerShip.forward) > 50f)
            Destroy(gameObject);
    }

    void UpdateFollowWake()
    {
        Vector3 target = playerShip.position - playerShip.forward * 15f;
        MovePod(target);
    }

    void UpdateRideBowWave()
    {
        Vector3 target = playerShip.position + playerShip.forward * 10f;
        MovePod(target);
    }

    void MovePod(Vector3 target)
    {
        Vector3 dir = (target - transform.position).normalized;
        transform.forward = Vector3.Lerp(transform.forward, dir, Time.deltaTime * turnRate);
        transform.position += transform.forward * speed * Time.deltaTime;
    }
}
