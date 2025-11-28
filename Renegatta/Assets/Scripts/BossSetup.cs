using UnityEngine;

public class BossSetup : MonoBehaviour
{
    [Header("Refs")]
    public MusicController musicController;
    public GameObject[] NonBossTentacles;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // Fire the boss music
        musicController.ChangeToBossMusic();

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
