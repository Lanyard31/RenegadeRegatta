using UnityEngine;

public class SchoolWaveFollower : MonoBehaviour
{
    public SimpleWaveDeformer water;

    [Header("Offsets")]
    public float depthOffset = -1f; // how far below water surface the school root should be

    [Header("Smoothing")]
    public float followSpeed = 4f;   // how snappy the bobbing is

    //get the water by its tag
    void Start()
    {
        if (!water)
        {
            water = GameObject.FindWithTag("Water").GetComponent<SimpleWaveDeformer>();
        }
    }

    void LateUpdate()
    {
        if (!water) return;

        Vector3 worldPos = transform.position;

        // Convert world position to the water's local coordinates
        Vector3 localPos = water.transform.InverseTransformPoint(worldPos);

        // Sample wave height at local (x, z)
        float localHeight = GetWaveHeightLocal(localPos.x, localPos.z);

        // Convert the sampled height back into world space
        Vector3 waveWorld = water.transform.TransformPoint(
            new Vector3(localPos.x, localHeight, localPos.z)
        );

        // Target depth under surface
        float targetY = waveWorld.y + depthOffset;

        // Smooth movement
        worldPos.y = Mathf.Lerp(worldPos.y, targetY, Time.deltaTime * followSpeed);
        transform.position = worldPos;
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
