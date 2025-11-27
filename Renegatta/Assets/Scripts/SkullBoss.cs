using UnityEngine;
using System;
using System.Collections.Generic;

public class SkullBoss : MonoBehaviour
{
    [Header("External")]
    public Transform player;
    [SerializeField] private MonoBehaviour hitFlash;

    [Header("Vertical Motion")]
    [SerializeField] private float submergedY = -22f;
    [SerializeField] private float emergedY = -2.5f;
    [SerializeField] private float verticalSpeed = 2f;
    [SerializeField] private WaveFollower waveFollower;

    [Header("Rotation")]
    [SerializeField] private bool rotateToFacePlayer = true;
    [SerializeField] private float rotateSpeed = 3f;

    [Header("Figure-Eight Movement")]
    [SerializeField] private float driftRadiusX = 4f;   // horizontal amplitude
    [SerializeField] private float driftRadiusZ = 2.5f; // forward amplitude
    [SerializeField] private float driftSpeed = 0.6f;   // speed of looping

    private Vector3 basePosition;

    [Header("Combat")]
    [SerializeField] private int maxHealth = 20;
    private int currentHealth;

    [Header("SFX")]
    [SerializeField] private AudioSource deathSFX;
    private float deathVolume;

    [Header("Linked Tentacles")]
    [SerializeField] private List<Tentacle> tentaclesToNotify;
    // They will handle their own flail-and-sink logic.

    private bool aboveWater;
    private bool rising;
    private bool isDead;

    public static event Action OnSkullBossDefeated;


    void Start()
    {
        currentHealth = maxHealth;

        if (!player)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        deathVolume = deathSFX ? deathSFX.volume : 1f;

        Vector3 pos = transform.position;
        pos.y = submergedY;
        transform.position = pos;

        basePosition = transform.position;
        if (!waveFollower) waveFollower = GetComponent<WaveFollower>();

    }


    void Update()
    {
        if (isDead)
        {
            HandleDeathMovement();
            return;
        }

        if (!player) return;

        HandleRiseLogic();
        HandleVerticalMotion();
        HandleRotation();
        HandleDrift();
    }


    private void HandleRiseLogic()
    {
        float dist = Vector3.Distance(player.position, transform.position);

        if (!aboveWater && dist < 150f)
        {
            aboveWater = true;
            rising = true;
        }

        // Only disable WaveFollower once at the start of rising
        if (rising && waveFollower && waveFollower.isActiveAndEnabled)
        {
            waveFollower.enabled = false;
        }
    }


    private void HandleVerticalMotion()
    {
        float targetY = aboveWater ? emergedY : submergedY;

        Vector3 p = transform.position;
        p.y = Mathf.MoveTowards(p.y, targetY, verticalSpeed * Time.deltaTime);
        transform.position = p;

        // Once fully emerged, enable WaveFollower just once
        if (aboveWater && !waveFollower.isActiveAndEnabled && Mathf.Abs(p.y - emergedY) < 0.05f)
        {
            if (waveFollower)
            {
                waveFollower.enabled = true;
            }
            rising = false; // Done rising
        }
    }

    private void HandleRotation()
    {
        if (!rotateToFacePlayer || !aboveWater)
            return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

            // Only yaw: preserve original local X and Z tilt
            Vector3 e = targetRot.eulerAngles;
            e.x = transform.eulerAngles.x;
            e.z = transform.eulerAngles.z;

            Quaternion fixedRot = Quaternion.Euler(e);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                fixedRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }


    private void HandleDrift()
    {
        if (!aboveWater)
            return;

        // Lissajous figure-eight pattern
        float t = Time.time * driftSpeed;

        float offsetX = Mathf.Sin(t) * driftRadiusX;
        float offsetZ = Mathf.Sin(t * 2f) * driftRadiusZ;

        Vector3 newPos = basePosition + transform.right * offsetX + transform.forward * offsetZ;

        // preserve vertical movement from rise/sink
        newPos.y = transform.position.y;

        transform.position = newPos;
    }


    private void HandleDeathMovement()
    {
        float targetY = submergedY;

        Vector3 p = transform.position;
        p.y = Mathf.MoveTowards(p.y, targetY, verticalSpeed * Time.deltaTime * 0.5f);
        transform.position = p;

        // Optional dramatic backward tilt
        Quaternion targetRot = Quaternion.Euler(-110f, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * 0.2f * Time.deltaTime);

        if (p.y <= submergedY - 1f)
            Destroy(gameObject);
    }


    public void TakeDamage(int dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        TriggerHitFlash();

        if (currentHealth <= 0)
        {
            StartDeathSequence();
        }
    }


    private void StartDeathSequence()
    {
        Debug.Log("StartDeathSequence called");
        isDead = true;
        aboveWater = false;
        rising = false;
        if (waveFollower) waveFollower.enabled = false;


        if (deathSFX)
        {
            deathSFX.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
            deathSFX.volume = deathVolume;
            deathSFX.Play();
        }

        // Notify tentacles to use their own flail-and-sink logic
        if (tentaclesToNotify != null)
        {
            foreach (var t in tentaclesToNotify)
                if (t) t.DontScreamDeath();
        }

        OnSkullBossDefeated?.Invoke();
    }


    private void TriggerHitFlash()
    {
        if (!hitFlash) return;

        var type = hitFlash.GetType();
        var m = type.GetMethod("Flash") ?? type.GetMethod("DoFlash");

        if (m != null)
            m.Invoke(hitFlash, null);
    }
}
