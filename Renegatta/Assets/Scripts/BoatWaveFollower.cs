using UnityEngine;

public class BoatWaveFollower : MonoBehaviour
{
    public SimpleWaveDeformer water;

    [Header("Offsets and Smoothing")]
    public float heightOffset = 1f;
    public float heightMultiplier = 1f;
    public float positionLerpSpeed = 5f;
    public float rotationLerpSpeed = 2f;
    [Range(0f, 1f)] public float normalInfluence = 0.3f;

    void LateUpdate()
    {
        if (!water) return;

        Vector3 pos = transform.position;

        // Convert world position to the local space of the water mesh
        Vector3 localPos = water.transform.InverseTransformPoint(pos);

        // Calculate wave height & normal in local space
        float localHeight = GetWaveHeightLocal(localPos.x, localPos.z) * heightMultiplier;
        Vector3 localNormal = GetWaveNormalLocal(localPos.x, localPos.z);

        // Convert the local wave height back to world space
        Vector3 worldWavePoint = water.transform.TransformPoint(new Vector3(localPos.x, localHeight, localPos.z));

        // Move toward the wave height
        Vector3 targetPos = new Vector3(pos.x, worldWavePoint.y + heightOffset, pos.z);
        transform.position = Vector3.Lerp(pos, targetPos, Time.deltaTime * positionLerpSpeed);

        // Blend the wave normal with upright for stability
        Vector3 worldNormal = water.transform.TransformDirection(localNormal).normalized;
        Vector3 blendedUp = Vector3.Lerp(Vector3.up, worldNormal, normalInfluence).normalized;
        Quaternion targetRot = Quaternion.FromToRotation(transform.up, blendedUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
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

    Vector3 GetWaveNormalLocal(float x, float z)
    {
        float t = Time.time;
        Vector2 d1 = water.direction1.normalized;
        Vector2 d2 = water.direction2.normalized;
        float k1 = 2 * Mathf.PI / water.wavelength1;
        float k2 = 2 * Mathf.PI / water.wavelength2;

        float dx =
            Mathf.Cos((x * d1.x + z * d1.y) * k1 + t * water.speed1) * water.amplitude1 * k1 * d1.x +
            Mathf.Cos((x * d2.x + z * d2.y) * k2 + t * water.speed2) * water.amplitude2 * k2 * d2.x;

        float dz =
            Mathf.Cos((x * d1.x + z * d1.y) * k1 + t * water.speed1) * water.amplitude1 * k1 * d1.y +
            Mathf.Cos((x * d2.x + z * d2.y) * k2 + t * water.speed2) * water.amplitude2 * k2 * d2.y;

        Vector3 n = new Vector3(-dx, 1f, -dz).normalized;
        return n;
    }

}
