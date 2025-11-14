using UnityEngine;

public class WindPush : MonoBehaviour
{
    [Header("References")]
    public Transform mainSpars;
    public Rigidbody shipRigidbody;

    [Header("Wind Settings")]
    public float baseForce = 2f;
    public float maxBoostForce = 5f;
    public float sweetSpotBonus = 2f;
    public float sweetSpotThreshold = 5f; // degrees
    public float perfectAlignmentThreshold = 3f; // degrees for extra top speed
    public float maxSpeed = 10f;
    public float topSpeedBonusPercent = 0.1f; // 10% extra when perfect
    public float alignmentExponent = 2f;

    [Header("Friction / Slowdown")]
    public float horizontalDrag = 0.5f; // 0 = no drag, higher = faster slowdown

    void FixedUpdate()
    {
        if (!mainSpars || !shipRigidbody)
            return;

        // --- Flattened sail forward (global) ---
        Vector3 sparsForward = Vector3.ProjectOnPlane(mainSpars.forward, Vector3.up).normalized;

        // --- Angle to world forward (0° target) ---
        float angleToTarget = Vector3.SignedAngle(sparsForward, Vector3.forward, Vector3.up);
        float absAngle = Mathf.Abs(angleToTarget);

        // --- Alignment-based boost ---
        float t = Mathf.Clamp01(1f - absAngle / 180f);
        float alignmentBoost = Mathf.Pow(t, alignmentExponent) * maxBoostForce;

        // Sweet spot bonus
        if (absAngle < sweetSpotThreshold)
            alignmentBoost += sweetSpotBonus;

        float totalForce = baseForce + alignmentBoost;

        // --- Push along ship's local X axis (sailing direction) ---
        Vector3 pushDirection = transform.right;
        pushDirection.y = 0f;
        pushDirection.Normalize();

        // --- Determine max speed, extra bonus for perfect alignment ---
        float currentMaxSpeed = maxSpeed;
        if (absAngle <= perfectAlignmentThreshold)
            currentMaxSpeed *= 1f + topSpeedBonusPercent;

        // --- Horizontal velocity ---
        Vector3 horizontalVelocity = new Vector3(shipRigidbody.linearVelocity.x, 0f, shipRigidbody.linearVelocity.z);

        // --- Apply drag / slowdown ---
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, Vector3.zero, horizontalDrag * Time.fixedDeltaTime);

        // --- Apply force if under max speed ---
        if (horizontalVelocity.magnitude < currentMaxSpeed)
        {
            shipRigidbody.AddForce(pushDirection * totalForce, ForceMode.Acceleration);
        }
        else
        {
            // Optional: clamp horizontal speed to currentMaxSpeed
            horizontalVelocity = horizontalVelocity.normalized * currentMaxSpeed;
        }

        // --- Update Rigidbody velocity (keep Y as-is) ---
        shipRigidbody.linearVelocity = new Vector3(horizontalVelocity.x, shipRigidbody.linearVelocity.y, horizontalVelocity.z);
    }
}
