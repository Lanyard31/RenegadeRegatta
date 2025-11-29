using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider))]
public class SchoolOfFish : MonoBehaviour
{
    [Header("Fish Prefabs")]
    [SerializeField] List<GameObject> fishPrefabs;

    [Header("Spawn Settings")]
    [SerializeField] int minCount = 5;
    [SerializeField] int maxCount = 12;
    [SerializeField] float spawnRadius = 2f;
    [SerializeField] float spawnDepthOffset = 0.5f; // how far under surface the fish bottom should be

    [Header("Activation")]
    [SerializeField] Transform player;
    [SerializeField] float activationDistance = 200f;

    [Header("Parent Trigger (scare)")]
    [Tooltip("Dimensions of the parent's box trigger. X/Z roughly area, Y = depth band")]
    [SerializeField] Vector3 boxSize = new Vector3(10f, 6f, 10f);
    [SerializeField] Vector3 boxCenter = new Vector3(0f, -2f, 0f); // lower under surface

    [Header("Water")]
    [SerializeField] float waterLevelY = 0f;

    List<FishBoid> spawnedFish = new List<FishBoid>();
    bool active = false;
    bool scattered = false;

    BoxCollider parentTrigger;

    void Awake()
    {
        parentTrigger = GetComponent<BoxCollider>();
        parentTrigger.isTrigger = true;
        parentTrigger.size = boxSize;
        parentTrigger.center = boxCenter;
    }

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (scattered) return;

        if (player == null) return;

        float d2 = (player.position - transform.position).sqrMagnitude;
        if (!active && d2 < activationDistance * activationDistance)
            ActivateSchool();
        else if (active && d2 >= activationDistance * activationDistance)
            DeactivateSchool();
    }

    void ActivateSchool()
    {
        if (active) return;
        active = true;

        if (fishPrefabs == null || fishPrefabs.Count == 0) return;

        GameObject prefab = fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        int count = Random.Range(minCount, maxCount + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 localOffset = Random.insideUnitSphere * spawnRadius;
            // Force downward clustering so they don't spawn above surface
            localOffset.y = -Mathf.Abs(localOffset.y);

            Vector3 spawnPos = transform.position + localOffset;
            // make sure roughly below the water line
            float desiredBottomY = waterLevelY - spawnDepthOffset;

            GameObject fishGO = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            // Immediately orient the fish to a random horizontal heading, which compensates for weird prefab rotations
            Vector3 randDir = Random.onUnitSphere;
            randDir.y = 0f;
            if (randDir.sqrMagnitude < 0.001f) randDir = Vector3.forward;
            fishGO.transform.forward = randDir.normalized;

            // Adjust vertical position so mesh bottom sits at desiredBottomY (works even if model pivot is high or rotated)
            Renderer r = fishGO.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                float meshBottom = r.bounds.min.y;
                float delta = desiredBottomY - meshBottom;
                fishGO.transform.position += new Vector3(0f, delta, 0f);
            }
            else
            {
                // Fallback: clamp fish center under water a bit
                Vector3 p = fishGO.transform.position;
                p.y = Mathf.Min(p.y, waterLevelY - 0.5f);
                fishGO.transform.position = p;
            }

            FishBoid boid = fishGO.GetComponent<FishBoid>();
            if (boid != null)
            {
                boid.SetParentSchool(this);
                spawnedFish.Add(boid);
            }
            else
            {
                Debug.LogWarning("Spawned fish prefab missing FishBoid component.");
            }
        }
    }

    void DeactivateSchool()
    {
        active = false;
        foreach (var f in spawnedFish)
        {
            if (f != null) Destroy(f.gameObject);
        }
        spawnedFish.Clear();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!active || scattered) return;
        // Scare by tag or layer - adapt to your tags
        if (other.CompareTag("Player") || other.CompareTag("Projectile") || other.CompareTag("Rock"))
        {
            Scatter();
        }
    }

    public void Scatter()
    {
        if (scattered) return;
        scattered = true;

        foreach (var f in spawnedFish)
        {
            if (f != null) f.Scatter();
        }

        // Give them a little time to swim away before cleanup
        Destroy(gameObject, 4f);
    }

    // Debugging helper to visualize box in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Matrix4x4 trs = Matrix4x4.TRS(transform.position + transform.rotation * boxCenter, transform.rotation, Vector3.one);
        Gizmos.matrix = trs;
        Gizmos.DrawWireCube(Vector3.zero, boxSize);
    }
}
