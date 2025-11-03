using UnityEngine;

namespace Heathen.Demos
{
    [System.Obsolete("This script is for demonstration purposes ONLY")]
    public class Sample1FantasyProjectile : MonoBehaviour
    {
        public Vector3 velocity;
        public float gravity;

        private Rigidbody selfBody;

        private void Start()
        {
            selfBody = GetComponent<Rigidbody>();
            selfBody.transform.rotation = Quaternion.LookRotation(velocity);
            Invoke("TimeKill", 10f);
            selfBody.drag = 0;
            selfBody.angularDrag = 0;
            selfBody.velocity = velocity;
            selfBody.useGravity = false;
        }

        private void FixedUpdate()
        {
            selfBody.AddForce(0, -gravity * selfBody.mass, 0);
            selfBody.transform.rotation = Quaternion.LookRotation(selfBody.velocity);
        }

        void TimeKill()
        {
            Debug.Log("Time Kill");
            Destroy(gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log("Collided");
            Destroy(gameObject);
        }
    }
}