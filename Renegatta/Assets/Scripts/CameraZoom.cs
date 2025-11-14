using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [Header("Settings")]
    public float zoomSpeed = 5f;
    public float minZ = -5f;
    public float maxZ = -1f;

    private Vector3 targetLocalPos;

    void Start()
    {
        targetLocalPos = transform.localPosition;
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetLocalPos.z += scroll * zoomSpeed;
            targetLocalPos.z = Mathf.Clamp(targetLocalPos.z, minZ, maxZ);
            transform.localPosition = targetLocalPos;
        }
    }
}
