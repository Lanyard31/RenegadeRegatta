using UnityEngine;

public class BoatCollisionBounce : MonoBehaviour
{
    [Header("Settings")]
    public float bounceForce = 8f;      // Lateral bounce strength
    public float upwardBoost = 2f;      // Small lift so we don't get trapped
    public float maxBounceSpeed = 10f;  // Clamp rebound velocity

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Find the average contact point/normal
        ContactPoint contact = collision.GetContact(0);

        // Normal points AWAY from the surface we hit
        Vector3 pushDir = contact.normal;

        // Add a little upward lift
        pushDir += Vector3.up * upwardBoost;
        pushDir.Normalize();

        // Apply bounce impulse
        rb.AddForce(pushDir * bounceForce, ForceMode.Impulse);

        // Clamp absurd rebound from stacked forces or old momentum
        if (rb.linearVelocity.magnitude > maxBounceSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxBounceSpeed;
        }
    }
}
