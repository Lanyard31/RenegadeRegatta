using System;
using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    /// <summary>
    /// Assists in the simulation of ballistics behaviors
    /// </summary>
    [Serializable]
    public struct BallisticsData
    {
        public Vector3 velocity;
        public float radius;

        public float Speed
        {
            get => velocity.magnitude;
            set => velocity = velocity.normalized * value;
        }
        public Vector3 Direction
        {
            get => velocity.normalized;
            set => velocity = value * velocity.magnitude;
        }
        public Quaternion Rotation
        {
            get => Quaternion.LookRotation(Direction);
            set => velocity = value * Vector3.forward * Speed;
        }

        private bool TryGetSolution(Vector3 from, Vector3 to, Vector3 acceleration, out Quaternion low, out Quaternion high)
        {
            return API.Ballistics.Solution(from, Speed, to, acceleration, out low, out high) > 0;
        }

        public bool Aim(Vector3 from, Vector3 to)
            => TryGetSolution(from, to, Physics.gravity, out var low, out _) && SetRotation(low);

        public bool Aim(Vector3 from, Vector3 to, Vector3 constantAcceleration)
            => TryGetSolution(from, to, constantAcceleration, out var low, out _) && SetRotation(low);

        public bool AimHigh(Vector3 from, Vector3 to)
            => TryGetSolution(from, to, Physics.gravity, out _, out var high) && SetRotation(high);

        public bool AimHigh(Vector3 from, Vector3 to, Vector3 constantAcceleration)
            => TryGetSolution(from, to, constantAcceleration, out _, out var high) && SetRotation(high);

        private bool SetRotation(Quaternion rotation)
        {
            Rotation = rotation;
            return true;
        }

        public BallisticPath Predict(Vector3 from, Collider fromCollider, float resolution, float distanceLimit, LayerMask collisionLayers, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.UseGlobal, Vector3? constantAcceleration = null)
        {
            if (velocity == Vector3.zero)
                return BallisticPath.Empty;

            var acceleration = constantAcceleration ?? Physics.gravity;
            bool impact = API.Ballistics.SphereCast(from, fromCollider, velocity, acceleration, radius, resolution, distanceLimit, collisionLayers, queryTriggerInteraction, out var hit, out var path, out var flightDistance);

            return new BallisticPath
            {
                steps = path.ToArray(),
                flightDistance = flightDistance,
                flightTime = path[^1].time,
                impact = impact ? hit : default
            };
        }
    }
}