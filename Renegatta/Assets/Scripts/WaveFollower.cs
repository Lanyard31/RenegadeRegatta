using UnityEngine;

public class WaveFollower : MonoBehaviour
{
    public SimpleWaveDeformer water;

    [Header("Offsets")]
    public float depthOffset = -1f;   // how far below water surface this root should be

    [Header("Smoothing")]
    public float followSpeed = 4f;    // snappiness of wave following

    void Start()
    {
        if (!water)
        {
            var waterObj = GameObject.FindWithTag("Water");
            if (waterObj)
                water = waterObj.GetComponent<SimpleWaveDeformer>();
        }
    }

    void LateUpdate()
    {
        if (!water) return;

        Vector3 worldPos = transform.position;

        // convert to water-local for sampling
        Vector3 localPos = water.transform.InverseTransformPoint(worldPos);

        float localHeight = SampleWaveHeight(localPos.x, localPos.z);

        float waveY = water.transform.TransformPoint(
            new Vector3(localPos.x, localHeight, localPos.z)
        ).y;

        float targetY = waveY + depthOffset;

        // only adjust Y
        worldPos.y = Mathf.Lerp(worldPos.y, targetY, followSpeed * Time.deltaTime);

        transform.position = worldPos;
    }


    float SampleWaveHeight(float x, float z)
    {
        float t = Time.time;

        Vector2 d1 = water.direction1.normalized;
        Vector2 d2 = water.direction2.normalized;

        float k1 = (Mathf.PI * 2f) / water.wavelength1;
        float k2 = (Mathf.PI * 2f) / water.wavelength2;

        float p1 = (x * d1.x + z * d1.y) * k1 + t * water.speed1;
        float p2 = (x * d2.x + z * d2.y) * k2 + t * water.speed2;

        return Mathf.Sin(p1) * water.amplitude1 + Mathf.Sin(p2) * water.amplitude2;
    }
}
