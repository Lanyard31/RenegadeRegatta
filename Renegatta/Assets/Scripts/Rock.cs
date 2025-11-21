using UnityEngine;

public class Rock : MonoBehaviour
{
    [SerializeField] private GameObject explosionVFXGround;
    [SerializeField] private GameObject explosionVFXPlayer;
    [SerializeField] private GameObject explosionVFXSplashWater;
    [SerializeField] private AudioSource rockHit;
    private float originalVolume;
    Rigidbody rb;

    void Start()
    {
        rockHit = GetComponent<AudioSource>();
        originalVolume = rockHit.volume;
        rb = GetComponent<Rigidbody>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.tag == "Water")
        {
            Instantiate(explosionVFXSplashWater, transform.position + new Vector3(0, 1f, 0), Quaternion.identity);
            Destroy(gameObject);
        }

        else if (collision.transform.tag == "Player")
        {
            Instantiate(explosionVFXPlayer, transform.position, Quaternion.identity);

            //get component and cause some damage
            //collision.transform.GetComponent<PlayerHealth>().TakeDamage(1);
            Destroy(gameObject);
        }

        else if (collision.transform.tag == "Ground" || collision.transform.tag == "Cannonball")
        {
            Instantiate(explosionVFXGround, transform.position, Quaternion.FromToRotation(Vector3.up, collision.contacts[0].normal));
            //if (audioSource.isPlaying == false)
            {
                rockHit.pitch = Random.Range(0.85f, 1.15f);
                rockHit.volume = Random.Range(0.8f, 1.2f) * originalVolume;
                rockHit.Play();
            }

            //bounce
            rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, collision.contacts[0].normal);
        }
    }
}
