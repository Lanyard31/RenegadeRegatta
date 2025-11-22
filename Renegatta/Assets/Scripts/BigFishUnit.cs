using UnityEngine;

public enum BigFishBehavior
{
    Follow,
    WakeRide,
    BowCross
}

public class BigFishUnit : MonoBehaviour
{
    [Header("Behavior")]
    public BigFishBehavior behavior;

    [Header("Movement")]
    public float speed = 8f;
    public float diveSpeed = 5f;

    [Header("Lifetime")]
    public float activeTime = 6f;

    [HideInInspector] public BigFishManager manager;
    [HideInInspector] public Transform player;

    private float timer;
    private bool descending;
    private bool finished;

    private float bowCrossHeightOffset = -6f;

    void Start()
    {
        timer = activeTime;
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
        Vector3 target = new Vector3(player.position.x, 0f, player.position.z) + Vector3.back * 10f + Vector3.down * 2f;

        // Move forward toward target
        transform.position += (target - transform.position).normalized * speed * Time.deltaTime;

        // Look at player
        Vector3 dir = (target - transform.position).normalized;
        if (dir != Vector3.zero)
            transform.forward = Vector3.Lerp(transform.forward, dir, Time.deltaTime * 2f);

        // Begin descending gradually
        if (timer < activeTime * 0.4f)
            transform.position += Vector3.down * diveSpeed * Time.deltaTime;
    }

    void DoWakeRide()
    {
        Vector3 bowPoint = new Vector3(player.position.x, 0f, player.position.z)
                           + player.right * 6f;

        Vector3 oldPos = transform.position;
        Vector3 next = Vector3.MoveTowards(oldPos, bowPoint, speed * Time.deltaTime);

        // preserve Y
        next.y = oldPos.y;
        transform.position = next;

        transform.forward = Vector3.Lerp(transform.forward, player.forward, Time.deltaTime * 3f);
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

    void Finish()
    {
        finished = true;
        manager.NotifyUnitFinished();
        Destroy(gameObject);
    }
}
