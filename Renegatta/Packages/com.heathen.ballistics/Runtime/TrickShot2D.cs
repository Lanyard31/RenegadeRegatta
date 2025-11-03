using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/trick-shot")]
    public class TrickShot2D : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [Min(0)] public float speed = 10f;
        [Min(0)] public float radius = 0.5f;
        public Vector2 constantAcceleration = new(0, -9.81f);

        [Header("Prediction Settings")]
        [Min(0.0001f)] public float resolution = 0.01f;
        [Min(0)] public float distance = 10f;
        [Min(0)] public int bounces = 0;
        [Range(0, 1)] public float bounceDamping = 0.5f;
        public bool distanceIsTotalLength = false;

        [Header("Prediction Output")]
        public List<BallisticPath2D> prediction = new();

        [Header("References")]
        public BallisticPathFollow2D template;
        public LayerMask collisionLayers = 0;

        private Transform selfTransform;

        private void Start()
        {
            selfTransform = transform;
            resolution = math.max(resolution, 0.0001f);
        }

        [ContextMenu("Shoot")]
        public void Shoot()
        {
            var GO = Instantiate(template.gameObject);
            var comp = GO.GetComponent<BallisticPathFollow2D>();
            comp.projectile = new BallisticsData2D
            {
                velocity = selfTransform.up * speed,
                radius = radius
            };
            comp.path = new List<BallisticPath2D>(prediction);
            GO.transform.SetPositionAndRotation(selfTransform.position, selfTransform.rotation);
        }

        [ContextMenu("Predict Path")]
        public void Predict()
        {
            if (selfTransform == null)
                selfTransform = transform;

            prediction.Clear();

            var projectile = new BallisticsData2D
            {
                velocity = selfTransform.up * speed,
                radius = radius
            };

            var current = projectile.Predict(selfTransform.position, null, resolution, distance, collisionLayers, constantAcceleration);
            prediction.Add(current);

            if (current.impact.HasValue && bounces > 0)
            {
                float remainingDistance = distanceIsTotalLength ? distance - current.flightDistance : distance;
                SimulateBounces(projectile, current, remainingDistance);
            }
        }

        private void SimulateBounces(BallisticsData2D projectile, BallisticPath2D previous, float remainingDistance)
        {
            var proj = projectile;
            var (position, velocity, time) = previous.steps[^1];
            proj.velocity = ReflectAndDampen(velocity, previous.impact.Value.normal);

            for (int i = 0; i < bounces; i++)
            {
                var current = proj.Predict(position, previous.impact.Value.collider, resolution, remainingDistance, collisionLayers, constantAcceleration);
                prediction.Add(current);

                if (!current.impact.HasValue)
                    break;

                (position, velocity, time) = current.steps[^1];
                proj.velocity = ReflectAndDampen(velocity, current.impact.Value.normal);

                if (distanceIsTotalLength)
                    remainingDistance -= current.flightDistance;

                previous = current;
            }
        }

        private Vector2 ReflectAndDampen(Vector2 velocity, Vector2 normal)
        {
            return Vector2.Reflect(velocity, normal) * (1f - bounceDamping);
        }
    }
}
