using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/ballistic-path-follow")]
    public class BallisticPathFollow2D : MonoBehaviour
    {
        [Serializable]
        public class Vector2Event : UnityEvent<float2> { }

        public BallisticsData2D projectile;
        public List<BallisticPath2D> path;
        public bool resumeDynamicOnEnd = true;

        [Header("Planned Collisions")]
        public bool validateBounce = true;
        public bool invokeOnCollisionEnter2D = true;
        public bool simulateCollision2D = false;

        [Header("Unplanned Collisions")]
        public LayerMask collisionMask = 1;
        public bool endOnCollision = true;

        public Vector2Event endOfPath;

        Transform selfTransform;
        float startTime;
        bool playing;
        int previousSegment = -1;

        Queue<(GameObject target, Collision2D collision)> impactQueue = new();

        void Start()
        {
            selfTransform = transform;
            playing = true;
            previousSegment = 0;
            startTime = Time.time;
        }

        void OnEnable() => startTime = Time.time;

        void LateUpdate()
        {
            if (path == null || path.Count == 0)
            {
                playing = false;
                return;
            }

            if (!playing) return;

            float elapsed = Time.time - startTime;
            if (Lerp(elapsed))
                playing = false;
        }

        void FixedUpdate()
        {
            while (impactQueue.Count > 0)
            {
                var (target, collision) = impactQueue.Dequeue();
                InjectCollision(target, collision);
            }
        }

        bool Lerp(float time)
        {
            float pastTime = 0f;
            float accumulatedTime = 0f;
            bool endPath = true;
            int currentSegment = 0;
            (Vector2 position, Vector2 velocity, float time) currentStep = default;

            for (; currentSegment < path.Count; currentSegment++)
            {
                var segment = path[currentSegment];
                accumulatedTime += segment.flightTime;

                if (accumulatedTime > time)
                {
                    float segmentTime = time - pastTime;
                    currentStep = segment.Lerp(segmentTime);
                    endPath = false;
                    break;
                }

                pastTime = accumulatedTime;
            }

            if (endPath)
            {
                var lastPath = path[^1];
                var lastStep = lastPath.steps[^1];

                bool cancelImpact = false;

                if (lastPath.impact.HasValue && invokeOnCollisionEnter2D)
                    cancelImpact = SimulatePlannedCollision(lastPath.impact.Value, lastStep.velocity);

                if (cancelImpact)
                    EndOfPath(selfTransform.position, lastStep.velocity);
                else
                    EndOfPath(lastStep.position, lastStep.velocity);

                return true;
            }

            if (invokeOnCollisionEnter2D && previousSegment < currentSegment && path[previousSegment].impact.HasValue)
            {
                endPath = SimulatePlannedCollision(path[previousSegment].impact.Value, path[previousSegment].steps[^1].velocity);
                previousSegment = currentSegment;

                if (endPath)
                {
                    EndOfPath(selfTransform.position, path[previousSegment].steps[^1].velocity);
                    return true;
                }
            }

            if (endOnCollision)
            {
                endPath = ConsiderCollision(selfTransform.position, currentStep.velocity, path[currentSegment].impact?.collider);

                if (endPath)
                {
                    EndOfPath(selfTransform.position, currentStep.velocity);
                    return true;
                }
            }

            selfTransform.position = currentStep.position;
            return false;
        }

        bool ConsiderCollision(Vector2 position, Vector2 velocity, Collider2D plannedCollider)
        {
            var predictedHit = Physics2D.CircleCast(position, projectile.radius, velocity.normalized,
                velocity.magnitude * Time.fixedDeltaTime * 2, collisionMask);

            return predictedHit.collider != null && plannedCollider != predictedHit.collider;
        }

        bool SimulatePlannedCollision(RaycastHit2D impact, Vector2 velocity)
        {
            if (validateBounce)
            {
                var predictedHit = Physics2D.CircleCast(selfTransform.position, projectile.radius, velocity.normalized,
                    velocity.magnitude * Time.fixedDeltaTime);

                if (predictedHit.collider != impact.collider)
                    return true; // Cancel path
            }

            try
            {
                var targetGO = impact.collider.gameObject;

                if (simulateCollision2D)
                {
                    var collisionData = CreateFakeCollision(impact, velocity);
                    impactQueue.Enqueue((targetGO, collisionData));
                }
                else
                {
                    impactQueue.Enqueue((targetGO, null));
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            return false;
        }

        Collision2D CreateFakeCollision(RaycastHit2D impact, Vector2 velocity)
        {
            // Note: Creating Collision2D instances via reflection is fragile and not officially supported.
            var collisionData = (Collision2D)Activator.CreateInstance(typeof(Collision2D));

            var myCollider = GetComponentInParent<Collider2D>();
            var myRigidbody = GetComponentInParent<Rigidbody2D>();

            if (myCollider != null)
            {
                var colliderField = typeof(Collision2D).GetField("m_Collider", BindingFlags.Instance | BindingFlags.NonPublic);
                colliderField?.SetValue(collisionData, myCollider.GetInstanceID());
            }

            if (myRigidbody != null)
            {
                var rigidbodyField = typeof(Collision2D).GetField("m_Rigidbody", BindingFlags.Instance | BindingFlags.NonPublic);
                rigidbodyField?.SetValue(collisionData, myRigidbody.GetInstanceID());
            }

            var contactsField = typeof(Collision2D).GetField("m_ReusedContacts", BindingFlags.Instance | BindingFlags.NonPublic);

            var contact = (ContactPoint2D)Activator.CreateInstance(typeof(ContactPoint2D));
            typeof(ContactPoint2D).GetField("m_Point", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(contact, impact.point);
            typeof(ContactPoint2D).GetField("m_Normal", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(contact, impact.normal);
            typeof(ContactPoint2D).GetField("m_RelativeVelocity", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(contact, velocity);

            contactsField?.SetValue(collisionData, new[] { contact });

            typeof(Collision2D).GetField("m_RelativeVelocity", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(collisionData, velocity);
            typeof(Collision2D).GetField("m_ContactCount", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(collisionData, 1);

            return collisionData;
        }

        void EndOfPath(Vector2 finalPos, Vector2 velocity)
        {
            selfTransform.position = finalPos;

            if (resumeDynamicOnEnd)
            {
                var body = GetComponentInParent<Rigidbody2D>();
                if (body != null)
                {
                    body.bodyType = RigidbodyType2D.Dynamic;
                    body.linearVelocity = velocity;
                    body.WakeUp();
                }
            }

            endOfPath?.Invoke(velocity);
        }

        void InjectCollision(GameObject target, Collision2D collision)
        {
            foreach (var component in target.GetComponents<MonoBehaviour>())
            {
                var method = component.GetType().GetMethod("OnCollisionEnter2D",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    method.Invoke(component, new object[] { collision });
            }
        }
    }
}
