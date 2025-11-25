using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System;
using System.Collections.Generic;

public class ShipCannonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Rigidbody shipRigidbody;
    [SerializeField] PlayerHealth playerHealth;
    public GameObject cannonballPrefab;

    [Header("Cannon Settings")]
    [SerializeField] float force;
    public Transform starboardCannon;
    public Transform starboardCannonForward;
    public Transform starboardCannonStern;
    public Transform portCannon;
    public Transform portCannonForward;
    public Transform portCannonStern;
    private ObjectPool<GameObject> cannonballPool;
    private static readonly Vector3 poolInitPos = new Vector3(1000f, 1000f, 1000f);

    [Header("Reload Settings")]
    public float initialDelay = 3f;        // big first-time delay
    public float reloadDuration = 1.5f;    // normal reload time
    public AudioSource reloadAudioSource;
    float reloadVolumeOriginal;
    public float reloadSFXDelay;
    public Texture lineTexture;

    [Header("Predictor Settings")]
    [SerializeField] private Shader lineShader;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float fakeForceSmoothTime = 0.1f; // smaller = snappier

    bool SFXinitialized = false;
    private bool cannonsReady = false;
    private float reloadTimer;
   
    static readonly Color lineColor = Color.red;

    public event Action<float> OnCannonVisualSignal;
    private float fakeForce;
    private float fakeForceVelocity = 0f; // required for SmoothDamp
    private List<TrajectoryPredictor> allPredictors = new List<TrajectoryPredictor>();

    void Awake()
    {
        // Create a runtime parent for all cannonballs
        GameObject poolParent = new GameObject("Cannonballs");

        cannonballPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(cannonballPrefab, poolParent.transform);
                obj.transform.position = poolInitPos;
                var cb = obj.GetComponent<Cannonball>();
                if (cb != null) cb.SetPool(cannonballPool);
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

        if (reloadAudioSource != null)
            reloadVolumeOriginal = reloadAudioSource.volume;
    }

    void Start()
    {
        // Create predictors but don't auto-draw until X is held
        allPredictors.Clear();
        allPredictors.Add(CreatePredictor(starboardCannon.gameObject));
        allPredictors.Add(CreatePredictor(starboardCannonForward.gameObject));
        allPredictors.Add(CreatePredictor(starboardCannonStern.gameObject));
        allPredictors.Add(CreatePredictor(portCannon.gameObject));
        allPredictors.Add(CreatePredictor(portCannonForward.gameObject));
        allPredictors.Add(CreatePredictor(portCannonStern.gameObject));

        reloadTimer = initialDelay;
        cannonsReady = false;
        SetPredictorsVisible(false);

        // Warm pool
        for (int i = 0; i < 12; i++)
        {
            var ball = cannonballPool.Get();
            cannonballPool.Release(ball);
        }
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
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && cannonsReady || Input.GetKeyDown(KeyCode.X) && cannonsReady)
        {
            fakeForce = 0f;
        }

        if (Input.GetKey(KeyCode.Space) && cannonsReady || Input.GetKey(KeyCode.X) && cannonsReady)
        {
            SetPredictorsVisible(true);
            fakeForce = Mathf.SmoothDamp(fakeForce, force, ref fakeForceVelocity, fakeForceSmoothTime);
        }

        if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.X))
        {
            SetPredictorsVisible(false);
            fakeForce = 0f;
            fakeForceVelocity = 0f;
        }

        if (Input.GetKeyUp(KeyCode.Space) && cannonsReady || Input.GetKeyUp(KeyCode.X) && cannonsReady)
        {
            cannonsReady = false;
            reloadTimer = reloadDuration;

            if (SFXinitialized)
            {
                Invoke(nameof(BeginReloadSFXTimer), reloadSFXDelay);
            }
            else SFXinitialized = true;

            FireAllCannons();
            OnCannonVisualSignal?.Invoke(0f);
        }
    }

    void LateUpdate()
    {
        Vector3 horizontalShipVelocity = new Vector3(shipRigidbody.linearVelocity.x, 0f, shipRigidbody.linearVelocity.z);

        foreach (var predictor in allPredictors)
        {
            if (predictor == null)
            {
                Debug.LogWarning("Predictor is null");

                continue;
            }

            predictor.debugLineDuration = Time.unscaledDeltaTime;
            predictor.Predict3D(
                predictor.transform.position,
                predictor.transform.forward * fakeForce + horizontalShipVelocity,
                Physics.gravity
            );
        }
    }

    private void BeginReloadSFXTimer()
    {
        if (reloadAudioSource == null) return;
        reloadAudioSource.volume = UnityEngine.Random.Range(0.8f, 1f) * reloadVolumeOriginal;
        reloadAudioSource.pitch = UnityEngine.Random.Range(0.8f, 1.2f);
        reloadAudioSource.Play();
    }

    void FireAllCannons()
    {
        StartCoroutine(LaunchCannonWithDelay(starboardCannon, force, 0f));
        StartCoroutine(LaunchCannonWithDelay(portCannon, force, 0f));
        StartCoroutine(LaunchCannonWithDelay(starboardCannonForward, force, 20f));
        StartCoroutine(LaunchCannonWithDelay(portCannonForward, force, 20f));
        StartCoroutine(LaunchCannonWithDelay(starboardCannonStern, force, -20f));
        StartCoroutine(LaunchCannonWithDelay(portCannonStern, force, -20f));
    }

    IEnumerator LaunchCannonWithDelay(Transform cannon, float force, float angle)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.03f, 0.22f));
        AudioSource audioSource = cannon.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioSource.volume = UnityEngine.Random.Range(0.25f, 0.4f);
            audioSource.Play();
        }

        GameObject ball = cannonballPool.Get();
        ball.transform.position = cannon.position;
        ball.transform.rotation = cannon.rotation;
        Rigidbody rb = ball.GetComponent<Rigidbody>();
        ball.SetActive(true);
        if (rb != null)
        {
            Vector3 horizontalShipVelocity = new Vector3(shipRigidbody.linearVelocity.x, 0f, shipRigidbody.linearVelocity.z);
            rb.linearVelocity = cannon.forward * force + horizontalShipVelocity;
        }
    }

    private TrajectoryPredictor CreatePredictor(GameObject cannonObj)
    {
        var predictor = cannonObj.AddComponent<TrajectoryPredictor>();
        predictor.drawDebugOnPrediction = false; // default off; X will turn it on
        predictor.reuseLine = true;
        predictor.accuracy = 0.99f;
        predictor.lineWidth = 0.4f;
        predictor.iterationLimit = 250;
        predictor.lineTexture = lineTexture;
        predictor.textureTilingMult = 0.35f;
        predictor.lineStartColor = lineColor;
        predictor.lineEndColor = lineColor;
        return predictor;
    }

    void SetPredictorsVisible(bool visible)
    {
        foreach (var predictor in allPredictors)
            SetPredictorVisibility(predictor, visible);
    }


    void SetPredictorVisibility(TrajectoryPredictor predictor, bool visible)
    {
        if (predictor == null) return;
        if (playerHealth.isInvulnerable) visible = false;

        // Let the predictor draw (or not) on prediction
        predictor.drawDebugOnPrediction = visible;

        // If the debug line exists, enable/disable the LineRenderer
        if (predictor.debugLine != null)
        {
            LineRenderer lr = predictor.debugLine;
            lr.enabled = visible;
        }
    }
}
