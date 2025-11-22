using UnityEngine;

public class SeagullUnit : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string glideState = "Glide";
    public string flapTrigger = "Flap";

    [Header("Motion")]
    public float flapIntervalMin = 1.5f;
    public float flapIntervalMax = 4f;
    public float flapLift = 0.5f;

    private float flapTimer;
    private bool scattered;

    void Start()
    {
        ResetFlapTimer();
        if (animator)
            animator.Play(glideState);
    }

    void Update()
    {
        if (scattered)
        {
            transform.position += Vector3.up * Time.deltaTime * 6f;
            return;
        }

        flapTimer -= Time.deltaTime;
        if (flapTimer <= 0f)
        {
            DoFlap();
            ResetFlapTimer();
        }
    }

    void DoFlap()
    {
        if (animator)
            animator.SetTrigger(flapTrigger);

        // slight upward lift to match the animation
        Vector3 p = transform.position;
        p.y += flapLift;
        transform.position = p;
    }

    void ResetFlapTimer()
    {
        flapTimer = Random.Range(flapIntervalMin, flapIntervalMax);
    }

    public void Scatter()
    {
        scattered = true;

        if (animator)
            animator.SetTrigger(flapTrigger);
    }
}
