using System;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    /// <summary>
    /// Defines the ballistic path of a projectile in 2D
    /// </summary>
    [Serializable]
    public struct BallisticPath2D
    {
        public (Vector2 position, Vector2 velocity, float time)[] steps;
        public float flightDistance;
        public float flightTime;
        public RaycastHit2D? impact;

        public static BallisticPath2D Empty => new BallisticPath2D
        {
            steps = Array.Empty<(Vector2, Vector2, float)>(),
            flightDistance = 0f,
            flightTime = 0f,
            impact = null
        };

        public static BallisticPath2D Get(Vector2 start, Collider2D startCollider, Vector2 velocity, float radius, float resolution, float maxLength, LayerMask collisionLayers)
        {
            if (API.Ballistics.CircleCast(start, startCollider, velocity, Physics2D.gravity, radius, resolution, maxLength, collisionLayers, out var hit, out var path, out var distance))
            {
                return new BallisticPath2D
                {
                    flightDistance = distance,
                    flightTime = path[^1].time,
                    impact = hit,
                    steps = path.ToArray(),
                };
            }
            else
            {
                return new BallisticPath2D
                {
                    flightDistance = distance,
                    flightTime = (path != null && path.Count > 0) ? path[^1].time : 0f,
                    impact = hit,
                    steps = null,
                };
            }
        }

        public readonly (Vector2 position, Vector2 velocity, float time) Lerp(float time)
        {
            if (steps == null || steps.Length == 0)
                return (Vector2.zero, Vector2.zero, 0f);

            if (steps.Length == 1 || time <= 0)
                return steps[0];

            if (time >= flightTime)
                return steps[^1];

            for (int i = 1; i < steps.Length; i++)
            {
                if (steps[i].time > time)
                {
                    var (sPos, sVel, sT) = steps[i - 1];
                    var (ePos, eVel, eT) = steps[i];
                    float dT = eT - sT;
                    float lT = (time - sT) / dT;
                    return (Vector2.Lerp(sPos, ePos, lT), Vector2.Lerp(sVel, eVel, lT), time);
                }
            }

            return steps[^1];
        }
    }
}
