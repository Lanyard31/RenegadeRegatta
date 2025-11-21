using UnityEngine;
using UnityEngine.Pool;

public class Cannonball : MonoBehaviour
{
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private GameObject explosionVFXSquishy;
    [SerializeField] private GameObject explosionVFXSplashWater;
    Rigidbody rb;
    private bool isReleased = false;

    private ObjectPool<GameObject> pool;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void SetPool(ObjectPool<GameObject> pool)
    {
        this.pool = pool;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isReleased) return;
        isReleased = true;

        if (collision.transform.tag == "Water")
        {
            Instantiate(explosionVFXSplashWater, transform.position + new Vector3(0, 1f, 0), Quaternion.identity);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        else if (collision.transform.tag == "Tentacle")
        {
            //rotation to match collision
            Instantiate(explosionVFXSquishy, transform.position, Quaternion.FromToRotation(Vector3.up, collision.contacts[0].normal));
            Instantiate(explosionVFX, transform.position, Quaternion.FromToRotation(Vector3.up, collision.contacts[0].normal));
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            //get component and cause some damage
            collision.transform.GetComponent<Tentacle>().TakeDamage(1);
        }

        else if (collision.transform.tag == "Ground")
        {
            Instantiate(explosionVFX, transform.position, Quaternion.FromToRotation(Vector3.up, collision.contacts[0].normal));
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        pool.Release(gameObject);
    }

    void Update()
    {
        if (!isReleased && transform.position.y < -50f)
        {
            isReleased = true;
            pool.Release(gameObject);
        }
    }

    void OnEnable()
    {
        isReleased = false; // reset when reused
    }
}
