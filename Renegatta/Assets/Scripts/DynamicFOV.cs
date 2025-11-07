using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DynamicFOV : MonoBehaviour
{
    [Header("References")]
    public Rigidbody shipRigidbody;   // Rigidbody of the ship

    [Header("FOV Settings")]
    public float baseFOV = 60f;       // Default FOV at low speed
    public float maxFOV = 75f;        // Maximum FOV at high speed
    public float maxSpeed = 20f;      // Speed at which FOV reaches max
    public float smoothSpeed = 2f;    // How fast FOV interpolates

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (shipRigidbody == null)
            Debug.LogWarning("DynamicFOV: Ship Rigidbody not assigned!");
    }

    private void LateUpdate()
    {
        if (shipRigidbody == null) return;

        // Get ship forward speed
        float speed = shipRigidbody.linearVelocity.magnitude;

        // Map speed to FOV
        float targetFOV = Mathf.Lerp(baseFOV, maxFOV, Mathf.Clamp01(speed / maxSpeed));

        // Smoothly interpolate camera FOV
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * smoothSpeed);
    }
}
