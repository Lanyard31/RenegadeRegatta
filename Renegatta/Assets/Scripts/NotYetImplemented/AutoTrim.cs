using UnityEngine;

/// <summary>
/// AutoTrim: automatically adjusts spars after player input stops for a delay.
/// Mirrors WindPushNew's spars->wind alignment classification and SimpleShipController's
/// spars rotation mechanics. Does NOT change your thresholds or logic — it copies them.
/// </summary>
public class AutoTrim : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform of the main spars (the transform that SimpleShipController rotates on its local Z).")]
    [SerializeField] private Transform mainSpars;

    [Tooltip("Optional: transform of the fore spars that should rotate the same amount as mainSpars.")]
    [SerializeField] private Transform foreSpars;

    [Tooltip("Reference to the WindPushNew component that exposes windDir (world-space).")]
    [SerializeField] private MonoBehaviour windPushNewHolder; // use MonoBehaviour to avoid compile error if type differs
    // We'll reflectively fetch windDir from the referenced component so the script works if WindPushNew is not known at compile-time.
    
    [Header("AutoTrim Settings")]
    [Tooltip("Seconds of no Q/E input before AutoTrim begins.")]
    [SerializeField] private float autoTrimDelay = 5f;

    [Tooltip("Degrees per second that AutoTrim turns the spars (should be lower than player speed).")]
    [SerializeField] private float autoTrimTurnSpeed = 30f;

    [Tooltip("Maximum allowable spars angle in degrees (must match your SimpleShipController's maxSparsAngle).")]
    [SerializeField] private float maxSparsAngle = 45f;

    [Tooltip("Small simulation step in degrees used to probe reachable alignments.")]
    [SerializeField] private float simulationStep = 1f;

    [Tooltip("If true, print debug decisions to the console.")]
    [SerializeField] private bool debug = false;

    // internal
    private float timeSinceInput = 0f;
    private float currentLocalSparsAngle = 0f; // current angle (degrees) in range -180..180 (we clamp later to -max..max)
    private Quaternion baseLocalRotation; // rotation corresponding to spars angle = 0
    private object windPushObj = null;
    private System.Reflection.FieldInfo windDirField = null;

    private void Start()
    {
        if (mainSpars == null)
        {
            Debug.LogError("AutoTrim: mainSpars is required.");
            enabled = false;
            return;
        }

        // compute current local Z angle
        currentLocalSparsAngle = NormalizeAngle(mainSpars.localEulerAngles.z);

        // build baseLocalRotation such that baseLocalRotation * Euler(0,0,currentLocalSparsAngle) == mainSpars.localRotation
        baseLocalRotation = mainSpars.localRotation * Quaternion.Euler(0f, 0f, -currentLocalSparsAngle);

        // reflectively get a windDir Vector3 from the provided holder (so this script doesn't require compile-time WindPushNew)
        if (windPushNewHolder != null)
        {
            windPushObj = windPushNewHolder;
            var type = windPushObj.GetType();
            windDirField = type.GetField("windDir", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (windDirField == null)
            {
                Debug.LogWarning("AutoTrim: provided WindPushNew holder does not have a 'windDir' field. Assign a WindPushNew or expose windDir as public Vector3.");
            }
        }
        else
        {
            Debug.LogWarning("AutoTrim: windPushNewHolder is not assigned. AutoTrim will not function without wind direction.");
        }
    }

    private void FixedUpdate()
    {
        // detect player input (mirrors SimpleShipController)
        bool pressingQ = Input.GetKey(KeyCode.Q);
        bool pressingE = Input.GetKey(KeyCode.E);
        bool anyInput = pressingQ || pressingE;

        if (anyInput)
        {
            timeSinceInput = 0f;
            // keep local bookkeeping in sync with actual transform in case other script moved it
            currentLocalSparsAngle = NormalizeAngle(mainSpars.localEulerAngles.z);
            baseLocalRotation = mainSpars.localRotation * Quaternion.Euler(0f, 0f, -currentLocalSparsAngle);
            return; // player is controlling; don't auto-trim
        }

        timeSinceInput += Time.fixedDeltaTime;
        if (timeSinceInput < autoTrimDelay) return;

        Vector3 windDir;
        if (!TryGetWindDir(out windDir)) return;

        // Compute current alignment using the same code path as WindPushNew
        string currentCategory;
        float currentEfficiency;
        float currentSparsWindAngle = ComputeSparsWindAngleForAngle(currentLocalSparsAngle, windDir, out currentCategory, out currentEfficiency);

        if (debug) Debug.Log($"AutoTrim: currentCategory={currentCategory}, sparsWindAngle={currentSparsWindAngle:0.0}");

        // If Perfect or Broad Reach, do nothing (they're "good enough")
        if (currentCategory == "Perfect" || currentCategory == "Broad Reach")
        {
            if (debug) Debug.Log("AutoTrim: already Perfect or Broad Reach — no adjustment needed.");
            return;
        }

        // We will search for a reachable target angle by probing left and right.
        // Preference order: Perfect > Broad Reach > Beam Reach > Close Reach.
        string[] preferred = new string[] { "Perfect", "Broad Reach", "Beam Reach", "Close Reach" };

        // Find best reachable direction and immediate rotation sign (-1 left, +1 right)
        int chosenDirection = 0;
        float chosenTargetAngle = currentLocalSparsAngle;
        string chosenCategory = currentCategory;

        foreach (var targetCategory in preferred)
        {
            // for both directions, probe by stepping candidate angles (from small to max) to see if any yields the target category
            for (int dir = -1; dir <= 1; dir += 2)
            {
                float candidateAngle = currentLocalSparsAngle;
                bool found = false;
                // step outward from current angle
                int steps = Mathf.CeilToInt((maxSparsAngle * 2f) / simulationStep);
                for (int s = 1; s <= steps; s++)
                {
                    candidateAngle = currentLocalSparsAngle + dir * s * simulationStep;
                    // clamp to allowed [-maxSparsAngle, maxSparsAngle]
                    candidateAngle = Mathf.Clamp(candidateAngle, -maxSparsAngle, maxSparsAngle);

                    string cat;
                    float eff;
                    ComputeSparsWindAngleForAngle(candidateAngle, windDir, out cat, out eff);

                    if (cat == targetCategory)
                    {
                        chosenDirection = dir;
                        chosenTargetAngle = candidateAngle;
                        chosenCategory = cat;
                        found = true;
                        break;
                    }

                    // if we've hit the clamp limit, break early
                    if (Mathf.Abs(candidateAngle) >= maxSparsAngle - 0.0001f) break;
                }

                if (found) break;
            }

            if (chosenDirection != 0) break; // found reachable category
        }

        if (chosenDirection == 0)
        {
            // couldn't find any reachable better category — do nothing.
            if (debug) Debug.Log("AutoTrim: no reachable improvement from Misaligned — no rotation.");
            return;
        }

        if (debug) Debug.Log($"AutoTrim: chosenCategory={chosenCategory}, chosenDir={chosenDirection}, targetAngle={chosenTargetAngle:0.0}");

        // Apply a smooth rotation toward chosen direction at autoTrimTurnSpeed
        float deltaAngle = chosenDirection * autoTrimTurnSpeed * Time.fixedDeltaTime;
        float newAngle = Mathf.Clamp(currentLocalSparsAngle + deltaAngle, -maxSparsAngle, maxSparsAngle);

        // If applying delta would overshoot the chosenTargetAngle, clamp to targetAngle instead
        if (chosenDirection < 0)
        {
            if (newAngle < chosenTargetAngle) newAngle = chosenTargetAngle;
        }
        else
        {
            if (newAngle > chosenTargetAngle) newAngle = chosenTargetAngle;
        }

        float appliedDelta = newAngle - currentLocalSparsAngle;
        if (Mathf.Abs(appliedDelta) > 0.0001f)
        {
            // rotate the actual transforms like SimpleShipController does
            mainSpars.Rotate(0f, 0f, appliedDelta, Space.Self);
            if (foreSpars != null) foreSpars.Rotate(0f, 0f, appliedDelta, Space.Self);

            // update internal state
            currentLocalSparsAngle = newAngle;
            // update base rotation so subsequent simulation is consistent
            baseLocalRotation = mainSpars.localRotation * Quaternion.Euler(0f, 0f, -currentLocalSparsAngle);
        }
    }

    // Compute the spars-wind angle (0-360) and categorise it using the exact thresholds in your snippet.
    // Returns sparsWindAngle (0..360), and outputs category and efficiency.
    private float ComputeSparsWindAngleForAngle(float candidateLocalAngle, Vector3 windDir, out string category, out float efficiency)
    {
        // Reconstruct a local rotation for the spars that corresponds to candidateLocalAngle:
        Quaternion candidateLocalRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, candidateLocalAngle);

        // Spars forward flattened to horizontal plane: note WindPushNew used Vector3.ProjectOnPlane(mainSpars.up, Vector3.up).normalized;
        Vector3 candidateUp = candidateLocalRotation * Vector3.up;
        Vector3 sparsForward = Vector3.ProjectOnPlane(candidateUp, Vector3.up).normalized;

        // Compute angle between wind and sail in degrees
        float sparsWindAngle = Vector3.SignedAngle(windDir, sparsForward, Vector3.up);

        sparsWindAngle = sparsWindAngle - 90f;
        if (sparsWindAngle < 0f) sparsWindAngle += 360f;

        // Categorize using your exact thresholds
        if (sparsWindAngle <= 30f || sparsWindAngle >= 330f)
        {
            category = "Perfect";
            efficiency = 1f;
        }
        else if (sparsWindAngle <= 60f || sparsWindAngle >= 300f)
        {
            category = "Broad Reach";
            efficiency = 0.85f;
        }
        else if (sparsWindAngle <= 90f || sparsWindAngle >= 270f)
        {
            category = "Beam Reach";
            efficiency = 0.75f;
        }
        else if (sparsWindAngle <= 100f || sparsWindAngle >= 200f)
        {
            category = "Close Reach";
            efficiency = 0.65f;
        }
        else
        {
            category = "Misaligned";
            efficiency = 0.1f;
        }

        return sparsWindAngle;
    }

    private bool TryGetWindDir(out Vector3 windDir)
    {
        windDir = Vector3.forward;
        if (windPushObj == null || windDirField == null) return false;

        var val = windDirField.GetValue(windPushObj);
        if (val is Vector3 v)
        {
            windDir = v.normalized;
            return true;
        }

        return false;
    }

    // Normalize angle from 0..360 to -180..180
    private float NormalizeAngle(float a)
    {
        a = (a + 180f) % 360f;
        if (a < 0) a += 360f;
        return a - 180f;
    }
}
