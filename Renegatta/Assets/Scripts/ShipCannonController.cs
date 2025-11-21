using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System;

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
    [SerializeField] Rigidbody shipRigidbody;
    private static readonly Vector3 poolInitPos = new Vector3(1000f, 1000f, 1000f);

    [Header("Reload Settings")]
    public float initialDelay = 3f;        // big first-time delay
    public float reloadDuration = 1.5f;    // normal reload time
    private float reloadTimer;
    private bool cannonsReady = false;

    public float minForce = 10f;
    public float maxForce = 100f;
    public float pingPongSpeed = 2f; // how fast the force oscillates
    private float starboardForce;
    private float portForce;
    private float starboardDir = 1f; // 1 = increasing, -1 = decreasing
    private float portDir = 1f;

    private TrajectoryPredictor starboardPredictor;
    private TrajectoryPredictor starboardForwardPredictor;
    private TrajectoryPredictor starboardSternPredictor;

    private TrajectoryPredictor portPredictor;
    private TrajectoryPredictor portForwardPredictor;
    private TrajectoryPredictor portSternPredictor;
    public Texture lineTexture;

    [SerializeField] float fadeSpeed = 6f;

    static readonly Color baseStart = Color.red;
    static readonly Color baseEnd = Color.orange;
    private float predictorTargetAlpha = 0f;

    [Header("Angle Mapping")]
    public float minAngle = 20f;   // flatter when force is high
    public float maxAngle = 65f;   // lob angle when force is low

    public event Action<float> OnCannonVisualSignal;

    void Awake()
    {
        // Create a runtime parent for all cannonballs
        GameObject poolParent = new GameObject("Cannonballs");

        cannonballPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                // Instantiate under the parent for organization
                GameObject obj = Instantiate(cannonballPrefab, poolParent.transform);
                obj.transform.position = poolInitPos;
                obj.GetComponent<Cannonball>().SetPool(cannonballPool); // optional if bullets manage themselves
                return obj;
            },
            actionOnGet: obj => { },
            actionOnRelease: obj =>
            {
                obj.SetActive(false);
                obj.transform.position = poolInitPos;
            },
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: true,
            defaultCapacity: 12,
            maxSize: 100
        );
    }

    void Start()
    {
        starboardPredictor = CreatePredictor(starboardCannon.gameObject);
        starboardForwardPredictor = CreatePredictor(starboardCannonForward.gameObject);
        starboardSternPredictor = CreatePredictor(starboardCannonStern.gameObject);

        portPredictor = CreatePredictor(portCannon.gameObject);
        portForwardPredictor = CreatePredictor(portCannonForward.gameObject);
        portSternPredictor = CreatePredictor(portCannonStern.gameObject);

        starboardForce = (minForce + maxForce) * 0.8f;
        portForce = (minForce + maxForce) * 0.8f;

        reloadTimer = initialDelay;
        cannonsReady = false;
        SetPredictorsVisible(false);
    }

    void Update()
    {
        // Handle reload timer
        if (!cannonsReady)
        {
            reloadTimer -= Time.deltaTime;
            if (reloadTimer <= 0f)
            {
                cannonsReady = true;
                //SetPredictorsVisible(true);
            }
        }

        UpdatePredictorFade();
        UpdatePredictorLines();

        // Fire cannonballs
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (!cannonsReady) return;

            SetPredictorsVisible(true);
        }

        if (Input.GetKeyUp(KeyCode.X))
        {
            if (!cannonsReady) return;
            cannonsReady = false;
            SetPredictorsVisible(false);
            reloadTimer = reloadDuration;
            FireAllCannons();
            OnCannonVisualSignal?.Invoke(0f);
        }
    }

    private void UpdatePredictorLines()
    {
        // Starboard
        if (Input.GetKey(KeyCode.C))
        {
            SetPredictorsVisible(true);
            starboardForce += starboardDir * pingPongSpeed * Time.deltaTime;

            // Bounce at limits
            if (starboardForce >= maxForce)
            {
                starboardForce = maxForce;
                starboardDir = -1f;
            }
            else if (starboardForce <= minForce)
            {
                starboardForce = minForce;
                starboardDir = 1f;
            }
        }

        // Port
        if (Input.GetKey(KeyCode.Z))
        {
            SetPredictorsVisible(true);
            portForce += portDir * pingPongSpeed * Time.deltaTime;

            if (portForce >= maxForce)
            {
                portForce = maxForce;
                portDir = -1f;
            }
            else if (portForce <= minForce)
            {
                portForce = minForce;
                portDir = 1f;
            }
        }

        // Reset direction to +1 when key is newly pressed
        if (Input.GetKeyDown(KeyCode.C)) starboardDir = 1f;
        if (Input.GetKeyDown(KeyCode.Z)) portDir = 1f;

        // --- After force update ---

        float starboardAngle = GetMappedAngle(starboardForce);
        float portAngle = GetMappedAngle(portForce);

        ApplyCannonAngles(starboardAngle, portAngle);

    }

    void LateUpdate()
    {
        // Update predictors
        starboardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardPredictor.Predict3D(starboardCannon.position, starboardCannon.forward * starboardForce + shipRigidbody.linearVelocity, Physics.gravity);

        portPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portPredictor.Predict3D(portCannon.position, portCannon.forward * portForce + shipRigidbody.linearVelocity, Physics.gravity);

        starboardForwardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardForwardPredictor.Predict3D(starboardCannonForward.position, starboardCannonForward.forward * starboardForce + shipRigidbody.linearVelocity, Physics.gravity);

        portForwardPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portForwardPredictor.Predict3D(portCannonForward.position, portCannonForward.forward * portForce + shipRigidbody.linearVelocity, Physics.gravity);

        starboardSternPredictor.debugLineDuration = Time.unscaledDeltaTime;
        starboardSternPredictor.Predict3D(starboardCannonStern.position, starboardCannonStern.forward * starboardForce + shipRigidbody.linearVelocity, Physics.gravity);

        portSternPredictor.debugLineDuration = Time.unscaledDeltaTime;
        portSternPredictor.Predict3D(portCannonStern.position, portCannonStern.forward * portForce + shipRigidbody.linearVelocity, Physics.gravity);
    }

    void FireAllCannons()
    {
        StartCoroutine(LaunchCannonWithDelay(starboardCannon, starboardForce, 0f));
        StartCoroutine(LaunchCannonWithDelay(portCannon, portForce, 0f));
        StartCoroutine(LaunchCannonWithDelay(starboardCannonForward, starboardForce, 20f));
        StartCoroutine(LaunchCannonWithDelay(portCannonForward, portForce, 20f));
        StartCoroutine(LaunchCannonWithDelay(starboardCannonStern, starboardForce, -20f));
        StartCoroutine(LaunchCannonWithDelay(portCannonStern, portForce, -20f));
    }

    IEnumerator LaunchCannonWithDelay(Transform cannon, float force, float angle)
    {

        yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.22f));
        AudioSource audioSource = cannon.GetComponent<AudioSource>();
        audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
        audioSource.volume = UnityEngine.Random.Range(0.25f, 0.4f);
        audioSource.Play();
        GameObject ball = cannonballPool.Get();
        ball.transform.position = cannon.position;
        ball.transform.rotation = cannon.rotation;
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        ball.SetActive(true);
        rb.linearVelocity = cannon.forward * force + shipRigidbody.linearVelocity;
        //give a big random spin

    }


    private TrajectoryPredictor CreatePredictor(GameObject cannonObj)
    {
        var predictor = cannonObj.AddComponent<TrajectoryPredictor>();
        predictor.drawDebugOnPrediction = true;
        predictor.reuseLine = true;
        predictor.accuracy = 0.99f;
        predictor.lineWidth = 0.4f;
        predictor.iterationLimit = 600;
        predictor.lineTexture = lineTexture;
        predictor.textureTilingMult = 0.35f;
        predictor.lineStartColor = Color.clear;
        predictor.lineEndColor = Color.clear;

        return predictor;
    }

    // --- Fade control ----------------------------------------------------

    // Set desired visibility. This no longer toggles enabled; it only sets the target alpha
    void SetPredictorsVisible(bool visible)
    {
        predictorTargetAlpha = visible ? 0.9f : 0f;
        // Do not toggle predictor.enabled here - control visibility purely via alpha
    }

    // Called each Update to drive the alpha toward predictorTargetAlpha
    void UpdatePredictorFade()
    {
        // Early out if all predictors already at target alpha (tiny optimization)
        // We'll check one predictor; they all move together so that's fine.
        if (starboardPredictor == null) return;

        float currentA = starboardPredictor.lineStartColor.a;
        if (Mathf.Approximately(currentA, predictorTargetAlpha)) return;

        float newA = Mathf.MoveTowards(currentA, predictorTargetAlpha, fadeSpeed * Time.deltaTime);

        ApplyAlphaToAllPredictors(newA);
    }

    void ApplyAlphaToAllPredictors(float alpha)
    {
        Color s0 = baseStart; s0.a = alpha;
        Color s1 = baseEnd; s1.a = alpha;

        starboardPredictor.lineStartColor = s0;
        starboardPredictor.lineEndColor = s1;

        starboardForwardPredictor.lineStartColor = s0;
        starboardForwardPredictor.lineEndColor = s1;

        starboardSternPredictor.lineStartColor = s0;
        starboardSternPredictor.lineEndColor = s1;

        portPredictor.lineStartColor = s0;
        portPredictor.lineEndColor = s1;

        portForwardPredictor.lineStartColor = s0;
        portForwardPredictor.lineEndColor = s1;

        portSternPredictor.lineStartColor = s0;
        portSternPredictor.lineEndColor = s1;
    }

    float GetMappedAngle(float force)
    {
        float t = Mathf.InverseLerp(minForce, maxForce, force);
        // 0 -> maxAngle, 1 -> minAngle
        return Mathf.Lerp(minAngle, maxAngle, t);
    }

    void ApplyCannonAngles(float starboardAngle, float portAngle)
    {
        // Starboard cannons pitch downward = local X rotation
        Vector3 e;

        e = starboardCannon.localEulerAngles;
        e.x = -starboardAngle;
        starboardCannon.localEulerAngles = e;

        e = starboardCannonForward.localEulerAngles;
        e.x = -starboardAngle;
        starboardCannonForward.localEulerAngles = e;

        e = starboardCannonStern.localEulerAngles;
        e.x = -starboardAngle;
        starboardCannonStern.localEulerAngles = e;

        // Port cannons may face the opposite direction, so we invert pitch
        e = portCannon.localEulerAngles;
        e.x = portAngle;
        portCannon.localEulerAngles = e;

        e = portCannonForward.localEulerAngles;
        e.x = portAngle;
        portCannonForward.localEulerAngles = e;

        e = portCannonStern.localEulerAngles;
        e.x = portAngle;
        portCannonStern.localEulerAngles = e;
    }


    public float GetReloadRatio()
    {
        if (cannonsReady) return 1f;                         // fully cooled
        return 1f - Mathf.Clamp01(reloadTimer / reloadDuration);
    }
}
