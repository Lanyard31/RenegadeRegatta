using System;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    /// <summary>
    /// Assists in the simulation of ballistics behaviors
    /// </summary>
    [Serializable]
    public struct BallisticsData2D
    {
        public Vector2 velocity;
        public float radius;

        public float Speed
        {
            get => velocity.magnitude;
            set => velocity = velocity.normalized * value;
        }

        public Vector2 Direction
        {
            get => velocity.normalized;
            set => velocity = value * velocity.magnitude;
        }

        public Quaternion Rotation
        {
            get => Quaternion.LookRotation(Vector3.forward, Direction);
            set => velocity = value * Vector2.right * Speed;
        }

        /// <summary>
        /// Modifies velocity to aim at target using low arc solution.
        /// </summary>
        public bool Aim(Vector2 from, Vector2 to)
        {
            if (API.Ballistics.Solution2D(from, Speed, to, Physics.gravity.magnitude, out Quaternion low, out _) > 0)
            {
                Rotation = low;
                return true;
            }
            return false;
        }

        public bool Aim(Vector2 from, Vector2 to, Vector2 constantAcceleration)
        {
            if (API.Ballistics.Solution2D(from, Speed, to, constantAcceleration, out Quaternion low, out _) > 0)
            {
                Rotation = low;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Modifies velocity to aim at target using high arc solution.
        /// </summary>
        public bool AimHigh(Vector2 from, Vector2 to)
        {
            if (API.Ballistics.Solution2D(from, Speed, to, Physics.gravity.magnitude, out _, out Quaternion high) > 0)
            {
                Rotation = high;
                return true;
            }
            return false;
        }

        public bool AimHigh(Vector2 from, Vector2 to, Vector2 constantAcceleration)
        {
            if (API.Ballistics.Solution2D(from, Speed, to, constantAcceleration, out _, out Quaternion high) > 0)
            {
                Rotation = high;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Predicts the projectile path up to first impact or distance limit.
        /// </summary>
        public BallisticPath2D Predict(Vector3 from, Collider2D fromCollider, float resolution, float distanceLimit, LayerMask collisionLayers)
        {
            return Predict(from, fromCollider, resolution, distanceLimit, collisionLayers, Physics.gravity);
        }

        public BallisticPath2D Predict(Vector3 from, Collider2D fromCollider, float resolution, float distanceLimit, LayerMask collisionLayers, Vector2 constantAcceleration)
        {
            if (velocity == Vector2.zero)
                return BallisticPath2D.Empty;

            bool impact = API.Ballistics.CircleCast(from, fromCollider, velocity, constantAcceleration, radius, resolution, distanceLimit, collisionLayers, out var hit, out var path, out var flightDistance);

            return new BallisticPath2D
            {
                steps = path.ToArray(),
                flightDistance = flightDistance,
                flightTime = path.Count > 0 ? path[^1].time : 0f,
                impact = impact ? hit : null
            };
        }
    }
}