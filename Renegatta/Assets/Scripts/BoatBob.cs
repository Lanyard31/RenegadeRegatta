using UnityEngine;

public class BoatBob : MonoBehaviour
{
    public float amplitude = 0.2f;
    public float frequency = 1f;
    public float speed = 1f;

    public Transform shipModel;

    private float startHeight;

    void Start()
    {
        startHeight = shipModel.position.y;
    }

    void Update()
    {
        float t = Time.time;
        Vector3 pos = shipModel.position;

        // Sample wave height
        float height = WaveMath.GetWaveHeight(pos, t, amplitude, frequency, speed);
        pos.y = height + startHeight;

        // Sample normal for rotation
        Vector3 normal = WaveMath.GetWaveNormal(pos, t, amplitude, frequency, speed);
        shipModel.position = pos;

        // Orient to normal
        shipModel.up = Vector3.Lerp(shipModel.up, normal, Time.deltaTime * 2f);
    }
}
