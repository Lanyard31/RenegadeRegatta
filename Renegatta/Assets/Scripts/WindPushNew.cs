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

    [Header("Heel")]
    [SerializeField] private Transform hullVisual;
    [SerializeField] public float maxHeelAngle = 8f;     // visual-only tilt, in degrees
    [SerializeField] private float heelSmoothing = 3f;    // higher = snappier
    private float currentHeel;

    [Header("Sail Fill")]
    [SerializeField] private Transform MainBall;
    [SerializeField] private Transform ForeBall;
    [SerializeField] private float emptyOffset = 0.5f; // adjust to taste
    private Vector3 mainBallFullPos;
    private Vector3 foreBallFullPos;
    private Vector3 mainBallEmptyPos;
    private Vector3 foreBallEmptyPos;
    public AudioSource windFlapAudioSource;
    float windFlapSFXvolumeOriginal;

    private Rigidbody rb;
    Vector3 pushDir;
    Vector3 windDir;
    private PlayerHealth health;
    private float healthPenalty = 0f;
    private string previousAlignmentCategory = "";


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        windDir = Vector3.right;
        health = GetComponent<PlayerHealth>();
        windFlapSFXvolumeOriginal = windFlapAudioSource.volume;
    }

    void Start()
    {
        mainBallFullPos = MainBall.localPosition + new Vector3(emptyOffset, 0, 0);
        foreBallFullPos = ForeBall.localPosition + new Vector3(emptyOffset, 0, 0);

        mainBallEmptyPos = MainBall.localPosition - new Vector3(emptyOffset, 0, 0);
        foreBallEmptyPos = ForeBall.localPosition - new Vector3(emptyOffset, 0, 0);

        health.OnHealthPenaltyChanged += HandleHealthPenaltyChanged;
    }

    private void HandleHealthPenaltyChanged(float penalty)
    {
        healthPenalty = penalty;  // cache it
    }

    void FixedUpdate()
    {
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
            efficiency = 0.8f;
        }
        else if (sparsWindAngle <= 90f || sparsWindAngle >= 270f)
        {
            alignmentCategory = "Beam Reach";
            efficiency = 0.65f;
        }
        else if (sparsWindAngle <= 100f || sparsWindAngle >= 200f)
        {
            alignmentCategory = "Close Reach";
            efficiency = 0.55f;
        }
        else
        {
            alignmentCategory = "Misaligned";
            efficiency = 0.1f; // Negative to slow down
        }

        efficiency = efficiency * (1f - (healthPenalty * 0.5f));

        ApplyWindHeel(sparsWindAngle, efficiency, alignmentCategory);
        AdjustWindBalls(efficiency, alignmentCategory);

        // --- Push only if under max speed ---
        Vector3 forwardVel = Vector3.Dot(rb.linearVelocity, hull.right) * hull.right;
        float boatSpeed = forwardVel.magnitude;

        if (boatSpeed < (maxSpeedAligned * efficiency))
        {
            pushDir = hull.right;
            rb.AddForce(pushDir * accelerationForce * efficiency, ForceMode.Acceleration);
        }

        // --- WindFlap SFX: Beam Reach -> Broad Reach ---
        if (previousAlignmentCategory == "Beam Reach" && alignmentCategory == "Broad Reach")
        {
            if (!windFlapAudioSource.isPlaying)
            {
                windFlapAudioSource.volume = Random.Range(0.8f, 1.2f) * windFlapSFXvolumeOriginal;
                windFlapAudioSource.pitch = Random.Range(0.8f, 1.2f);
                windFlapAudioSource.Play();
            }
        }

        // Update previous category for next frame
        previousAlignmentCategory = alignmentCategory;


        //Debug.Log($"Spars vs Wind Angle: {sparsWindAngle:F1}°, Category: {alignmentCategory}, BoatSpeed: {boatSpeed:F2}");
    }

    void ApplyWindHeel(float sparsWindAngle, float efficiency, string alignmentCategory)
    {
        if (alignmentCategory == "Misaligned") return;

        // Compute how much the wind hits the side of the hull
        // Wind from side: 0 = wind ahead/behind, 1 = wind perfectly from side
        Vector3 windRight = Vector3.Cross(Vector3.up, windDir).normalized; // right-facing vector relative to wind
        float windFromSide = Mathf.Abs(Vector3.Dot(hull.right, windRight));
        float sideSign = Mathf.Sign(Vector3.Dot(hull.right, windRight)); // +1 = starboard, -1 = port

        // Scale by sail efficiency
        float heelStrength = windFromSide * efficiency;
        float targetHeel = maxHeelAngle * heelStrength * sideSign;

        // Smooth the heel so it doesn't snap
        currentHeel = Mathf.Lerp(currentHeel, targetHeel, Time.fixedDeltaTime * heelSmoothing);

        Quaternion heelRot = Quaternion.Euler(-90f + currentHeel, 0f, 0f);
        hullVisual.localRotation = heelRot;

        //Debug.Log($"Heel: {currentHeel:F1}°, windFromSide: {windFromSide:F2}, sideSign: {sideSign}");
    }

    void AdjustWindBalls(float efficiency, string alignmentCategory)
    {
        if (alignmentCategory == "Misaligned")
        {
            efficiency = 0f;
        }
        else
        {
            efficiency = Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(efficiency));
        }

        // Calculate the efficiency-based target positions
        Vector3 mainTargetPos = Vector3.Lerp(mainBallEmptyPos, mainBallFullPos, efficiency);
        Vector3 foreTargetPos = Vector3.Lerp(foreBallEmptyPos, foreBallFullPos, efficiency);

        // Smoothly move toward the targets
        MainBall.localPosition = Vector3.Lerp(MainBall.localPosition, mainTargetPos, Time.fixedDeltaTime);
        ForeBall.localPosition = Vector3.Lerp(ForeBall.localPosition, foreTargetPos, Time.fixedDeltaTime);
    }

    public void AddCoinBoost(float amount)
    {
        rb.AddForce(hull.right * amount, ForceMode.VelocityChange);
    }

    public float CurrentHeel => currentHeel;

}
