using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerKind { HowToSail, HowToTack, CompletionSave }
    public TriggerKind kind;

    // Optional - set to the Player tag you use
    [SerializeField] private string playerTag = "Player";

    public delegate void TriggerEvent(TutorialTrigger trigger);
    public static event TriggerEvent OnEntered;

    private void Awake()
    {
        // ensure it's a trigger
        var col = GetComponent<Collider>();
        if (col == null) Debug.LogError("TutorialTrigger needs a Collider.");
        else col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        OnEntered?.Invoke(this);
        this.enabled = false;
    }
}
