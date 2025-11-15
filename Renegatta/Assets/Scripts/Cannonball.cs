using UnityEngine;
using UnityEngine.Pool;

public class Cannonball : MonoBehaviour
{
    private ObjectPool<GameObject> pool;

    public void SetPool(ObjectPool<GameObject> pool)
    {
        this.pool = pool;
    }

    void OnCollisionEnter(Collision collision)
    {
        // do hit logic here

        // release back to pool
        pool.Release(gameObject);
    }

    void Update()
    {
        // Destroy if far below water
        if (transform.position.y < -10f) pool.Release(gameObject);
    }
}
