using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/ballistic-path-line-render")]
    [RequireComponent(typeof(LineRenderer))]
    public class BallisticPathLineRender : MonoBehaviour
    {
        public enum GravityMode { None, Physics, Custom }

        [Header("Launch Settings")]
        public Vector3 start;
        public BallisticsData projectile;

        [Header("Behaviour Settings")]
        public bool runOnStart = true;
        public bool continuousRun = false;
        public LayerMask collisionLayers = 0;
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.UseGlobal;
        [Min(0.001f)] public float resolution = 0.1f;
        public float maxLength = 10f;
        public int maxBounces = 0;
        [Range(0f, 1f)] public float bounceDamping = 0.2f;
        public GravityMode gravityMode = GravityMode.Physics;
        public Vector3 customGravity = Vector3.zero;

        private LineRenderer lineRenderer;

        // Reusable lists to reduce GC allocations
        private readonly List<Vector3> trajectory = new List<Vector3>(100);
        private readonly List<RaycastHit> impacts = new List<RaycastHit>();

        // Cached array buffer for LineRenderer.SetPositions
        private Vector3[] positionBuffer = new Vector3[100];

        private void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.useWorldSpace = true;

            if (runOnStart)
                Simulate();
        }

        private void LateUpdate()
        {
            if (continuousRun)
                Simulate();
        }

        public void Simulate()
        {
            if (projectile.Speed <= 0f)
                return;

            Vector3 gravity = gravityMode switch
            {
                GravityMode.Custom => customGravity,
                GravityMode.Physics => Physics.gravity,
                _ => Vector3.zero
            };

            trajectory.Clear();
            impacts.Clear();

            // Initial prediction
            var result = projectile.Predict(start, null, resolution, maxLength, collisionLayers, triggerInteraction, gravity);
            foreach (var step in result.steps)
                trajectory.Add(step.position);

            if (result.impact.HasValue)
            {
                impacts.Add(result.impact.Value);

                if (maxBounces > 0)
                {
                    float remainingLength = maxLength - result.flightDistance;
                    var projCopy = projectile; // local copy to modify safely

                    // Unpack last step's position and velocity
                    var (pos, vel, _) = result.steps[^1];

                    projCopy.velocity = Vector3.Reflect(vel, result.impact.Value.normal) * (1f - bounceDamping);

                    for (int i = 0; i < maxBounces && remainingLength > 0f; i++)
                    {
                        result = projCopy.Predict(pos, result.impact.Value.collider, resolution, remainingLength, collisionLayers, triggerInteraction, gravity);

                        foreach (var step in result.steps)
                            trajectory.Add(step.position);

                        if (result.impact.HasValue)
                        {
                            impacts.Add(result.impact.Value);
                            (pos, vel, _) = result.steps[^1];
                            projCopy.velocity = Vector3.Reflect(vel, result.impact.Value.normal) * (1f - bounceDamping);
                        }
                        else
                        {
                            break;
                        }

                        remainingLength -= result.flightDistance;
                    }
                }
            }

            if (positionBuffer.Length < trajectory.Count)
                positionBuffer = new Vector3[trajectory.Count];

            trajectory.CopyTo(positionBuffer);

            lineRenderer.positionCount = trajectory.Count;
            lineRenderer.SetPositions(positionBuffer);
        }
    }
}
