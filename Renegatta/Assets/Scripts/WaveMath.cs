using UnityEngine;

public static class WaveMath
{
    public static float GetWaveHeight(Vector3 worldPos, float time,
                                      float amplitude, float frequency, float speed)
    {
        float wave =
            Mathf.Sin((worldPos.x + time * speed) * frequency) * amplitude +
            Mathf.Cos((worldPos.z + time * speed) * frequency * 0.7f) * amplitude * 0.5f;

        return wave;
    }

    public static Vector3 GetWaveNormal(Vector3 worldPos, float time,
                                        float amplitude, float frequency, float speed)
    {
        // partial derivatives
        float dx = frequency * amplitude *
                   Mathf.Cos((worldPos.x + time * speed) * frequency);
        float dz = frequency * amplitude * 0.7f *
                   -Mathf.Sin((worldPos.z + time * speed) * frequency * 0.7f) * 0.5f;

        Vector3 normal = new Vector3(-dx, 1f, -dz);
        return normal.normalized;
    }
}
