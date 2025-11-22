using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using EasyTransition;   // EasyTransitions namespace

public class DeathHandler : MonoBehaviour
{
    public PlayerHealth health;

    [Header("Disable on death")]
    public Behaviour[] componentsToDisable;
    public ParticleSystem[] particlesToStop;

    [Header("Death UI")]
    public GameObject shipwreckedUIPanel;

    [Header("Death Animation")]
    public float deathTiltZ = 20f;
    public float deathTiltX = 10f;
    public float sinkDistance = 5f;
    public float sinkDuration = 3f;
    public float deathDelayBeforeTransition = 1.5f;

    [Header("Transition")]
    public TransitionSettings transitionSettings; // <-- correct type
    public float transitionDuration = 1f;
    public TransitionManager transitionManager;


    private void Awake()
    {
        if (health != null)
            health.OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        foreach (var c in componentsToDisable)
        {
            if (c != null)
                c.enabled = false;
        }

        foreach (var p in particlesToStop)
        {
            if (p != null)
                p.Stop();
        }

        GetComponent<Collider>().enabled = false;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        if (shipwreckedUIPanel != null)
            shipwreckedUIPanel.SetActive(true);

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        Quaternion targetRot = Quaternion.Euler(
            deathTiltX,
            startRot.eulerAngles.y,
            startRot.eulerAngles.z + deathTiltZ);

        Vector3 targetPos = startPos - Vector3.up * sinkDistance;

        float elapsed = 0f;

        // Sink and tilt animation
        while (elapsed < sinkDuration)
        {
            float t = elapsed / sinkDuration;
            transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localRotation = targetRot;
        transform.localPosition = targetPos;

        // Small pause before transition
        yield return new WaitForSeconds(deathDelayBeforeTransition);

        // Perform the dissolve scene transition
        Scene active = SceneManager.GetActiveScene();

        if (transitionManager != null && transitionSettings != null)
        {
            transitionManager.Transition(
                active.name,
                transitionSettings,
                transitionDuration
            );
        }
        else
        {
            Debug.LogError("[DeathHandler] Missing TransitionManager or TransitionSettings.");
            SceneManager.LoadScene(active.name); // fallback
        }

    }
}
