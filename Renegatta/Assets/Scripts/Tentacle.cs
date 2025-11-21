using UnityEngine;

public class Tentacle : MonoBehaviour
{
    [Header("External")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private GameObject rockPrefab;
    [SerializeField] private GameObject heldRock;
    [SerializeField] private ParticleSystem bubbleParticles;

    [Header("Movement")]
    private float submergedY = -21f;
    private float emergedY = -2.5f;
    [SerializeField] private float verticalSpeed = 2f;

    [Header("Distances")]
    [SerializeField] private float emergeDistance = 20f;
    [SerializeField] private float throwDistance = 12f;
    [SerializeField] private float rotateSpeed = 4f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 3;
    private int currentHealth;

    private Transform player;
    private Animator anim;

    private bool aboveWater;
    private bool rising;      // used so we know when rising just started
    private bool armed;       // has a rock in hand
    private bool busy;        // mid-animation
    private bool isDead;
    private float grabRockRandomYaw = 0f;
    private bool grabRockOffsetChosen = false;
    [SerializeField] private AudioSource whipCrackSound;
    private float originalVolume;
    [SerializeField] private AudioSource deathRoarSound;
    private float originalRoarVolume;
    [SerializeField] private ParticleSystem whipCrackParticles;

    void Start()
    {
        anim = GetComponent<Animator>();
        originalVolume = whipCrackSound.volume;
        originalRoarVolume = deathRoarSound.volume;
        currentHealth = maxHealth;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;

        // Start submerged
        Vector3 pos = transform.position;
        pos.y = submergedY;
        transform.position = pos;

        if (heldRock) heldRock.SetActive(false);
    }

    void Update()
    {
        if (!player) return;

        float dist = Vector3.Distance(player.position, transform.position);

        HandleVerticalMovement(dist);
        HandleRotation();
        HandleThrowLogic(dist);
        HandleIdle();
    }

    private void HandleIdle()
    {
        var state = anim.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Idle") && busy)
        {
            busy = false;
        }
    }

    private void HandleVerticalMovement(float dist)
    {
        if (!aboveWater && dist < emergeDistance && currentHealth > 0)
        {
            aboveWater = true;
            rising = true; // mark that we've just started rising
            bubbleParticles.Stop();
        }

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            aboveWater = false;
            verticalSpeed *= 1.65f;
            busy = true;
            anim.SetTrigger("Hit");

            //randomize pitch and volume
            deathRoarSound.pitch = Random.Range(0.9f, 1.1f);
            deathRoarSound.volume = Random.Range(0.8f, 1.2f) * originalRoarVolume;
            deathRoarSound.Play();
        }

        float targetY = aboveWater ? emergedY : submergedY;

        Vector3 pos = transform.position;
        pos.y = Mathf.MoveTowards(pos.y, targetY, verticalSpeed * Time.deltaTime);
        transform.position = pos;

        // Once rising finishes (y reaches emergedY), grab a rock
        if (rising && Mathf.Approximately(pos.y, emergedY))
        {
            rising = false;
            if (!armed)
            {
                busy = true;
                anim.SetTrigger("GrabRock");
            }
        }

        //if dead and fully submerged, delete
        if (isDead && (pos.y < submergedY) && !deathRoarSound.isPlaying)
        {
            Destroy(gameObject);
        }
    }

private void HandleRotation()
{
    if (!aboveWater || !player) return;

    Vector3 dir = player.position - transform.position;
    dir.y = 0;

    if (dir.sqrMagnitude < 0.1f) return;

    Quaternion targetRot = Quaternion.LookRotation(dir);

    // Check animator state
    var state = anim.GetCurrentAnimatorStateInfo(0);
    bool isGrabbing = state.IsName("GrabRock");

    if (isGrabbing && !armed)
    {
            // Choose a one-time random yaw offset when animation starts
            if (!grabRockOffsetChosen)
            {
                grabRockOffsetChosen = true;
                // Choose either left or right side, avoiding the center
                if (Random.value < 0.5f)
                {
                    // Negative range
                    grabRockRandomYaw = Random.Range(-80f, -10f);
                }
                else
                {
                    // Positive range
                    grabRockRandomYaw = Random.Range(10f, 80f);
                }

            }

            // Apply the rotation offset
            targetRot *= Quaternion.Euler(0f, grabRockRandomYaw, 0f);
    }
    else
    {
        // Reset so next grab picks a new value
        grabRockOffsetChosen = false;
    }

    transform.rotation = Quaternion.Slerp(
        transform.rotation,
        targetRot,
        rotateSpeed * Time.deltaTime
    );
}


