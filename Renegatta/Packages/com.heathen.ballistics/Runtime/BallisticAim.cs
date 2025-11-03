using Heathen.UnityPhysics.API;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace Heathen.UnityPhysics
{
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/ballistics-aim")]
    public class BallisticAim : MonoBehaviour
    {
        [FormerlySerializedAs("initialVelocity")]
        public float initialSpeed;
        public Vector3 constantAcceleration = new(0, -9.81f, 0);
        [SerializeField]
        private Transform yPivot;
        [SerializeField]
        private Transform xPivot;
        public Vector2 yLimit = new(-180, 180);
        public Vector2 xLimit = new(-180, 180);

        public bool Aim(Vector3 target)
        {
            if (Ballistics.Solution(yPivot.position, initialSpeed, target, constantAcceleration, out Quaternion lowAngle, out _) <= 0)
                return false;

            return ApplyAimRotation(lowAngle);
        }

        public bool Aim(Vector3 target, Vector3 targetVelocity)
        {
            if (Ballistics.Solution(yPivot.position, initialSpeed, target, targetVelocity, constantAcceleration.magnitude, out Quaternion lowAngle, out _) <= 0)
                return false;

            return ApplyAimRotation(lowAngle);
        }

        private bool ApplyAimRotation(Quaternion lowAngle)
        {
            bool valid = true;

            // Extract yaw (rotation about Y axis) from lowAngle
            Vector3 euler = lowAngle.eulerAngles;
            float yaw = NormalizeAngle(euler.y);
            if (yaw < yLimit.x || yaw > yLimit.y)
            {
                yaw = math.clamp(yaw, yLimit.x, yLimit.y);
                valid = false;
            }
            yPivot.rotation = Quaternion.Euler(0, yaw, 0);

            // Extract pitch (rotation about X axis) from lowAngle *relative* to yaw pivot
            // Since xPivot is child of yPivot, set its local rotation with pitch only.
            float pitch = NormalizeAngle(euler.x);
            if (pitch < xLimit.x || pitch > xLimit.y)
            {
                pitch = math.clamp(pitch, xLimit.x, xLimit.y);
                valid = false;
            }
            xPivot.localRotation = Quaternion.Euler(pitch, 0, 0);

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
