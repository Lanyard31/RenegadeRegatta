using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/trick-shot-constant-acceleration")]
    [RequireComponent(typeof(TrickShot2D))]
    public class TrickShot2DConstantAcceleration : MonoBehaviour
    {
        [Tooltip("Forces applied in world space, e.g., gravity.")]
        public List<Vector2> globalConstants = new() { new(0, -9.81f) };

        [Tooltip("Forces applied relative to this GameObject's local space.")]
        public List<Vector2> localConstants = new();

        private TrickShot2D trickShot;

        private void Awake()
        {
            trickShot = GetComponent<TrickShot2D>();
        }

        private void LateUpdate()
        {
            Vector2 total = Vector2.zero;

            // Add world-space accelerations
            foreach (var v in globalConstants)
                total += v;

            // Add local-space accelerations (transformed to world-space)
            var rotation = trickShot.transform.rotation;
            foreach (var v in localConstants)
                total += (Vector2)(rotation * v);

            trickShot.constantAcceleration = total;
        }
    }
}
