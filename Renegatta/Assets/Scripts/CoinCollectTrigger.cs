public class CoinCollectTrigger : MonoBehaviour
{
    [SerializeField] private CoinPickup coin;

    void OnTriggerEnter(Collider other)
    {
        coin.OnCollectTrigger(other);
    }
}
