using UnityEngine;

public class CoinCollectTrigger : MonoBehaviour
{
    private CoinPickup coin;

    void Awake()
    {
        // Find the CoinPickup on the parent. 
        // This keeps the prefab tidy.
        coin = GetComponentInParent<CoinPickup>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (coin == null)
        {
            Debug.LogWarning("CoinCollectTrigger has no parent CoinPickup. Fix your prefab, captain.");
            return;
        }

        coin.OnCollectTrigger(other);
    }
}
