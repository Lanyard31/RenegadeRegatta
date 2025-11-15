using UnityEngine;
using UnityEngine.Pool;

public class ShipCannonController : MonoBehaviour
{
    [Header("Cannon Settings")]
    public GameObject cannonballPrefab;
    private ObjectPool<GameObject> cannonballPool;
    public Transform starboardCannon;
    public Transform portCannon;

    public float minForce = 10f;
    public float maxForce = 100f;
    public float pingPongSpeed = 2f; // how fast the force oscillates

    private float starboardForce;
    private float portForce;

    private TrajectoryPredictor starboardPredictor;
    private TrajectoryPredictor portPredictor;
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
        // Setup predictors
        starboardPredictor = starboardCannon.gameObject.AddComponent<TrajectoryPredictor>();
        starboardPredictor.drawDebugOnPrediction = true;
        starboardPredictor.reuseLine = true;
        starboardPredictor.accuracy = 0.99f;
        starboardPredictor.lineWidth = 0.2f;
        starboardPredictor.iterationLimit = 600;
        starboardPredictor.lineTexture = lineTexture;
        starboardPredictor.textureTilingMult = 0.35f;
		starboardPredictor.lineWidth = 0.2f;
        starboardPredictor.lineStartColor = Color.red;

        portPredictor = portCannon.gameObject.AddComponent<TrajectoryPredictor>();
        portPredictor.drawDebugOnPrediction = true;
        portPredictor.reuseLine = true;
        portPredictor.accuracy = 0.99f;
        portPredictor.lineWidth = 0.2f;
        portPredictor.iterationLimit = 600;
        portPredictor.lineTexture = lineTexture;
        portPredictor.textureTilingMult = 0.35f;
		portPredictor.lineWidth = 0.2f;
        portPredictor.lineStartColor = Color.red;

        starboardForce = Mathf.PingPong(Time.time * pingPongSpeed, maxForce - minForce) + minForce;
        portForce = Mathf.PingPong(Time.time * pingPongSpeed, maxForce - minForce) + minForce;
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
            LaunchCannon(starboardCannon, starboardForce);
            LaunchCannon(portCannon, portForce);
        }
    }

    void LateUpdate()
    {
        // Update predictors
        starboardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardPredictor.Predict3D(starboardCannon.position, starboardCannon.forward * starboardForce, Physics.gravity);

        portPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portPredictor.Predict3D(portCannon.position, portCannon.forward * portForce, Physics.gravity);
    }

    void LaunchCannon(Transform cannon, float force)
    {
        GameObject ball = cannonballPool.Get();
        ball.transform.position = cannon.position;
        ball.transform.rotation = cannon.rotation;
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        rb.linearVelocity = cannon.forward * force;

        //also launch another ball slightly behind and another slightly ahead
        GameObject ball2 = cannonballPool.Get();
        ball2.transform.position = cannon.position + (cannon.right * 1.5f);
        ball2.transform.rotation = cannon.rotation;
        Rigidbody rb2 = ball2.GetComponent<Rigidbody>();
        //adjust the cannon forward with a small forward rotation to spread them out
        Vector3 forwardRotation = Quaternion.Euler(0, 20, 0) * cannon.forward;
        rb2.linearVelocity = (cannon.forward + forwardRotation * force) * 0.8f;

        GameObject ball3 = cannonballPool.Get();
        ball3.transform.position = cannon.position - (cannon.right * 1.5f);
        ball3.transform.rotation = cannon.rotation;
        Rigidbody rb3 = ball3.GetComponent<Rigidbody>();
        Vector3 backwardRotation = Quaternion.Euler(0, -20, 0) * cannon.forward;
        rb3.linearVelocity = (cannon.forward + backwardRotation * force) * 0.8f;
    }
}
