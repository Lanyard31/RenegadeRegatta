using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/ballistic-path-follow")]
    public class BallisticPathFollow : MonoBehaviour
    {
        [Serializable]
        public class Vector3Event : UnityEvent<float3> { }

        public BallisticsData projectile;
        public List<BallisticPath> path;
        public bool resumeDynamicOnEnd = true;

        [Header("Planned Collisions")]
        [Tooltip("Tests if the bounce surface has moved before impact, if so the path will end")]
        public bool validateBounce = true;
        [Tooltip("Should the system invoke the OnCollisionEnter message for planned impacts")]
        public bool invokeOnCollisionEnter = true;

        [Header("Unplanned Collisions")]
        [Tooltip("If End On Collision is true what layers should collision be tested for.")]
        public LayerMask collisionMask = 1;
        [Tooltip("Should we check for unplanned collisions")]
        public bool endOnCollision = true;

        public Vector3Event endOfPath;

        Transform selfTransform;
        float time;
        bool playing = false;
        int previous = 0;

        Queue<(GameObject target, Collision collision)> impactQueue = new();

        // Cache for OnCollisionEnter method info per GameObject
        private readonly Dictionary<GameObject, List<(MonoBehaviour component, MethodInfo method)>> collisionMethodCache = new();

        private void Start()
        {
            selfTransform = transform;
            playing = true;
            previous = 0;
        }

        private void OnEnable()
        {
            time = Time.time;
        }

        private void LateUpdate()
        {
            if (path == null || path.Count == 0)
                playing = false;

            if (playing)
            {
                var total = Time.time - time;
                if (Lerp(total))
                    playing = false;
            }
        }

        private void FixedUpdate()
        {
            while (impactQueue.Count > 0)
            {
                var impact = impactQueue.Dequeue();
                InjectCollision(impact.target, impact.collision);
            }
        }

        private bool Lerp(float time)
        {
            var pastStepTime = 0f;
            var traveledTime = 0f;

            (Vector3 position, Vector3 velocity, float time) current = default;
            var endPath = true;
            int currentSegment;

            for (currentSegment = 0; currentSegment < path.Count; currentSegment++)
            {
                var p = path[currentSegment];
                traveledTime += p.flightTime;

                if (traveledTime > time)
                {
                    var deltaTime = time - pastStepTime;
                    current = p.Lerp(deltaTime);

                    endPath = false;
                    break;
                }

                pastStepTime = traveledTime;
            }

            if (endPath)
            {
                var cPath = path[^1];
                var cStep = cPath.steps[^1];
                var cancelImpact = false;

                if (cPath.impact.HasValue && invokeOnCollisionEnter)
                    cancelImpact = SimulatePlannedCollision(cPath.impact.Value, cStep.velocity);

                if (cancelImpact)
                    EndOfPath(selfTransform.position, cStep.velocity);
                else
                    EndOfPath(cStep.position, cStep.velocity);

                return endPath;
            }

            if (invokeOnCollisionEnter && previous < currentSegment && path[previous].impact.HasValue)
            {
                endPath = SimulatePlannedCollision(path[previous].impact.Value, path[previous].steps[^1].velocity);
                previous = currentSegment;

                if (endPath)
                {
                    EndOfPath(selfTransform.position, path[previous].steps[^1].velocity);
                    return endPath;
                }
            }

            if (endOnCollision)
            {
                endPath = ConsiderCollision(selfTransform.position, current.velocity, path[currentSegment].impact.HasValue ? path[currentSegment].impact.Value.collider : null);

                if (endPath)
                {
                    EndOfPath(selfTransform.position, current.velocity);
                    return endPath;
                }
            }

            selfTransform.position = current.position;
            return false;
        }

        private bool ConsiderCollision(Vector3 position, Vector3 velocity, Collider segmentImpact)
        {
            if (Physics.SphereCast(position, projectile.radius, velocity.normalized, out var predictHit, velocity.magnitude * Time.fixedDeltaTime * 2, collisionMask)
                && (segmentImpact == null || segmentImpact != predictHit.collider))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool SimulatePlannedCollision(RaycastHit impact, Vector3 velocity)
        {
            if (validateBounce)
            {
                if (!Physics.SphereCast(selfTransform.position, projectile.radius, velocity.normalized, out var predictHit, velocity.magnitude * Time.fixedDeltaTime)
                    || predictHit.collider != impact.collider)
                {
                    return true;
                }
            }

            var invokeTarget = impact.collider.gameObject;
            impactQueue.Enqueue((invokeTarget, null));

            return false;
        }

        private void EndOfPath(Vector3 final, Vector3 velocity)
        {
            selfTransform.position = final;

            if (resumeDynamicOnEnd)
            {
                var body = GetComponentInParent<Rigidbody>();
                body.isKinematic = false;
                body.linearVelocity = velocity;
                body.WakeUp();
            }

            endOfPath.Invoke(velocity);
        }

        private void InjectCollision(GameObject target, Collision collision)
        {
            if (!collisionMethodCache.TryGetValue(target, out var methods))
            {
                methods = new List<(MonoBehaviour, MethodInfo)>();
                foreach (var component in target.GetComponents<MonoBehaviour>())
                {
                    var method = component.GetType().GetMethod("OnCollisionEnter",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, new Type[] { typeof(Collision) }, null);
                    if (method != null)
                    {
                        methods.Add((component, method));
                    }
                }
                collisionMethodCache[target] = methods;
            }

            foreach (var (component, method) in methods)
            {
                method.Invoke(component, new object[] { collision });
            }
        }
    }
}
