using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SimpleWaveDeformer : MonoBehaviour
{
    [Header("Primary Wave")]
    public float amplitude1 = 0.5f;
    public float wavelength1 = 5.0f;
    public float speed1 = 1.0f;
    public Vector2 direction1 = new Vector2(1f, 0f);

    [Header("Secondary Wave (optional)")]
    public float amplitude2 = 0.2f;
    public float wavelength2 = 2.5f;
    public float speed2 = 2.0f;
    public Vector2 direction2 = new Vector2(0.8f, 0.2f);

    private Mesh deformingMesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;

    void Start()
    {
        deformingMesh = GetComponent<MeshFilter>().mesh;
        baseVertices = deformingMesh.vertices;
        displacedVertices = new Vector3[baseVertices.Length];
    }

    void Update()
    {
        float t = Time.time;

        Vector2 dir1 = direction1.normalized;
        Vector2 dir2 = direction2.normalized;
        float k1 = 2 * Mathf.PI / wavelength1;
        float k2 = 2 * Mathf.PI / wavelength2;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 v = baseVertices[i];

            float phase1 = (v.x * dir1.x + v.z * dir1.y) * k1 + t * speed1;
            float phase2 = (v.x * dir2.x + v.z * dir2.y) * k2 + t * speed2;

            float height = Mathf.Sin(phase1) * amplitude1
                         + Mathf.Sin(phase2) * amplitude2;

            v.y = height;
            displacedVertices[i] = v;
        }

        deformingMesh.vertices = displacedVertices;
        deformingMesh.RecalculateNormals();
        deformingMesh.RecalculateBounds();
    }
}