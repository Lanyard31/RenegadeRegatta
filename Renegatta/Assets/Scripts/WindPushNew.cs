using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WindPushNew : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform hull;
    [SerializeField] private Transform mainSpars;

    [Header("Wind Settings")]
    [SerializeField] private Vector3 windDirection = Vector3.right;
    [SerializeField] private float maxSpeedAligned = 12f;
    [SerializeField] private float accelerationForce = 20f;

    private Rigidbody rb;
    Vector3 pushDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }


    void FixedUpdate()
    {
        // Wind points along +X
        Vector3 windDir = Vector3.right;

        // Flatten sail forward to horizontal plane
        Vector3 sparsForward = Vector3.ProjectOnPlane(mainSpars.up, Vector3.up).normalized;

        // Compute angle between wind and sail in degrees
        float sparsWindAngle = Vector3.SignedAngle(windDir, sparsForward, Vector3.up);

        sparsWindAngle = sparsWindAngle - 90f;
        // Map into 0–360 range for same logic as Euler Y
        if (sparsWindAngle < 0f) sparsWindAngle += 360f;

        // Categorize using your exact thresholds
        string alignmentCategory;
        float efficiency = 0f;

        if (sparsWindAngle <= 30f || sparsWindAngle >= 330f)
        {
            alignmentCategory = "Perfect";
            efficiency = 1f;
        }
        else if (sparsWindAngle <= 60f || sparsWindAngle >= 300f)
        {
            alignmentCategory = "Broad Reach";
            efficiency = 0.75f;
        }
        else if (sparsWindAngle <= 90f || sparsWindAngle >= 270f)
        {
            alignmentCategory = "Beam Reach";
            efficiency = 0.5f;
        }
        else if (sparsWindAngle <= 120f || sparsWindAngle >= 240f)
        {
            alignmentCategory = "Close Reach";
            efficiency = 0.25f;
        }
        else
        {
            alignmentCategory = "Misaligned";
            efficiency = -0.5f; // Negative to slow down
        }

        // --- Push only if under max speed ---
        Vector3 forwardVel = Vector3.Dot(rb.linearVelocity, hull.right) * hull.right;
        float boatSpeed = forwardVel.magnitude;

        if (boatSpeed < maxSpeedAligned && boatSpeed > 0f)
        {
            pushDir = hull.right;
            rb.AddForce(pushDir * accelerationForce * efficiency, ForceMode.Acceleration);
        }

        Debug.Log($"Spars vs Wind Angle: {sparsWindAngle:F1}°, Category: {alignmentCategory}, BoatSpeed: {boatSpeed:F2}");
    }
}
