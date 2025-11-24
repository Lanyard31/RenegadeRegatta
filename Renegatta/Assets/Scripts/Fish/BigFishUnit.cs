using UnityEngine;

public enum BigFishBehavior
{
    Follow,
    WakeRide,
    BowCross,
    AirCross
}

public class BigFishUnit : MonoBehaviour
{
    [Header("Behavior")]
    public BigFishBehavior behavior;

    [Header("Movement")]
    public float speed = 8f;
    public float diveSpeed = 5f;
    public float surfaceYWakeRiderOffset = 0f;
    public Vector3 modelEulerWakeRiderOffset = Vector3.zero;
    public float followLerp = 3f;
    public float rotateLerp;

    [Header("Lifetime")]
    public float activeTime = 6f;

    [HideInInspector] public BigFishManager manager;
    [HideInInspector] public Transform player;

    private float timer;
    private bool descending;
    private bool finished;
    private bool surfaced;

    private float bowCrossHeightOffset = -6f;
    public AudioClip dolphinSfx;
    private Vector3 cachedOffset;
    private Vector3 airCrossDir;
    private bool airDirectionSet = false;
    private bool ascending = true;
    [SerializeField] private float spawnDistanceAir = 10f; // how far behind the flight path to spawn


    void Start()
    {
        timer = activeTime;
        cachedOffset = Random.value < 0.5f ?
               new Vector3(4f, -1f, 3.15f) :
               new Vector3(4f, -1f, -3.15f);
    }

    void Update()
    {
        if (finished) return;

        timer -= Time.deltaTime;

        // Run the normal behavior EVERY frame
        switch (behavior)
        {
            case BigFishBehavior.Follow:
                DoFollow();
                break;

            case BigFishBehavior.WakeRide:
                DoWakeRide();
                break;

            case BigFishBehavior.BowCross:
                DoBowCross();
                break;

            case BigFishBehavior.AirCross:
                AirCrossing();
                break;
        }

        // If descending, override ONLY THE Y MOTION
        if (descending)
        {
            ApplyDescendingY();
        }

        if (!descending && timer <= 0f)
            descending = true;
    }

    void ApplyDescendingY()
    {
        Vector3 p = transform.position;
        p.y -= diveSpeed * Time.deltaTime;
        transform.position = p;

        if (transform.position.y < player.position.y - 20f)
            Finish();
    }

    void DoFollow()
    {
        Vector3 target = new Vector3(player.position.x, 0f, player.position.z) + Vector3.back * 10f + Vector3.down * 4f;

        // Move forward toward target
        transform.position += (target - transform.position).normalized * speed * Time.deltaTime;

        // Look at player
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, dir, Time.deltaTime * 2f);

        // Begin descending gradually
        if (timer < activeTime * 0.4f)
            transform.position += Vector3.down * diveSpeed * 1.5f * Time.deltaTime;
    }

    void DoWakeRide()
    {
        Vector3 pos = transform.position;

        Vector3 target = player.TransformPoint(cachedOffset);

        float desiredY = target.y + surfaceYWakeRiderOffset;

        // 1) Rise toward surface
        if (!surfaced)
        {
            pos.y = Mathf.MoveTowards(pos.y, desiredY, diveSpeed * Time.deltaTime);

            if (Mathf.Abs(pos.y - desiredY) < 0.05f)
            {
                pos.y = desiredY;
                surfaced = true;

                if (dolphinSfx != null && Random.value < 0.5f)
                    AudioSource.PlayClipAtPoint(dolphinSfx, pos, Random.Range(0.25f, 0.3f));
            }
        }
        else if (!descending)
        {
            pos.y = desiredY;
        }

        // 2) Follow XZ
        Vector3 targetXZ = new Vector3(target.x, pos.y, target.z);
        pos = Vector3.Lerp(pos, targetXZ, followLerp * Time.deltaTime);

        transform.position = pos;

        if (player != null)
        {
            Quaternion look = Quaternion.LookRotation(player.forward, Vector3.up)
                              * Quaternion.Euler(modelEulerWakeRiderOffset);

            // Slow down rotation by interpolating from current to target
            transform.rotation = Quaternion.Slerp(transform.rotation, look, rotateLerp * Time.deltaTime);
        }
    }


    void DoBowCross()
    {
        Vector3 crossDir = -player.right;

        Vector3 oldPos = transform.position;
        Vector3 next = oldPos + crossDir.normalized * speed * Time.deltaTime;

        // maintain bow-cross animation height unless descending overrides it
        if (!descending)
            next.y = bowCrossHeightOffset;

        transform.position = next;

        transform.forward = crossDir.normalized;
    }

void AirCrossing()
{
    // pick direction once based on spawn rotation and offset spawn behind flight path
    if (!airDirectionSet)
    {
        // rotate spawn forward 0° on Y (or adjust if you want "with the wind")
        airCrossDir = Quaternion.Euler(0f, Random.Range(-10f, 10f), 0f) * transform.forward;

        // move spawn back along the flight path so bird starts off-camera
        transform.position -= airCrossDir.normalized * spawnDistanceAir;

        airDirectionSet = true;
    }

    Vector3 oldPos = transform.position;
    Vector3 next = oldPos + airCrossDir.normalized * speed * Time.deltaTime;

    // vertical movement
    if (timer < activeTime * 0.4f)
    {
        // event ending, ascend to get off camera
        next.y += 2f * Time.deltaTime; // faster ascent at end
    }
    else
    {
        // normal ascend/descend
        if (ascending)
            next.y += 0.5f * Time.deltaTime;
        else
            next.y -= 0.5f * Time.deltaTime;

        if (next.y >= bowCrossHeightOffset + 1f) ascending = false;
        if (next.y <= bowCrossHeightOffset) ascending = true;
    }

    transform.position = next;
    transform.forward = airCrossDir.normalized; // fixed flight direction
}

    void Finish()
    {
        finished = true;
        manager.NotifyUnitFinished();
        Destroy(gameObject);
    }
}
