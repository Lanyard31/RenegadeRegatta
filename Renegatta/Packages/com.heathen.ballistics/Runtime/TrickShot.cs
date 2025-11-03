using System;
using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/trick-shot")]
    public class TrickShot : MonoBehaviour
    {
        public float speed;
        [Tooltip("Acceleration applied over flight (usually gravity).")]
        public Vector3 constantAcceleration = new Vector3(0, -9.81f, 0);
        public float radius;
        public BallisticPathFollow template;
        public float resolution = 0.01f;
        public float distance = 10f;
        public LayerMask collisionLayers = 0;
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;
        public int bounces = 0;
        [Range(0f, 1f)] public float bounceDamping = 0.5f;
        [Tooltip("Measure distance as total arc length or per bounce.")]
        public bool distanceIsTotalLength = false;

        public List<BallisticPath> prediction = new();

        private Transform selfTransform;

        private void Start()
        {
            selfTransform = transform;
        }

        public void Shoot()
        {
            var go = Instantiate(template.gameObject, selfTransform.position, selfTransform.rotation);
            try
            {
                var comp = go.GetComponent<BallisticPathFollow>();
                comp.projectile = new BallisticsData
                {
                    velocity = selfTransform.forward * speed,
                    radius = radius
                };
                comp.path = prediction != null && prediction.Count > 0
                    ? new List<BallisticPath>(prediction)
                    : new List<BallisticPath>();

                if (comp.path.Count == 0)
                    Debug.LogWarning($"{nameof(Shoot)}: No prediction path available, projectile spawned but may not behave as expected.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }

        public void Predict()
        {
            var projectileSettings = new BallisticsData
            {
                velocity = selfTransform.forward * speed,
                radius = radius
            };

            prediction.Clear();

            // Helper local function for bounce prediction loop
            void PredictBounces(Vector3 startPos, Vector3 startVelocity, float remainingDistance, int bouncesCount, bool useTotalDistance)
            {
                var proj = projectileSettings;
                proj.velocity = startVelocity;

                var pos = startPos;
                float remDistance = remainingDistance;

                for (int i = 0; i <= bouncesCount; i++)
                {
                    var current = proj.Predict(pos, null, resolution, remDistance, collisionLayers, triggerInteraction, constantAcceleration);
                    prediction.Add(current);

                    if (!current.impact.HasValue)
                        break;

                    var (impactPos, impactVel, _) = current.steps[^1];

                    pos = impactPos;
                    proj.velocity = Vector3.Reflect(impactVel, current.impact.Value.normal) * (1f - bounceDamping);

                    if (useTotalDistance)
                        remDistance -= current.flightDistance;
                }
            }

            if (bounces == 0)
            {
                prediction.Add(projectileSettings.Predict(selfTransform.position, null, resolution, distance, collisionLayers, triggerInteraction, constantAcceleration));
            }
            else
            {
                PredictBounces(selfTransform.position, projectileSettings.velocity, distance, bounces, distanceIsTotalLength);
            }
        }
    }
}
