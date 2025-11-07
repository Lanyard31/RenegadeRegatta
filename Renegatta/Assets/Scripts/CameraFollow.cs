using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;         // The object to follow
    public float followSpeed = 5f;   // Smooth movement speed

    private Vector3 offset;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraFollow: No target assigned.");
            enabled = false;
            return;
        }

        // Calculate initial offset relative to target
        offset = transform.position - target.position;
    }

    void LateUpdate()
    {
        // Keep camera offset relative to target
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}
