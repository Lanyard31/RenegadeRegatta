using Heathen.UnityPhysics;
using Heathen.UnityPhysics.API;
using UnityEngine;

namespace Heathen.Demos
{
    [System.Obsolete("This script is for demonstration purposes ONLY")]
    public class Sample3Launcher : MonoBehaviour
    {
        public Transform projector;
        public Transform emitter;
        public TrickShot2D trickShot;

        private void Aim()
        {
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (Ballistics.Solution2D(emitter.position, trickShot.speed, mousePos, Physics2D.gravity.magnitude, out Quaternion low, out Quaternion _) > 0)
                projector.rotation = low;
            else
                Debug.Log("No solution found!");
        }

        private void Update()
        {
            Aim();
            
            if(Input.GetMouseButtonDown(0))
            {
                trickShot.Shoot();
            }
        }
    }
}