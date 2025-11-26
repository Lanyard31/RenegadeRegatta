using UnityEngine;
using System.Collections.Generic;

public class BossPivot : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 2f;
    [SerializeField] private float driftAmplitude = 0.5f;
    [SerializeField] private float driftFrequency = 0.4f;

    // Tentacles register themselves here
    public List<TentaclePivotFollower> followers = new();

    private Transform player;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    void Update()
    {
        if (!player) return;

        // Face the player
        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.1f)
        {
            Quaternion target = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                target,
                rotateSpeed * Time.deltaTime
            );
        }

        // Optional drifting (makes the whole beast breathe / pulse)
        if (driftAmplitude > 0f)
        {
            float offset = Mathf.Sin(Time.time * driftFrequency) * driftAmplitude;
            transform.position += transform.right * offset * Time.deltaTime;
        }
    }
}
