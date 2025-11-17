using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleShipController : MonoBehaviour
{
    [Header("References")]
    public Transform mainSpars;      // Child of hull
    public Transform foreSpars;
    public Rigidbody shipRigidbody;  // Rigidbody on hull

    [Header("Hull Rotation")]
    public float hullTurnSpeed = 30f;   // Degrees per second
    public float hullDamping = 2f;      // How quickly rotation slows when no input

    [Header("Spars Rotation")]
    public float sparsTurnSpeed = 50f;  // Degrees per second
    public float maxSparsAngle = 70f;  // Maximum local rotation in degrees
    private float sparsCurrentAngle = 0f;

    private void Awake()
    {
        if (shipRigidbody == null)
            shipRigidbody = GetComponent<Rigidbody>();

        // Freeze pitch and roll for simplicity
        shipRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        HandleHullRotation();
        HandleSparsRotation();
    }

    private void HandleHullRotation()
    {
        float turnInput = 0f;
        if (Input.GetKey(KeyCode.A)) turnInput = -1f;
        if (Input.GetKey(KeyCode.D)) turnInput = 1f;

        float desiredYaw = turnInput * hullTurnSpeed;
        Vector3 currentAngularVelocity = shipRigidbody.angularVelocity;
        float newYawVelocity = Mathf.Lerp(currentAngularVelocity.y, Mathf.Deg2Rad * desiredYaw, Time.fixedDeltaTime * hullDamping);

        shipRigidbody.angularVelocity = new Vector3(currentAngularVelocity.x, newYawVelocity, currentAngularVelocity.z);

    }

    private void HandleSparsRotation()
    {
        float sparsInput = 0f;
        if (Input.GetKey(KeyCode.Q)) sparsInput = -1f;
        if (Input.GetKey(KeyCode.E)) sparsInput = 1f;

        if (sparsInput != 0f)
        {
            float deltaAngle = sparsInput * sparsTurnSpeed * Time.fixedDeltaTime;
            float newAngle = Mathf.Clamp(sparsCurrentAngle + deltaAngle, -maxSparsAngle, maxSparsAngle);

            deltaAngle = newAngle - sparsCurrentAngle; // rotate only the delta
            sparsCurrentAngle = newAngle;

            // Rotate around local Z axis
            mainSpars.Rotate(0f, 0f, deltaAngle, Space.Self);
            foreSpars.Rotate(0f, 0f, deltaAngle, Space.Self);
        }
    }

}
