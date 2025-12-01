using UnityEngine;

public class CannonballSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    [SerializeField] private Vector3 minAngularVelocity = new Vector3(-180f, -180f, -180f);
    [SerializeField] private Vector3 maxAngularVelocity = new Vector3(180f, 180f, 180f);

    private Vector3 angularVelocity;

    void OnEnable()
    {
        // pick a random angular velocity within the provided bounds
        angularVelocity = new Vector3(
            Random.Range(minAngularVelocity.x, maxAngularVelocity.x),
            Random.Range(minAngularVelocity.y, maxAngularVelocity.y),
            Random.Range(minAngularVelocity.z, maxAngularVelocity.z)
        );
    }

    void Update()
    {
        // spin in world space so it doesn't get weird if the parent rotates
        transform.Rotate(angularVelocity * Time.deltaTime, Space.World);
    }
}
