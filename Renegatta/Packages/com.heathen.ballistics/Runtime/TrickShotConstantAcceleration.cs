using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/trick-shot-constant-acceleration")]
    [RequireComponent(typeof(TrickShot))]
    public class TrickShotConstantAcceleration : MonoBehaviour
    {
        [Tooltip("Forces applied in world space, e.g., gravity.")]
        public List<Vector3> globalConstants = new() { new(0, -9.81f, 0) };

        [Tooltip("Forces applied relative to this GameObject's local space.")]
        public List<Vector3> localConstants = new();

        private TrickShot trickShot;

        private void Awake()
        {
            trickShot = GetComponent<TrickShot>();
        }

        private void LateUpdate()
        {
            Vector3 total = Vector3.zero;

            foreach (var v in globalConstants)
                total += v;

            var rotation = trickShot.transform.rotation;
            foreach (var v in localConstants)
                total += rotation * v;

            trickShot.constantAcceleration = total;
        }
    }
}
