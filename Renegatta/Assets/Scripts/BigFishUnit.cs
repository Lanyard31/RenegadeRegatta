using UnityEngine;

public class BigFishUnit : MonoBehaviour
{
    private BigFishPod pod;
    private SimpleWaveDeformer water;
    private bool escaping;

    public float followSpeed = 2f;
    public float jitter = 1f;
    public float depthOffset = -0.5f;

    public void Init(BigFishPod pod, SimpleWaveDeformer wave)
    {
        this.pod = pod;
        this.water = wave;
    }

    void Update()
    {
        if (escaping) return;

        Vector3 target = pod.transform.position;
        target += Random.insideUnitSphere * jitter;

        Vector3 dir = (target - transform.position).normalized;
        transform.forward = Vector3.Lerp(transform.forward, dir, Time.deltaTime * followSpeed);

        transform.position += transform.forward * pod.speed * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (escaping || !water) return;

        // wave motion same as your SchoolWaveFollower
        Vector3 worldPos = transform.position;
        Vector3 localPos = water.transform.InverseTransformPoint(worldPos);

        float localHeight = GetWaveHeightLocal(localPos.x, localPos.z);

        Vector3 waveWorld = water.transform.TransformPoint(
            new Vector3(localPos.x, localHeight, localPos.z)
        );

        float targetY = waveWorld.y + depthOffset;
        worldPos.y = Mathf.Lerp(worldPos.y, targetY, Time.deltaTime * 4f);
        transform.position = worldPos;
    }

    public void Scatter()
    {
        if (escaping) return;
        escaping = true;

        Vector3 dir = (transform.position - pod.transform.position).normalized;
        dir.y = -0.6f;
        if (dir.sqrMagnitude < 0.01f)
            dir = (Vector3.back + Vector3.down).normalized;

        transform.forward = dir;
    }

    float GetWaveHeightLocal(float x, float z)
    {
        float t = Time.time;
        Vector2 d1 = water.direction1.normalized;
        Vector2 d2 = water.direction2.normalized;

        float k1 = 2 * Mathf.PI / water.wavelength1;
        float k2 = 2 * Mathf.PI / water.wavelength2;

        float p1 = (x * d1.x + z * d1.y) * k1 + t * water.speed1;
        float p2 = (x * d2.x + z * d2.y) * k2 + t * water.speed2;

        return Mathf.Sin(p1) * water.amplitude1 + Mathf.Sin(p2) * water.amplitude2;
    }
}
