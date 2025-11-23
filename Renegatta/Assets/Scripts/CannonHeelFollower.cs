using UnityEngine;

public class CannonHeelFollower : MonoBehaviour
{
    [SerializeField] private WindPushNew windPush; 
    [SerializeField] private float maxCannonHeel = 10f;
    [SerializeField] private float smoothing = 5f;

    private float currentCannonHeel;

    void FixedUpdate()
    {
        if (windPush == null) return;

        // Grab raw heel
        float boatHeel = windPush.CurrentHeel;

        // Scale it down from the boat's maxHeelAngle
        float scaled = Mathf.InverseLerp(-windPush.maxHeelAngle, windPush.maxHeelAngle, boatHeel);
        float target = Mathf.Lerp(-90f - maxCannonHeel, -90f + maxCannonHeel, scaled);

        // Smooth
        currentCannonHeel = Mathf.Lerp(currentCannonHeel, target, Time.fixedDeltaTime * smoothing);

        // Apply rotation
        transform.localRotation = Quaternion.Euler(currentCannonHeel, 0f, 0f);
    }
}
