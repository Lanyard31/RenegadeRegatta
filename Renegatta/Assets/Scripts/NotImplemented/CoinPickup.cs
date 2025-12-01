using UnityEngine;
using System;

[RequireComponent(typeof(SphereCollider))]
[RequireComponent(typeof(Rigidbody))]
public class CoinPickup : MonoBehaviour
{
    [Header("Idle Motion")]
    [SerializeField] private float hoverAmplitude = 0.15f;
    [SerializeField] private float hoverSpeed = 2f;
    [SerializeField] private float spinSpeed = 90f;

    [Header("Spawn Pop-In")]
    [SerializeField] private float popInScaleTime = 0.25f;
    [SerializeField] private float popInSpinSpeed = 720f;
    [SerializeField] private float popInJumpForce = 3f;
    [SerializeField] private float popInSideForce = 2f;

    [Header("Magnet")]
    [SerializeField] private float magnetRadius = 6f;
    [SerializeField] private float magnetSpeed = 12f;
    [SerializeField] private float magnetScaleUp = 1.4f;
    [SerializeField] private float magnetSpinSpeed = 360f;

    [Header("Colliders")]
    [SerializeField] private SphereCollider attractCollider; // large trigger
    [SerializeField] private BoxCollider collectCollider;    // small trigger

    [Header("Audio")]
    [SerializeField] private AudioSource humSource;
    [SerializeField] private float humMaxVolume = 0.6f;

    [Header("VFX / SFX")]
    [SerializeField] private GameObject collectVFX;
    [SerializeField] private AudioClip collectSFX;

    [Header("Wave Bobbing (optional)")]
    [SerializeField] private SchoolWaveFollower waveFollow;

    public event Action OnCoinCollected;

    private Rigidbody rb;
    private Transform player;
    private Vector3 startPos;
    private Vector3 scaleStart;
    private bool spawnedRuntime = false;
    private bool magnetized = false;
    private float spawnTime = 0f;
    private float originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        scaleStart = transform.localScale;
        originalScale = scaleStart.x;
    }

    void Start()
    {
        startPos = transform.position;
    }

    public void SpawnPopIn()
    {
        spawnedRuntime = true;
        spawnTime = Time.time;

        transform.localScale = Vector3.zero;

        Vector3 side = UnityEngine.Random.insideUnitCircle.normalized;
        Vector3 hop = new Vector3(side.x, 1f, side.y);

        rb.AddForce(hop * popInJumpForce, ForceMode.Impulse);
        rb.AddForce(new Vector3(side.x, 0, side.y) * popInSideForce, ForceMode.Impulse);
    }

    void Update()
    {
        if (!player) FindPlayer();

        RotateIdle();
        HoverIdle();
        HandleSpawnPopIn();
        HandleHumVolume();
        HandleMagnet();
    }

    void RotateIdle()
    {
        float speed = magnetized ? magnetSpinSpeed :
                      spawnedRuntime && Time.time - spawnTime < popInScaleTime ? popInSpinSpeed :
                      spinSpeed;

        transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.World);
    }

    void HoverIdle()
    {
        if (magnetized || spawnedRuntime && Time.time - spawnTime < popInScaleTime) return;

        Vector3 p = transform.position;
        p.y = startPos.y + Mathf.Sin(Time.time * hoverSpeed) * hoverAmplitude;
        transform.position = p;
    }

    void HandleSpawnPopIn()
    {
        if (!spawnedRuntime) return;

        float t = (Time.time - spawnTime) / popInScaleTime;
        if (t < 1f)
        {
            float s = Mathf.Lerp(0f, originalScale, t);
            transform.localScale = new Vector3(s, s, s);
        }
        else
        {
            transform.localScale = Vector3.one * originalScale;
            spawnedRuntime = false;
        }
    }

    void HandleHumVolume()
    {
        if (!player || humSource == null) return;

        float dist = Vector3.Distance(player.position, transform.position);
        float t = 1f - Mathf.Clamp01(dist / magnetRadius);
        humSource.volume = t * humMaxVolume;
    }

    void HandleMagnet()
    {
        if (!magnetized || !player) return;

        Vector3 dir = (player.position - transform.position).normalized;
        rb.MovePosition(transform.position + dir * magnetSpeed * Time.deltaTime);

        float s = Mathf.Lerp(originalScale, magnetScaleUp, 0.5f);
        transform.localScale = Vector3.one * s;
    }

    // Large sphere trigger
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            magnetized = true;
            rb.useGravity = false;
        }
    }

    // Small box trigger
    public void OnCollectTrigger(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Collect(other.GetComponent<WindPushNew>());
    }

    void Collect(WindPushNew wind)
    {
        if (collectVFX) Instantiate(collectVFX, transform.position, Quaternion.identity);
        if (collectSFX) AudioSource.PlayClipAtPoint(collectSFX, transform.position);

        OnCoinCollected?.Invoke();

        if (wind != null) GivePlayerSpeedBoost(wind);

        Destroy(gameObject);
    }

    void GivePlayerSpeedBoost(WindPushNew wind)
    {
        // Add a simple public function to WindPushNew:
        // public void AddCoinBoost(float amount)
        wind.AddCoinBoost(3f);
    }

    void FindPlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }
}
