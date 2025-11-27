using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleShipController : MonoBehaviour
{
    [Header("References")]
    public Transform mainSpars;
    public Transform foreSpars;
    public Rigidbody shipRigidbody;

    [Header("Hull Rotation")]
    public float hullTurnSpeed = 30f;
    public float hullDamping = 2f;
    public float lowSpeedTurnBoost = 1.5f;
    public float lowSpeedThreshold = 4f;

    [Header("Hull Acceleration")]
    public float hullAccelRate = 0.7f;      // Same pattern as spars
    public float hullDecelRate = 2f;
    public float hullAccelMax = 0.5f;
    private float hullAccelTimer = 0f;

    [Header("Spars Rotation")]
    public float sparsTurnSpeed = 50f;
    public float maxSparsAngle = 70f;
    private float sparsCurrentAngle = 0f;

    [Header("Spars Acceleration")]
    public float sparsAccelRate = 0.7f;
    public float sparsDecelRate = 2f;
    public float sparsAccelMax = 0.5f;
    private float sparsAccelTimer = 0f;

    private void Awake()
    {
        if (shipRigidbody == null)
            shipRigidbody = GetComponent<Rigidbody>();

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
        // allow left arrow as alternate
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        { turnInput = -1f; }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        { turnInput = 1f; }

        // Build or decay helm acceleration
        if (turnInput != 0f)
        {
            hullAccelTimer = Mathf.Clamp(
                hullAccelTimer + hullAccelRate * Time.fixedDeltaTime,
                0f,
                hullAccelMax
            );
        }
        else
        {
            hullAccelTimer = Mathf.Max(0f,
                hullAccelTimer - hullDecelRate * Time.fixedDeltaTime);
        }

        float helmMultiplier = 1f + hullAccelTimer;

        // Speed-based turn assist
        float shipSpeed = shipRigidbody.linearVelocity.magnitude;
        float t = Mathf.Clamp01(shipSpeed / lowSpeedThreshold);
        float turnBoost = Mathf.Lerp(lowSpeedTurnBoost, 1f, t);

        float desiredYaw = turnInput * hullTurnSpeed * helmMultiplier * turnBoost;
        Vector3 currentAngularVelocity = shipRigidbody.angularVelocity;

        float newYawVelocity = Mathf.Lerp(
            currentAngularVelocity.y,
            Mathf.Deg2Rad * desiredYaw,
            Time.fixedDeltaTime * hullDamping
        );

        shipRigidbody.angularVelocity =
            new Vector3(currentAngularVelocity.x, newYawVelocity, currentAngularVelocity.z);
    }

    private void HandleSparsRotation()
    {
        float sparsInput = 0f;
        if (Input.GetKey(KeyCode.Q)) sparsInput = -1f;
        if (Input.GetKey(KeyCode.E)) sparsInput = 1f;

        if (sparsInput != 0f)
        {
            sparsAccelTimer = Mathf.Clamp(
                sparsAccelTimer + sparsAccelRate * Time.fixedDeltaTime,
                0f,
                sparsAccelMax
            );

            float currentSpeed = sparsTurnSpeed * (1f + sparsAccelTimer);

            float deltaAngle = sparsInput * currentSpeed * Time.fixedDeltaTime;
            float newAngle = Mathf.Clamp(sparsCurrentAngle + deltaAngle, -maxSparsAngle, maxSparsAngle);

            deltaAngle = newAngle - sparsCurrentAngle;
            sparsCurrentAngle = newAngle;

            mainSpars.Rotate(0f, 0f, deltaAngle, Space.Self);
            foreSpars.Rotate(0f, 0f, deltaAngle, Space.Self);
        }
        else
        {
            sparsAccelTimer = Mathf.Max(0f, sparsAccelTimer - sparsDecelRate * Time.fixedDeltaTime);
        }
    }

    public float GetSparsAngle()
    {
        return sparsCurrentAngle;
    }

    public void SetSparsAngle(float angle)
    {
        float delta = angle - sparsCurrentAngle;
        sparsCurrentAngle = angle;

        mainSpars.Rotate(0f, 0f, delta, Space.Self);
        foreSpars.Rotate(0f, 0f, delta, Space.Self);
    }

}
