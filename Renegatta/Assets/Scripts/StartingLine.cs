using UnityEngine;

public class StartingLine : MonoBehaviour
{
    public RaceTimer timer;
    public GameObject fireworkPrefab;
    public Transform fireworkPointA;
    public Transform fireworkPointB;

    public GameObject objectToHide;

    private bool triggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        Instantiate(fireworkPrefab, fireworkPointA.position, Quaternion.identity);
        Instantiate(fireworkPrefab, fireworkPointB.position, Quaternion.identity);

        if (objectToHide) objectToHide.SetActive(false);

        timer.StartTimer();
    }
}
