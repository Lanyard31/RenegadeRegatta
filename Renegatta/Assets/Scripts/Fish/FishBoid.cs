using UnityEngine;

public class FishBoid : MonoBehaviour
{
    [Header("Movement")]
    public float swimSpeed = 2f;
    public float turnSpeed = 2f;
    public float wanderStrength = 0.3f; // small per-fish wander

    [Header("Avoidance")]
    public float avoidDistance = 1.5f;
    public float avoidStrength = 3f;

    Vector3 velocity;
    bool escaping = false;

    [Header("Parent School")]
    private SchoolOfFish parentSchool;
    private BoxCollider containment;

    // small per-fish offset for wandering around the group
    private Vector3 localOffset;

    public void SetParentSchool(SchoolOfFish s)
    {
        parentSchool = s;
        if (parentSchool != null)
            containment = parentSchool.GetComponent<BoxCollider>();

        // determine spread factor
        float spread = 1.5f; // default small school spread

        // scale by the fish mesh size if available
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            Vector3 size = rend.bounds.size;
            spread = spread + Mathf.Max(size.x, size.z) * 0.75f; // 75% of the largest horizontal dimension
        }

        // initialize a small random offset relative to the school center
        localOffset = Random.insideUnitSphere * spread;
        localOffset.y *= 0.3f; // flatten vertically
    }

    void Start()
    {
        velocity = transform.forward;
    }

    void Update()
    {
        if (escaping)
        {
            transform.position += transform.forward * swimSpeed * 3f * Time.deltaTime;
            return;
        }

        if (parentSchool != null)
        {
            // group target point
            Vector3 target = parentSchool.transform.position + localOffset;

            // small random wander added to target
            target += Random.insideUnitSphere * wanderStrength * 0.2f;

            // forward obstacle avoidance
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, avoidDistance))
                target += hit.normal * avoidStrength * 0.5f;

            // compute velocity toward target
            Vector3 desired = (target - transform.position).normalized;
            velocity = Vector3.Lerp(velocity, desired, Time.deltaTime * turnSpeed).normalized;

            transform.forward = velocity;
            transform.position += velocity * swimSpeed * Time.deltaTime;

            // containment in parent BoxCollider
            if (containment != null)
            {
                Vector3 localPos = containment.transform.InverseTransformPoint(transform.position);
                Vector3 halfSize = containment.size * 0.5f;

                localPos.x = Mathf.Clamp(localPos.x, -halfSize.x, halfSize.x);
                localPos.y = Mathf.Clamp(localPos.y, -halfSize.y, halfSize.y);
                localPos.z = Mathf.Clamp(localPos.z, -halfSize.z, halfSize.z);

                transform.position = containment.transform.TransformPoint(localPos);
            }
        }
        else
        {
            // fallback: basic random movement if no parent
            Vector3 steering = Random.insideUnitSphere * wanderStrength;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, avoidDistance))
                steering += hit.normal * avoidStrength;

            velocity = Vector3.Lerp(velocity, velocity + steering, Time.deltaTime * turnSpeed).normalized;
            transform.forward = velocity;
            transform.position += velocity * swimSpeed * Time.deltaTime;
        }
    }

    public void Scatter()
    {
        if (escaping) return;
        escaping = true;

        Vector3 dir = (transform.position - (parentSchool ? parentSchool.transform.position : Vector3.zero)).normalized;
        dir.y = -0.6f;
        if (dir.sqrMagnitude < 0.01f) dir = (Vector3.back + Vector3.down).normalized;
        transform.forward = dir.normalized;
    }
}