    private void HandleThrowLogic(float dist)
    {
        if (!aboveWater || busy || currentHealth <= 0) return;

        if (armed && dist < throwDistance)
        {
            busy = true;
            anim.SetTrigger("Throw");
            //play whip sfx
            Invoke(nameof(PlayWhipAudio), 0.21f);
        }
    }

    // --- Animation Events ---

    // Called when GrabRock animation reaches the moment it grabs the rock
    public void OnGrabRock()
    {
        if (heldRock) heldRock.SetActive(true);
        armed = true;
    }

public void OnThrowRock()
{
    if (heldRock) heldRock.SetActive(false);

    armed = false;

    GameObject rock = Instantiate(rockPrefab, throwPoint.position, throwPoint.rotation);

    whipCrackParticles.Play();

    if (rock.TryGetComponent<Rigidbody>(out var rb))
    {
        Vector3 target = player.position + Vector3.up * 1.2f; // aim for upper body
        Vector3 start = throwPoint.position;

        float speed = 25f;

        // Compute launch velocity
        Vector3 toTarget = target - start;
        Vector3 toTargetXZ = new Vector3(toTarget.x, 0, toTarget.z);
        float y = toTarget.y;
        float xz = toTargetXZ.magnitude;

        float gravity = Mathf.Abs(Physics.gravity.y);
        float v2 = speed * speed;

        float underRoot = v2 * v2 - gravity * (gravity * xz * xz + 2 * y * v2);
        if (underRoot < 0f) underRoot = 0f; // in case target is VERY close

        float root = Mathf.Sqrt(underRoot);

        float lowAngle = Mathf.Atan( (v2 - root) / (gravity * xz) );

        Vector3 launchDir = toTargetXZ.normalized;
        Vector3 launchVelocity =
            launchDir * Mathf.Cos(lowAngle) * speed +
            Vector3.up * Mathf.Sin(lowAngle) * speed;

        rb.linearVelocity = launchVelocity;
        //add slight random spin
        rb.AddTorque(Random.Range(-75f, 75f), Random.Range(-75f, 75f), Random.Range(-75f, 75f));
    }
}

    // Called at the END of GrabRock or Throw animations
    public void OnAnimationFinished()
    {
        busy = false;

        // If Throw finished, prepare next rock immediately
        if (!armed && aboveWater && currentHealth > 0)
        {
            busy = true;
            anim.SetTrigger("GrabRock");
        }
    }

    public void TakeDamage(int dmg)
    {
        if (currentHealth <= 0) return;

        currentHealth -= dmg;

        // Drop rock if holding one
        if (armed)
        {
            armed = false;
            if (heldRock) heldRock.SetActive(false);
            GameObject rock = Instantiate(rockPrefab, throwPoint.position, throwPoint.rotation);
            //weak toss with a bit of rotation added
            rock.GetComponent<Rigidbody>().AddTorque(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
            rock.GetComponent<Rigidbody>().AddForce(Vector3.forward * Random.Range(3f, 5f), ForceMode.Impulse);


        }

        busy = true;
        anim.SetTrigger("Hit");
    }

    private void PlayWhipAudio()
    {
        //randomize pitch and volume
        whipCrackSound.pitch = Random.Range(0.5f, 0.7f);
        whipCrackSound.volume = Random.Range(0.8f, 1.2f) * originalVolume;
        whipCrackSound.Play();
    }
}
