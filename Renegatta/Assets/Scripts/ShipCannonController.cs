using UnityEngine;
using UnityEngine.Pool;

public class ShipCannonController : MonoBehaviour
{
    [Header("Cannon Settings")]
    public GameObject cannonballPrefab;
    private ObjectPool<GameObject> cannonballPool;
    public Transform starboardCannon;
    public Transform starboardCannonForward;
    public Transform starboardCannonStern;
    public Transform portCannon;
    public Transform portCannonForward;
    public Transform portCannonStern;

    public float minForce = 10f;
    public float maxForce = 100f;
    public float pingPongSpeed = 2f; // how fast the force oscillates

    private float starboardForce;
    private float portForce;

    private TrajectoryPredictor starboardPredictor;
    private TrajectoryPredictor starboardForwardPredictor;
    private TrajectoryPredictor starboardSternPredictor;

    private TrajectoryPredictor portPredictor;
    private TrajectoryPredictor portForwardPredictor;
    private TrajectoryPredictor portSternPredictor;
    public Texture lineTexture;

void Awake()
{
    // Create a runtime parent for all cannonballs
    GameObject poolParent = new GameObject("Cannonballs");

    cannonballPool = new ObjectPool<GameObject>(
        createFunc: () =>
        {
            // Instantiate under the parent for organization
            GameObject obj = Instantiate(cannonballPrefab, poolParent.transform);
            obj.GetComponent<Cannonball>().SetPool(cannonballPool); // optional if bullets manage themselves
            return obj;
        },
        actionOnGet: obj => obj.SetActive(true),
        actionOnRelease: obj => obj.SetActive(false),
        actionOnDestroy: obj => Destroy(obj),
        collectionCheck: false,
        defaultCapacity: 10,
        maxSize: 120
    );

    
}

    void Start()
    {
        starboardPredictor        = CreatePredictor(starboardCannon.gameObject);
        starboardForwardPredictor = CreatePredictor(starboardCannonForward.gameObject);
        starboardSternPredictor   = CreatePredictor(starboardCannonStern.gameObject);

        portPredictor        = CreatePredictor(portCannon.gameObject);
        portForwardPredictor = CreatePredictor(portCannonForward.gameObject);
        portSternPredictor   = CreatePredictor(portCannonStern.gameObject);

        starboardForce = minForce;
        portForce = minForce;
    }

    void Update()
    {
        // Ping-pong starboard force when holding C
        if (Input.GetKey(KeyCode.C))
        {
            starboardForce = Mathf.PingPong(Time.time * pingPongSpeed, maxForce - minForce) + minForce;
        }

        // Ping-pong port force when holding Z
        if (Input.GetKey(KeyCode.Z))
        {
            portForce = Mathf.PingPong(Time.time * pingPongSpeed, maxForce - minForce) + minForce;
        }

        // Fire cannonballs
        if (Input.GetKeyDown(KeyCode.X))
        {
            LaunchCannon(starboardCannon, starboardForce, 0f);
            LaunchCannon(portCannon, portForce, 0f);
            LaunchCannon(starboardCannonForward, starboardForce, 20f);
            LaunchCannon(portCannonForward, portForce, 20f);
            LaunchCannon(starboardCannonStern, starboardForce, -20f);
            LaunchCannon(portCannonStern, portForce, -20f);
        }
    }

    void LateUpdate()
    {
        // Update predictors
        starboardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardPredictor.Predict3D(starboardCannon.position, starboardCannon.forward * starboardForce, Physics.gravity);

        portPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portPredictor.Predict3D(portCannon.position, portCannon.forward * portForce, Physics.gravity);

        starboardForwardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardForwardPredictor.Predict3D(starboardCannonForward.position, starboardCannonForward.forward * starboardForce, Physics.gravity);

        portForwardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portForwardPredictor.Predict3D(portCannonForward.position, portCannonForward.forward * portForce, Physics.gravity);

        starboardSternPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardSternPredictor.Predict3D(starboardCannonStern.position, starboardCannonStern.forward * starboardForce, Physics.gravity);

        portSternPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portSternPredictor.Predict3D(portCannonStern.position, portCannonStern.forward * portForce, Physics.gravity);
    }

    void LaunchCannon(Transform cannon, float force, float angle)
    {
        GameObject ball = cannonballPool.Get();
        ball.transform.position = cannon.position;
        ball.transform.rotation = cannon.rotation;
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        Vector3 forwardRotation = Quaternion.Euler(0, angle, 0) * cannon.forward;
        rb.linearVelocity = cannon.forward * force;
    }

    private TrajectoryPredictor CreatePredictor(GameObject cannonObj)
    {
        var predictor = cannonObj.AddComponent<TrajectoryPredictor>();
        predictor.drawDebugOnPrediction = true;
        predictor.reuseLine = true;
        predictor.accuracy = 0.99f;
        predictor.lineWidth = 0.2f;
        predictor.iterationLimit = 600;
        predictor.lineTexture = lineTexture;
        predictor.textureTilingMult = 0.35f;
        predictor.lineStartColor = Color.red;

        return predictor;
    }
}
