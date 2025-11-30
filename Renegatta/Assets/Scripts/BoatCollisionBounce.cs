using UnityEngine;

public class BoatCollisionBounce : MonoBehaviour
{
    [Header("Settings")]
    public float damageTaken = 15f;
    public float bounceForce = 8f;      // Lateral bounce strength
    public float upwardBoost = 2f;      // Small lift so we don't get trapped
    public float maxBounceSpeed = 10f;  // Clamp rebound velocity

    private float nudgeMultiplier = 1.75f;  // Scales lateral nudge when hit from below
    public HitVFXPool hitVFXPool;
    private PlayerHealth playerHealth;
    public WaterRushController waterRushController;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerHealth = GetComponent<PlayerHealth>();
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        Vector3 pushDir = contact.normal;

        // Check if hit is from below
        bool hitFromBelow = Vector3.Dot(pushDir, Vector3.up) > 0.8f;

        if (hitFromBelow)
        {
            // Upward lift
            pushDir += Vector3.up * (upwardBoost * 1.15f);

            // Determine if lateral push should help or fight current velocity
            Vector3 lateralDir = transform.right;
            float dot = Vector3.Dot(rb.linearVelocity, lateralDir);

            // Flip if it would push backward relative to motion
            if (dot < 0) lateralDir = -lateralDir;

            // Apply lateral push scaled by nudgeMultiplier
            pushDir += lateralDir * nudgeMultiplier;

            //Debug.Log($"Hit from below. Velocity: {rb.linearVelocity}, pushDir: {pushDir}");

        }
        else
        {
            pushDir += Vector3.up * upwardBoost;
        }

        pushDir.Normalize();

        rb.AddForce(pushDir * bounceForce, ForceMode.Impulse);

        if (rb.linearVelocity.magnitude > maxBounceSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxBounceSpeed;

        if (hitVFXPool != null)
        {
            //raise slightly on Y axis
            hitVFXPool.Get(contact.point + new Vector3(0, 0.15f, 0));
        }

        if (waterRushController != null)
        {
            waterRushController.OnGroundedBounce();
        }

        playerHealth.ApplyDamage(damageTaken);
    }


}
