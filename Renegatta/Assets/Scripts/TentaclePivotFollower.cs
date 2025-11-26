using UnityEngine;

[RequireComponent(typeof(Tentacle))]
public class TentaclePivotFollower : MonoBehaviour
{
    public SkullBoss boss;

    [Header("SmoothDamp Settings")]
    [SerializeField] private float smoothTimeMin = 0.15f;
    [SerializeField] private float smoothTimeMax = 0.25f;

    [Header("Organic Motion")]
    [SerializeField] private float maxRandomOffset = 0.3f; // max local offset in X/Z
    [SerializeField] private float phaseLagMultiplier = 0.05f; // for per-tentacle lag

    private Vector3 baseOffset;
    private Vector3 velocity;
    private float smoothTime;
    private float phaseOffset;

    void Start()
    {
        if (!boss) return;

        // Store local offset in boss Y-axis space
        baseOffset = Quaternion.Inverse(Quaternion.Euler(0, boss.transform.eulerAngles.y, 0))
                     * (transform.position - boss.transform.position);

        // Randomized smoothTime per tentacle for individuality
        smoothTime = Random.Range(smoothTimeMin, smoothTimeMax);

        // Random phase offset for small lag
        phaseOffset = Random.Range(-Mathf.PI, Mathf.PI);
    }

    void LateUpdate()
    {
        if (!boss) return;

        // Apply boss yaw rotation to the base offset
        Quaternion bossYaw = Quaternion.Euler(0, boss.transform.eulerAngles.y, 0);
        Vector3 rotatedOffset = bossYaw * baseOffset;

        // Apply small organic variation
        float t = Time.time + phaseOffset;
        Vector3 randomOffset = new Vector3(
            Mathf.Sin(t * 1.3f) * maxRandomOffset,
            0f,
            Mathf.Cos(t * 1.7f) * maxRandomOffset
        );

        // Target X/Z position
        Vector3 targetPos = boss.transform.position + rotatedOffset + randomOffset;

        // SmoothDamp to the target
        Vector3 newPos = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);

        // Preserve Y from Tentacle script
        newPos.y = transform.position.y;
        transform.position = newPos;
    }
}
