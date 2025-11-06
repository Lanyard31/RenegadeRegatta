using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BoatWaterAlign : MonoBehaviour
{
    public LayerMask waterLayer;
    public float positionLerpSpeed = 2f;
    public float rotationLerpSpeed = 2f;
    public float raycastDistance = 10f;
    public float heightOffset = 0f;

    //wavemath randomizer
    public float amplitude = 0.05f;
    public float frequency = 1.82f;
    public float speed = 0.2f;

    private Transform _transform;

    void Awake()
    {
        _transform = transform;
        if (waterLayer == 0)
            waterLayer = LayerMask.GetMask("Water");
    }

    void Update()
    {
        Ray ray = new Ray(_transform.position + Vector3.up * 20f, Vector3.down);
        Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, waterLayer))
        {
            MeshCollider meshCollider = hit.collider as MeshCollider;
            if (meshCollider == null || meshCollider.sharedMesh == null)
                return;

            Mesh mesh = meshCollider.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            // Transform hit point to local space of mesh
            Transform waterTransform = meshCollider.transform;
            Vector3 localHitPoint = waterTransform.InverseTransformPoint(hit.point);

            // Find nearest vertex to hit point
            float closestDist = float.MaxValue;
            int closestIndex = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                float dist = (vertices[i].x - localHitPoint.x) * (vertices[i].x - localHitPoint.x) +
                             (vertices[i].z - localHitPoint.z) * (vertices[i].z - localHitPoint.z);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            // Convert back to world space
            Vector3 worldVertex = waterTransform.TransformPoint(vertices[closestIndex]);
            Vector3 worldNormal = waterTransform.TransformDirection(normals[closestIndex]);

            Vector3 targetPosition = new Vector3(
                _transform.position.x,
                worldVertex.y + heightOffset,
                _transform.position.z
            );

            _transform.position = Vector3.Lerp(
                _transform.position,
                targetPosition,
                Time.deltaTime * positionLerpSpeed
            );

            Quaternion targetRotation = Quaternion.FromToRotation(_transform.up, worldNormal) * _transform.rotation;
            _transform.rotation = Quaternion.Slerp(
                _transform.rotation,
                targetRotation,
                Time.deltaTime * rotationLerpSpeed
            );
        }
    }
}
