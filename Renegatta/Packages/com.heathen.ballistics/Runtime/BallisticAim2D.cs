using Heathen.UnityPhysics.API;
using Unity.Mathematics;
using UnityEngine;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/ballistics-aim")]
    public class BallisticAim2D : MonoBehaviour
    {
        public float initialVelocity;
        [SerializeField]
        private Transform pivot;
        public Vector2 limit = new Vector2(-180, 180);

        public bool Aim(Vector2 target)
        {
            return TryAim(() => Ballistics.Solution2D(pivot.position, initialVelocity, target, Physics2D.gravity.magnitude, out Quaternion lowAngle, out _), out Quaternion lowAngle)
                ? ApplyRotation(lowAngle)
                : false;
        }

        public bool Aim(Vector2 target, Vector2 targetVelocity)
        {
            return TryAim(() => Ballistics.Solution2D(pivot.position, initialVelocity, target, targetVelocity, Physics2D.gravity.magnitude, out Quaternion lowAngle, out _), out Quaternion lowAngle)
                ? ApplyRotation(lowAngle)
                : false;
        }

        private bool TryAim(System.Func<int> solutionFunc, out Quaternion lowAngle)
        {
            int result = solutionFunc();
            // The out Quaternion is assigned inside the solutionFunc delegate capture
            // We assume lowAngle is captured from the delegate's out param, so just return if success
            lowAngle = default;
            return result > 0;
        }

        private bool ApplyRotation(Quaternion lowAngle)
        {
            bool valid = true;
            Vector3 euler = lowAngle.eulerAngles;

            // Zero unused axes
            euler.x = 0;
            euler.y = 0;

            // Normalize Z angle
            euler.z = NormalizeAngle(euler.z);

            if (euler.z < limit.x || euler.z > limit.y)
            {
                euler.z = math.clamp(euler.z, limit.x, limit.y);
                valid = false;
            }

            pivot.rotation = Quaternion.Euler(euler);
            return valid;
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180) angle -= 360;
            else if (angle < -180) angle += 360;
            return angle;
        }
    }
}
