using UnityEngine;

public class AutoTrim : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimpleShipController ship;    // handle spars rotation
    [SerializeField] private WindPushNew wind;             // provides wind direction and Perfect logic

    [Header("AutoTrim Settings")]
    [SerializeField] private float autoTrimDelay = 5f;     // seconds of no player input
    [SerializeField] private float autoTrimSpeed = 20f;    // deg/sec rotation rate

    private float lastPlayerTrimTime = 0f;
    private bool isAutoTrimming = false;

    void Awake()
    {
        if (ship == null) ship = GetComponent<SimpleShipController>();
        if (wind == null) wind = GetComponent<WindPushNew>();
    }

    void Update()
    {
        bool playerInput = Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E);

        if (playerInput)
        {
            lastPlayerTrimTime = Time.time;
            isAutoTrimming = false;
        }
        else if (!isAutoTrimming && Time.time - lastPlayerTrimTime >= autoTrimDelay)
        {
            isAutoTrimming = true;
        }
    }
void FixedUpdate()
{
    if (!isAutoTrimming)
        return;

    // Current spars forward (projected horizontally)
    Vector3 sparsForward = Vector3.ProjectOnPlane(ship.mainSpars.up, Vector3.up).normalized;

    // Delta to Perfect (180° fix applied)
    float delta = Vector3.SignedAngle(wind.WindDirection, sparsForward, Vector3.up) - 90f + 180f;

    // Stop if basically there
    if (Mathf.Abs(delta) < 0.1f)
        return;

    // Determine rotation direction: positive delta = starboard (E), negative = port (Q)
    float step = Mathf.Clamp(Mathf.Sign(delta) * autoTrimSpeed * Time.fixedDeltaTime, -Mathf.Abs(delta), Mathf.Abs(delta));

    // Apply step, respecting maxSparsAngle
    float newAngle = Mathf.Clamp(ship.GetSparsAngle() + step, -ship.maxSparsAngle, ship.maxSparsAngle);

    ship.SetSparsAngle(newAngle);
}


}
