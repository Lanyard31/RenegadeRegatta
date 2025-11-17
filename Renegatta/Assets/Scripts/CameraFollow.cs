using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;         // The object to follow
    public float followSpeed = 5f;   // Smooth movement speed

    private Vector3 offset;
    private Vector3 desiredPosition;
    private float currentZoom = 1f;
    private float zoomSpeed = 1f;
    public float minZ = -2f;
    public float maxZ = 2f;

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

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0.001f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZ, maxZ);
        }
        else if (scroll < -0.001f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZ, maxZ);
        }
    }
    

    void LateUpdate()
    {
        // Keep camera offset relative to target
        desiredPosition = target.position + offset;
        desiredPosition = target.position + offset * currentZoom;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }
}
