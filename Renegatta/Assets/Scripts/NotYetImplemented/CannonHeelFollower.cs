using UnityEngine;

public class CannonHeelFollower : MonoBehaviour
{
    [SerializeField] private Transform shipRoot;        
    [SerializeField] private float maxHeel = 5f;        // how much the cannon rig tilts
    [SerializeField] private float smooth = 4f;         // responsiveness

    // ship roll is clamped to these extremes before remapping
    [SerializeField] private float shipRollClamp = 30f; 

    private float currentHeel = 0f;

    void LateUpdate()
    {
        if (shipRoot == null) return;

        // read signed Z rotation
        float roll = shipRoot.eulerAngles.z;
        if (roll > 180f) roll -= 360f;

        // clamp ship roll to sane range
        roll = Mathf.Clamp(roll, -shipRollClamp, shipRollClamp);

        // remap to 0..1
        float t = Mathf.InverseLerp(-shipRollClamp, shipRollClamp, roll);

        // remap to -maxHeel..maxHeel
        float targetHeel = Mathf.Lerp(-maxHeel, maxHeel, t);

        // smooth
        currentHeel = Mathf.Lerp(currentHeel, targetHeel, Time.deltaTime * smooth);

        // apply to this parent
        Vector3 e = transform.localEulerAngles;
        e.z = currentHeel;
        transform.localEulerAngles = e;
    }
}
