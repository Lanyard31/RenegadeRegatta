using UnityEngine;

public class BossSetup : MonoBehaviour
{
    [Header("Refs")]
    public GameObject[] NonBossTentacles;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Fire the boss music
        MusicController.Instance.ChangeToBossMusic();

        // Clean up the appetizer-course tentacles
        KillAllTentacles();
    }

    private void KillAllTentacles()
    {
        foreach (var tentacle in NonBossTentacles)
        {
            if (tentacle != null)
                Destroy(tentacle);
        }
    }
}
