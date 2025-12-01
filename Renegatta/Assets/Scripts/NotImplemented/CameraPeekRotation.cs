using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraPeekFollow : MonoBehaviour
{
    [Header("References")]
    public Transform target;             // Player's hull
    public float followSpeed = 5f;       // Position Lerp speed
    public float rotationSpeed = 3f;     // Rotation Lerp speed
    public float maxPeekAngle = 20f;     // Max left/right rotation
    public Transform[] ignoreChildren;   // Children to not rotate

    [Header("Zoom")]
    public float currentZoom = 1f;
    public float zoomSpeed = 1f;
    public float minZ = -2f;
    public float maxZ = 2f;

    private Vector3 offset;
    private Quaternion[] childOriginalRotations;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("CameraPeekFollow: No target assigned.");
            enabled = false;
            return;
        }

        offset = transform.position - target.position;

        // Cache original child rotations
        if (ignoreChildren != null && ignoreChildren.Length > 0)
        {
            childOriginalRotations = new Quaternion[ignoreChildren.Length];
            for (int i = 0; i < ignoreChildren.Length; i++)
                childOriginalRotations[i] = ignoreChildren[i].localRotation;
        }
    }

    void Update()
    {
        HandleZoom();
    }

    void LateUpdate()
    {
        FollowTarget();
        PeekBasedOnHeading();
        RestoreChildRotations();
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
            currentZoom -= scroll * zoomSpeed;
        
        if (Input.GetKey(KeyCode.UpArrow))
            currentZoom -= zoomSpeed * 0.5f * Time.deltaTime;
        if (Input.GetKey(KeyCode.DownArrow))
            currentZoom += zoomSpeed * 0.5f * Time.deltaTime;

        currentZoom = Mathf.Clamp(currentZoom, minZ, maxZ);
    }

    private void FollowTarget()
    {
        Vector3 desiredPos = target.position + offset * currentZoom;
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
    }

    private void PeekBasedOnHeading()
    {
        // Get player's local movement relative to forward
        Vector3 playerForward = target.forward;
        Vector3 playerVelocity = (target.position - transform.position).normalized; // Or a rigidbody velocity
        float sideDot = Vector3.Dot(target.right, playerVelocity); // Positive if moving right, negative if left

        float targetYRotation = sideDot * maxPeekAngle;

        // Smoothly rotate around target's Y axis
        Quaternion targetRotation = Quaternion.Euler(0f, targetYRotation, 0f) * Quaternion.LookRotation(target.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void RestoreChildRotations()
    {
        if (ignoreChildren == null || ignoreChildren.Length == 0) return;
        for (int i = 0; i < ignoreChildren.Length; i++)
            ignoreChildren[i].localRotation = childOriginalRotations[i];
    }
}
