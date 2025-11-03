using System;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    /// <summary>
    /// Defines the ballistic path of a projectile.
    /// </summary>
    public struct BallisticPath
    {
        public (Vector3 position, Vector3 velocity, float time)[] steps;
        public float flightDistance;
        public float flightTime;
        public RaycastHit? impact;

        public static BallisticPath Empty => new BallisticPath
        {
            steps = Array.Empty<(Vector3, Vector3, float)>(),
            flightDistance = 0f,
            flightTime = 0f,
            impact = null
        };

        public static BallisticPath Get(Vector3 start, Collider startCollider, Vector3 velocity, float radius, float resolution, float maxLength, LayerMask collisionLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal)
        {
            if (API.Ballistics.SphereCast(start, startCollider, velocity, Physics.gravity, radius, resolution, maxLength, collisionLayers, queryTriggerInteraction, out var hit, out var path, out var distance))
            {
                return new BallisticPath
                {
                    flightDistance = distance,
                    flightTime = path[^1].time,
                    impact = hit,
                    steps = path.ToArray(),
                };
            }
            else
            {
                // path and distance are still defined, but path may be empty; check for safety
                return new BallisticPath
                {
                    flightDistance = distance,
                    flightTime = path != null && path.Count > 0 ? path[^1].time : 0f,
                    impact = hit,
                    steps = null,
                };
            }
        }

        public (Vector3 position, Vector3 velocity, float time) Lerp(float time)
        {
            if (steps == null || steps.Length == 0)
                return (Vector3.zero, Vector3.zero, 0f);

            if (steps.Length == 1 || time <= 0f)
                return steps[0];

            if (time >= flightTime)
                return steps[^1];

            for (int i = 1; i < steps.Length; i++) // start from 1 to safely do i-1
            {
                if (steps[i].time > time)
                {
                    var (sPos, sVel, sT) = steps[i - 1];
                    var (ePos, eVel, eT) = steps[i];
                    float t = (time - sT) / (eT - sT);
                    return (Vector3.Lerp(sPos, ePos, t), Vector3.Lerp(sVel, eVel, t), time);
                }
            }

            return steps[^1];
        }
    }
}
