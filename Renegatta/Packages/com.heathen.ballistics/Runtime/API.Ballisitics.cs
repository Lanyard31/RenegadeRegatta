using System;
using System.Collections.Generic;
using UnityEngine;

namespace Heathen.UnityPhysics.API
{
    /// <summary>
    /// Interface for dealing with ballistic problems such as resolving a firing solution accounting for the parabolic trajectory of a projectile
    /// </summary>
    [HelpURL("https://kb.heathen.group/unity/physics/ballistics/api.ballistics")]
    public static class Ballistics
    {
        /// <summary>
        /// Find the max range of a ballistic projectile
        /// </summary>
        /// <param name="speed">projectile speed</param>
        /// <param name="gravity">gravity magnitude</param>
        /// <param name="height">Height above</param>
        /// <returns></returns>
        public static float MaxRange(float speed, float gravity, float height)
        {
            if (speed <= 0f || gravity <= 0f || height < 0f)
                return 0f;

            // 45 degrees in radians - optimal launch angle for max range without air resistance.
            const float angle = Mathf.PI / 4f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);

            // Formula derived from projectile motion equations
            // Range = (v*cos(g) / g) * (v*sin(g) + sqrt(v^2 * sin^2(g) + 2 * g * h))
            float term1 = speed * cos / gravity;
            float term2 = speed * sin;
            float term3 = Mathf.Sqrt(speed * speed * sin * sin + 2f * gravity * height);

            return term1 * (term2 + term3);
        }
        /// <summary>
        /// Calculates the flight time for a projectile from start to end positions given initial velocity and constant acceleration.
        /// Returns NaN if no valid flight time is found.
        /// </summary>
        public static float FlightTime(Vector3 start, Vector3 end, Vector3 velocity, Vector3 constantAcceleration, float tolerance = 0.01f)
        {
            // Quadratic solver for at^2 + bt + c = 0
            static float[] SolveQuadratic(float a, float b, float c)
            {
                if (Mathf.Abs(a) < Mathf.Epsilon) // Linear case
                {
                    if (Mathf.Abs(b) < Mathf.Epsilon) return Array.Empty<float>();
                    return new float[] { -c / b };
                }
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0) return Array.Empty<float>();
                float sqrtD = Mathf.Sqrt(discriminant);
                return new float[]
                {
            (-b + sqrtD) / (2f * a),
            (-b - sqrtD) / (2f * a)
                };
            }

            // Solve for times per axis
            // Equation: 0.5 * a * t^2 + v * t + (s - e) = 0
            var timesX = SolveQuadratic(0.5f * constantAcceleration.x, velocity.x, start.x - end.x);
            var timesY = SolveQuadratic(0.5f * constantAcceleration.y, velocity.y, start.y - end.y);
            var timesZ = SolveQuadratic(0.5f * constantAcceleration.z, velocity.z, start.z - end.z);

            // Filter positive times and sort ascending
            timesX = Array.FindAll(timesX, t => t > 0);
            timesY = Array.FindAll(timesY, t => t > 0);
            timesZ = Array.FindAll(timesZ, t => t > 0);

            Array.Sort(timesX);
            Array.Sort(timesY);
            Array.Sort(timesZ);

            if (timesX.Length == 0 || timesY.Length == 0 || timesZ.Length == 0)
                return float.NaN; // No valid solution on one axis

            // Find common time that matches all axes within tolerance
            foreach (var tx in timesX)
            {
                foreach (var ty in timesY)
                {
                    if (Mathf.Abs(tx - ty) > tolerance) continue;
                    foreach (var tz in timesZ)
                    {
                        if (Mathf.Abs(tx - tz) <= tolerance)
                        {
                            return tx; // Found consistent flight time
                        }
                    }
                }
            }

            return float.NaN; // No consistent time found
        }
        /// <summary>
        /// Calculates the flight time for a projectile from start to end positions given initial velocity and constant acceleration.
        /// Returns NaN if no valid flight time is found.
        /// </summary>
        public static float FlightTime2D(Vector2 start, Vector2 end, Vector2 velocity, Vector2 constantAcceleration, float tolerance = 0.01f)
        {
            static float[] SolveQuadratic(float a, float b, float c)
            {
                if (Mathf.Abs(a) < Mathf.Epsilon)
                {
                    if (Mathf.Abs(b) < Mathf.Epsilon) return Array.Empty<float>();
                    return new float[] { -c / b };
                }
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f) return Array.Empty<float>();
                float sqrtD = Mathf.Sqrt(discriminant);
                return new float[]
                {
            (-b + sqrtD) / (2f * a),
            (-b - sqrtD) / (2f * a)
                };
            }

            var dx = start.x - end.x;
            var dy = start.y - end.y;

            var timesX = SolveQuadratic(0.5f * constantAcceleration.x, velocity.x, dx);
            var timesY = SolveQuadratic(0.5f * constantAcceleration.y, velocity.y, dy);

            timesX = Array.FindAll(timesX, t => t > 0);
            timesY = Array.FindAll(timesY, t => t > 0);

            Array.Sort(timesX);
            Array.Sort(timesY);

            if (timesX.Length == 0 || timesY.Length == 0)
                return float.NaN;

            foreach (var tx in timesX)
            {
                foreach (var ty in timesY)
                {
                    if (Mathf.Abs(tx - ty) <= tolerance)
                        return tx;
                }
            }

            return float.NaN;
        }

        /// <summary>
        /// Finds the final velocity of a projectile given constant acceleration.
        /// </summary>
        /// <param name="initialVelocity">Initial velocity of the projectile</param>
        /// <param name="constantAcceleration">Constant acceleration vector (e.g., gravity, drag, etc.)</param>
        /// <param name="flightTime">Time the projectile is in flight</param>
        /// <returns>The final velocity vector</returns>
        public static Vector3 FinalVelocity(Vector3 initialVelocity, Vector3 constantAcceleration, float flightTime)
        {
            return initialVelocity + constantAcceleration * flightTime;
        }
        /// <summary>
        /// Finds the final velocity of a projectile in 2D given constant acceleration.
        /// </summary>
        /// <param name="initialVelocity">Initial velocity of the projectile</param>
        /// <param name="constantAcceleration">Constant acceleration vector</param>
        /// <param name="flightTime">Time the projectile is in flight</param>
        /// <returns>The final velocity vector</returns>
        public static Vector2 FinalVelocity2D(Vector2 initialVelocity, Vector2 constantAcceleration, float flightTime)
        {
            return initialVelocity + constantAcceleration * flightTime;
        }

        /// <summary>
        /// Computes the low and high angle rotations needed to hit a target with a projectile at a given speed,
        /// accounting for a directional constant acceleration (e.g., gravity, Magnus effect).
        /// </summary>
        /// <param name="projectile">Point of projectile release (e.g., muzzle, hand)</param>
        /// <param name="speed">Initial scalar speed (magnitude of velocity)</param>
        /// <param name="target">Target position to hit</param>
        /// <param name="constantAcceleration">Constant acceleration vector (e.g., gravity, wind, Magnus)</param>
        /// <param name="lowAngle">Output: lower arc rotation solution</param>
        /// <param name="highAngle">Output: higher arc rotation solution</param>
        /// <returns>The number of valid trajectory solutions (0, 1, or 2)</returns>
        public static int Solution(Vector3 projectile, float speed, Vector3 target, Vector3 constantAcceleration, out Quaternion lowAngle, out Quaternion highAngle)
        {
            lowAngle = Quaternion.identity;
            highAngle = Quaternion.identity;

            if (speed <= 0f || constantAcceleration == Vector3.zero)
                return 0;

            // Rotate so constant acceleration points 'down'
            Quaternion alignToDown = Quaternion.FromToRotation(constantAcceleration.normalized, Vector3.down);

            Vector3 projLocal = alignToDown * projectile;
            Vector3 targetLocal = alignToDown * target;

            float accelMagnitude = constantAcceleration.magnitude;

            int solutionCount = Solution(projLocal, speed, targetLocal, accelMagnitude, out lowAngle, out highAngle);

            // Rotate solutions back to world space
            Quaternion restore = Quaternion.Inverse(alignToDown);
            lowAngle = restore * lowAngle;
            highAngle = restore * highAngle;

            return solutionCount;
        }

        /// <summary>
        /// Computes low and high angle launch rotations in 2D to hit a target with a projectile, 
        /// given a scalar speed and constant acceleration (e.g. gravity or Magnus effects).
        /// </summary>
        /// <param name="projectile">2D point of release (e.g., muzzle or hand)</param>
        /// <param name="speed">Initial scalar speed (magnitude of velocity)</param>
        /// <param name="target">2D target position</param>
        /// <param name="constantAcceleration">2D constant acceleration vector</param>
        /// <param name="lowAngle">Output: lower arc rotation (as a Quaternion)</param>
        /// <param name="highAngle">Output: higher arc rotation (as a Quaternion)</param>
        /// <returns>Number of valid solutions (0, 1, or 2)</returns>
        public static int Solution2D(Vector2 projectile, float speed, Vector2 target, Vector2 constantAcceleration, out Quaternion lowAngle, out Quaternion highAngle)
        {
            lowAngle = Quaternion.identity;
            highAngle = Quaternion.identity;

            if (speed <= 0f || constantAcceleration == Vector2.zero)
                return 0;

            // Convert 2D acceleration to 3D for alignment
            Vector3 accel3D = new Vector3(constantAcceleration.x, constantAcceleration.y, 0f);
            Quaternion alignToDown = Quaternion.FromToRotation(accel3D.normalized, Vector3.down);

            // Apply rotation in 3D space then project back to 2D
            Vector2 projLocal = (Vector2)(alignToDown * new Vector3(projectile.x, projectile.y, 0f));
            Vector2 targetLocal = (Vector2)(alignToDown * new Vector3(target.x, target.y, 0f));

            float accelMag = constantAcceleration.magnitude;

            int solutionCount = Solution2D(projLocal, speed, targetLocal, accelMag, out lowAngle, out highAngle);

            // Reorient solution angles to match original acceleration direction
            Quaternion restore = Quaternion.Inverse(alignToDown);
            lowAngle = restore * lowAngle;
            highAngle = restore * highAngle;

            return solutionCount;
        }

        /// <summary>
        /// Solves for two possible rotations to hit a fixed target from a fixed position.
        /// </summary>
        /// <remarks>
        /// Derived from:
        /// https://github.com/forrestthewoods/lib_fts/blob/master/code/fts_ballistic_trajectory.cs
        /// </remarks>
        /// <param name="projectile"></param>
        /// <param name="speed"></param>
        /// <param name="target"></param>
        /// <param name="gravity"></param>
        /// <param name="lowAngle"></param>
        /// <param name="highAngle"></param>
        /// <returns></returns>
        public static int Solution(Vector3 projectile, float speed, Vector3 target, float gravity, out Quaternion lowAngle, out Quaternion highAngle)
        {
            lowAngle = Quaternion.identity;
            highAngle = Quaternion.identity;

            if (speed <= 0f || gravity <= 0f)
                return 0;

            Vector3 heading = target - projectile;
            float y = heading.y;
            Vector2 planarXZ = new Vector2(heading.x, heading.z);
            float x = planarXZ.magnitude;

            float speed2 = speed * speed;
            float speed4 = speed2 * speed2;
            float gravX = gravity * x;

            float discriminant = speed4 - gravity * (gravity * x * x + 2f * y * speed2);
            if (discriminant < 0f)
                return 0;

            float sqrtDisc = Mathf.Sqrt(discriminant);
            float lowTheta = Mathf.Atan2(speed2 - sqrtDisc, gravX);
            float highTheta = Mathf.Atan2(speed2 + sqrtDisc, gravX);
            int solutions = Mathf.Approximately(lowTheta, highTheta) ? 1 : 2;

            Vector3 planarDir = new Vector3(planarXZ.x, 0f, planarXZ.y).normalized;

            Quaternion ComputeAngle(float theta) =>
                Quaternion.LookRotation(planarDir * Mathf.Cos(theta) * speed + Vector3.up * Mathf.Sin(theta) * speed);

            lowAngle = ComputeAngle(lowTheta);
            highAngle = solutions == 2 ? ComputeAngle(highTheta) : lowAngle;

            return solutions;
        }

        /// <summary>
        /// Solves for two possible rotations to hit a fixed target from a fixed position.
        /// </summary>
        /// <remarks>
        /// Derived from:
        /// https://github.com/forrestthewoods/lib_fts/blob/master/code/fts_ballistic_trajectory.cs
        /// </remarks>
        /// <param name="projectile"></param>
        /// <param name="speed"></param>
        /// <param name="target"></param>
        /// <param name="gravity"></param>
        /// <param name="lowAngle"></param>
        /// <param name="highAngle"></param>
        /// <returns></returns>
        public static int Solution2D(Vector2 projectile, float speed, Vector2 target, float gravity, out Quaternion lowAngle, out Quaternion highAngle)
        {
            lowAngle = Quaternion.identity;
            highAngle = Quaternion.identity;

            if (speed <= 0 || gravity <= 0)
                return 0;

            var heading = target - projectile;
            float x = Mathf.Abs(heading.x); // horizontal distance
            float y = heading.y;

            float speed2 = speed * speed;
            float speed4 = speed2 * speed2;
            float gravX = gravity * x;

            float root = speed4 - gravity * (gravity * x * x + 2 * y * speed2);

            if (root < 0)
                return 0;

            root = Mathf.Sqrt(root);

            float lowAng = Mathf.Atan2(speed2 - root, gravX);
            float highAng = Mathf.Atan2(speed2 + root, gravX);
            int sols = lowAng == highAng ? 1 : 2;

            // Direction in 2D plane is always to the right (x positive), so planarDir is (1,0)
            Vector2 planarDir = new Vector2(Mathf.Sign(heading.x), 0);

            Vector2 sDir = planarDir * Mathf.Cos(lowAng) * speed + Vector2.up * Mathf.Sin(lowAng) * speed;
            lowAngle = Quaternion.LookRotation(Vector3.forward, sDir);

            if (sols == 2)
            {
                sDir = planarDir * Mathf.Cos(highAng) * speed + Vector2.up * Mathf.Sin(highAng) * speed;
                highAngle = Quaternion.LookRotation(Vector3.forward, sDir);
            }
            else
            {
                highAngle = lowAngle;
            }

            return sols;
        }

        /// <summary>
        /// Solves for two possible rotations to hit a moving target from a fixed position
        /// </summary>
        /// <remarks>
        /// Derived from:
        /// https://github.com/forrestthewoods/lib_fts/blob/master/code/fts_ballistic_trajectory.cs
        /// </remarks>
        /// <param name="projectile"></param>
        /// <param name="speed"></param>
        /// <param name="target"></param>
        /// <param name="targetVelocity"></param>
        /// <param name="gravity"></param>
        /// <param name="lowAngle"></param>
        /// <param name="highAngle"></param>
        /// <returns></returns>
        public static int Solution(Vector3 projectile, float speed, Vector3 target, Vector3 targetVelocity, float gravity, out Quaternion lowAngle, out Quaternion highAngle)
        {
            lowAngle = Quaternion.identity;
            highAngle = Quaternion.identity;

            if (speed <= 0 || gravity <= 0)
                return 0;

            double g = gravity;
            double halfG = -0.5 * g;

            double dx = target.x - projectile.x;
            double dy = target.y - projectile.y;
            double dz = target.z - projectile.z;

            double vx = targetVelocity.x;
            double vy = targetVelocity.y;
            double vz = targetVelocity.z;

            double s = speed;

            // Quartic coefficients for time to intercept
            double c0 = halfG * halfG;
            double c1 = -2 * vy * halfG;
            double c2 = vy * vy - 2 * dy * halfG - s * s + vx * vx + vz * vz;
            double c3 = 2 * (dy * vy + dx * vx + dz * vz);
            double c4 = dx * dx + dy * dy + dz * dz;

            double[] times = new double[4];
            int n = SolveQuartic(c0, c1, c2, c3, c4, out times[0], out times[1], out times[2], out times[3]);
            if (n == 0)
                return 0;

            System.Array.Sort(times);

            Vector3[] solutions = new Vector3[2];
            int count = 0;

            for (int i = 0; i < n && count < 2; i++)
            {
                double t = times[i];
                if (t <= 0 || double.IsNaN(t))
                    continue;

                // Compute velocity vector to intercept target at time t
                solutions[count] = new Vector3(
                    (float)((dx + vx * t) / t),
                    (float)((dy + vy * t - halfG * t * t) / t),
                    (float)((dz + vz * t) / t)
                );
                count++;
            }

            if (count > 0)
            {
                lowAngle = Quaternion.LookRotation(solutions[0]);
                highAngle = (count > 1) ? Quaternion.LookRotation(solutions[1]) : lowAngle;
            }

            return count;
        }

        /// <summary>
        /// Solves for two possible rotations to hit a moving target from a fixed position
        /// </summary>
        /// <remarks>
        /// Derived from:
        /// https://github.com/forrestthewoods/lib_fts/blob/master/code/fts_ballistic_trajectory.cs
        /// </remarks>
        /// <param name="projectile"></param>
        /// <param name="speed"></param>
        /// <param name="target"></param>
        /// <param name="targetVelocity"></param>
        /// <param name="gravity"></param>
        /// <param name="lowAngle"></param>
        /// <param name="highAngle"></param>
        /// <returns></returns>
        public static int Solution2D(Vector2 projectile, float speed, Vector2 target, Vector2 targetVelocity, float gravity, out Quaternion lowAngle, out Quaternion highAngle)
        {
            lowAngle = Quaternion.identity;
            highAngle = Quaternion.identity;

            if (speed <= 0 || gravity <= 0)
                return 0;

            double g = gravity;
            double halfG = -0.5 * g;

            double dx = target.x - projectile.x;
            double dy = target.y - projectile.y;

            double vx = targetVelocity.x;
            double vy = targetVelocity.y;

            double s = speed;

            // Quartic coefficients for time to intercept
            double c0 = halfG * halfG;
            double c1 = -2 * vy * halfG;
            double c2 = vy * vy - 2 * dy * halfG - s * s + vx * vx;
            double c3 = 2 * (dy * vy + dx * vx);
            double c4 = dx * dx + dy * dy;

            double[] times = new double[4];
            int n = SolveQuartic(c0, c1, c2, c3, c4, out times[0], out times[1], out times[2], out times[3]);
            if (n == 0)
                return 0;

            System.Array.Sort(times);

            Vector2[] solutions = new Vector2[2];
            int count = 0;

            for (int i = 0; i < n && count < 2; i++)
            {
                double t = times[i];
                if (t <= 0 || double.IsNaN(t))
                    continue;

                solutions[count] = new Vector2(
                    (float)((dx + vx * t) / t),
                    (float)((dy + vy * t - halfG * t * t) / t)
                );
                count++;
            }

            if (count > 0)
            {
                lowAngle = Quaternion.LookRotation(Vector3.forward, new Vector3(solutions[0].x, solutions[0].y, 0));
                highAngle = (count > 1) ? Quaternion.LookRotation(Vector3.forward, new Vector3(solutions[1].x, solutions[1].y, 0)) : lowAngle;
            }

            return count;
        }

        /// <summary>
        /// Calculates the firing velocity and gravity needed for a projectile
        /// to hit a target at a specified maximum height (arc ceiling).
        /// </summary>
        /// <param name="projectile">Starting position.</param>
        /// <param name="linearSpeed">Speed along the horizontal plane (XZ).</param>
        /// <param name="target">Target position.</param>
        /// <param name="arcCeiling">Desired maximum height along the trajectory.</param>
        /// <param name="firingVelocity">Output initial velocity vector.</param>
        /// <param name="gravity">Output gravity value (negative).</param>
        /// <returns>True if solution exists; otherwise false.</returns>
        public static bool Solution(Vector3 projectile, float linearSpeed, Vector3 target, float arcCeiling, out Vector3 firingVelocity, out float gravity)
        {
            firingVelocity = Vector3.zero;
            gravity = 0f;

            if (projectile == target || linearSpeed <= 0 || arcCeiling <= projectile.y)
                return false;

            Vector3 diff = target - projectile;
            Vector3 planar = new Vector3(diff.x, 0f, diff.z);
            float horizontalDistance = planar.magnitude;
            if (horizontalDistance == 0)
                return false;

            float time = horizontalDistance / linearSpeed;
            firingVelocity = planar.normalized * linearSpeed;

            float y0 = projectile.y;
            float yPeak = arcCeiling;
            float yEnd = target.y;

            gravity = -4f * (y0 - 2f * yPeak + yEnd) / (time * time);
            firingVelocity.y = -(3f * y0 - 4f * yPeak + yEnd) / time;

            return true;
        }

        /// <summary>
        /// Solves for the required velocity and gravity to hit the target at a prescribed max height in 2D.
        /// </summary>
        /// <param name="projectile">Starting position.</param>
        /// <param name="linearSpeed">Speed along the horizontal (X) axis.</param>
        /// <param name="target">Target position.</param>
        /// <param name="arcCeiling">Desired max height.</param>
        /// <param name="firingVelocity">Output initial velocity vector.</param>
        /// <param name="gravity">Output gravity value (negative).</param>
        /// <returns>True if a solution exists; otherwise false.</returns>
        public static bool Solution2D(Vector2 projectile, float linearSpeed, Vector2 target, float arcCeiling, out Vector2 firingVelocity, out float gravity)
        {
            firingVelocity = Vector2.zero;
            gravity = 0f;

            if (projectile == target || linearSpeed <= 0 || arcCeiling <= projectile.y)
                return false;

            Vector2 diff = target - projectile;
            float horizontalDistance = Mathf.Abs(diff.x);
            if (horizontalDistance == 0)
                return false;

            float time = horizontalDistance / linearSpeed;

            firingVelocity.x = Mathf.Sign(diff.x) * linearSpeed;

            float y0 = projectile.y;
            float yPeak = arcCeiling;
            float yEnd = target.y;

            gravity = -4f * (y0 - 2f * yPeak + yEnd) / (time * time);
            firingVelocity.y = -(3f * y0 - 4f * yPeak + yEnd) / time;

            return true;
        }

        /// <summary>
        /// Solves for velocity and gravity to hit a moving target at a specified max height.
        /// </summary>
        /// <param name="projectile">Start position.</param>
        /// <param name="linearSpeed">Speed on horizontal plane.</param>
        /// <param name="target">Initial target position.</param>
        /// <param name="targetVelocity">Target velocity vector.</param>
        /// <param name="arcCeiling">Desired max height above max(startY, targetY).</param>
        /// <param name="firingVelocity">Output firing velocity.</param>
        /// <param name="gravity">Output gravity (negative value).</param>
        /// <param name="impactPoint">Computed impact point considering target movement.</param>
        /// <returns>True if solution found, false otherwise.</returns>
        public static bool Solution(Vector3 projectile, float linearSpeed, Vector3 target, Vector3 targetVelocity, float arcCeiling, out Vector3 firingVelocity, out float gravity, out Vector3 impactPoint)
        {
            firingVelocity = Vector3.zero;
            gravity = 0f;
            impactPoint = Vector3.zero;

            if (projectile == target || linearSpeed <= 0f)
                return false;

            // Project target velocity and displacement onto horizontal plane (XZ)
            Vector3 targetVelXZ = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            Vector3 diffXZ = target - projectile;
            diffXZ.y = 0f;

            // Quadratic coefficients for time to impact on XZ plane
            float c0 = Vector3.Dot(targetVelXZ, targetVelXZ) - linearSpeed * linearSpeed;
            float c1 = 2f * Vector3.Dot(diffXZ, targetVelXZ);
            float c2 = Vector3.Dot(diffXZ, diffXZ);

            double t0, t1;
            int roots = SolveQuadric(c0, c1, c2, out t0, out t1);

            // Select smallest positive time
            bool validT0 = roots > 0 && t0 > 0;
            bool validT1 = roots > 1 && t1 > 0;
            if (!validT0 && !validT1)
                return false;

            float t = validT0 && validT1 ? Mathf.Min((float)t0, (float)t1) : (validT0 ? (float)t0 : (float)t1);

            // Calculate future impact position considering target velocity
            impactPoint = target + targetVelocity * t;

            // Calculate horizontal firing velocity direction and magnitude
            Vector3 horizontalDir = new Vector3(impactPoint.x - projectile.x, 0f, impactPoint.z - projectile.z).normalized;
            firingVelocity = horizontalDir * linearSpeed;

            // Vertical motion: solve for gravity and vertical velocity to reach arcCeiling at t/2 and impact at t
            float y0 = projectile.y;
            float yPeak = Mathf.Max(y0, impactPoint.y) + arcCeiling;
            float yFinal = impactPoint.y;

            gravity = -4f * (y0 - 2f * yPeak + yFinal) / (t * t);
            firingVelocity.y = -(3f * y0 - 4f * yPeak + yFinal) / t;

            return true;
        }

        /// <summary>
        /// Solves for velocity and gravity to hit a moving target at a specified max height.
        /// </summary>
        /// <param name="projectile">Start position.</param>
        /// <param name="linearSpeed">Speed on horizontal plane.</param>
        /// <param name="target">Initial target position.</param>
        /// <param name="targetVelocity">Target velocity vector.</param>
        /// <param name="arcCeiling">Desired max height above max(startY, targetY).</param>
        /// <param name="firingVelocity">Output firing velocity.</param>
        /// <param name="gravity">Output gravity (negative value).</param>
        /// <param name="impactPoint">Computed impact point considering target movement.</param>
        /// <returns>True if solution found, false otherwise.</returns>
        public static bool Solution2D(Vector2 projectile, float linearSpeed, Vector2 target, Vector2 targetVelocity, float arcCeiling, out Vector2 firingVelocity, out float gravity, out Vector2 impactPoint)
        {
            firingVelocity = Vector2.zero;
            gravity = 0f;
            impactPoint = Vector2.zero;

            if (projectile == target || linearSpeed <= 0f)
                return false;

            // Use only horizontal component of target velocity (X axis in 2D)
            float targetVelX = targetVelocity.x;
            float diffX = target.x - projectile.x;

            // Quadratic: (targetVelX^2 - linearSpeed^2)*t^2 + 2*diffX*targetVelX*t + diffX^2 = 0
            float c0 = targetVelX * targetVelX - linearSpeed * linearSpeed;
            float c1 = 2f * diffX * targetVelX;
            float c2 = diffX * diffX;

            double t0, t1;
            int n = SolveQuadric(c0, c1, c2, out t0, out t1);

            bool valid0 = n > 0 && t0 > 0;
            bool valid1 = n > 1 && t1 > 0;

            if (!valid0 && !valid1)
                return false;

            float t = valid0 && valid1 ? Mathf.Min((float)t0, (float)t1) : (valid0 ? (float)t0 : (float)t1);

            // Calculate impact point considering target velocity
            impactPoint = target + targetVelocity * t;

            // Calculate horizontal firing velocity (X component)
            float dirX = impactPoint.x - projectile.x;
            float horizontalDir = Mathf.Sign(dirX);
            firingVelocity.x = horizontalDir * linearSpeed;

            // Vertical calculation (Y)
            float a = projectile.y;
            float b = Mathf.Max(projectile.y, impactPoint.y) + arcCeiling;
            float c = impactPoint.y;

            gravity = -4f * (a - 2f * b + c) / (t * t);
            firingVelocity.y = -(3f * a - 4f * b + c) / t;

            return true;
        }

        /// <summary>
        /// Returns the initial velocity required to hit the target in the specified flight time under gravity.
        /// </summary>
        /// <param name="projectile">Starting position</param>
        /// <param name="target">Target position</param>
        /// <param name="gravity">Gravity acceleration (negative value)</param>
        /// <param name="flightTime">Desired time to impact</param>
        /// <returns>Initial velocity vector</returns>
        public static Vector3 Solution(Vector3 projectile, Vector3 target, float gravity, float flightTime)
        {
            Vector3 displacement = target - projectile;
            Vector3 planar = new Vector3(displacement.x, 0f, displacement.z);

            float verticalDisplacement = displacement.y;
            float horizontalDistance = planar.magnitude;

            // Calculate vertical velocity component needed to reach the target height in flightTime
            float verticalVelocity = verticalDisplacement / flightTime - 0.5f * gravity * flightTime;

            // Calculate horizontal velocity component needed to cover horizontalDistance in flightTime
            float horizontalVelocity = horizontalDistance / flightTime;

            Vector3 velocity = planar.normalized * horizontalVelocity;
            velocity.y = verticalVelocity;

            return velocity;
        }

        /// <summary>
        /// Returns the velocity required to hit the target at the specified time in 2D (x, y).
        /// </summary>
        public static Vector2 Solution2D(Vector2 projectile, Vector2 target, float gravity, float flightTime)
        {
            Vector2 displacement = target - projectile;

            float verticalDisplacement = displacement.y;
            float horizontalDistance = displacement.x;

            float verticalVelocity = verticalDisplacement / flightTime - 0.5f * gravity * flightTime;
            float horizontalVelocity = horizontalDistance / flightTime;

            return new Vector2(horizontalVelocity, verticalVelocity);
        }

        /// <summary>
        /// Finds the required speed of the projectile to hit the target given a firing direction
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="target"></param>
        /// <param name="angle">the angle of launch</param>
        /// <param name="gravity"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public static bool Solution(Vector3 projectile, Vector3 target, float angleDegrees, float gravity, out float speed)
        {
            speed = 0f;

            Vector3 displacement = target - projectile;
            Vector3 planar = new Vector3(displacement.x, 0f, displacement.z);
            float range = planar.magnitude;

            if (range == 0f)
                return false;

            float angle = angleDegrees * Mathf.Deg2Rad;
            float tanAlpha = Mathf.Tan(angle);

            // Denominator must be positive for a valid sqrt
            float denominator = 2f * (displacement.y - range * tanAlpha);

            if (denominator >= 0f)
                return false;

            // Calculate horizontal velocity component (z)
            float z = Mathf.Sqrt(-gravity * range * range / denominator);

            // Vertical velocity component (y)
            float y = tanAlpha * z;

            speed = new Vector3(0f, y, z).magnitude;

            return !float.IsNaN(speed);
        }

        /// <summary>
        /// Finds the required speed of the projectile to hit the target given a firing direction
        /// </summary>
        /// <param name="projectile"></param>
        /// <param name="target"></param>
        /// <param name="angle">the angle of launch</param>
        /// <param name="gravity"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        public static bool Solution2D(Vector2 projectile, Vector2 target, float angleDegrees, float gravity, out float speed)
        {
            speed = 0f;
            Vector2 displacement = target - projectile;
            float range = Mathf.Abs(displacement.x);  // horizontal distance

            if (range == 0f)
                return false;

            float angle = angleDegrees * Mathf.Deg2Rad;
            float tanAlpha = Mathf.Tan(angle);

            float denominator = 2f * (displacement.y - range * tanAlpha);

            if (denominator >= 0f)
                return false;

            float z = Mathf.Sqrt(-gravity * range * range / denominator);  // horizontal velocity
            float y = tanAlpha * z;                                        // vertical velocity

            speed = new Vector2(z, y).magnitude;

            return !float.IsNaN(speed);
        }

        /// <summary>
        /// marches a ray cast along the trajectory checking for impact and tracing the path
        /// </summary>
        /// <param name="start">The world point to start from</param>
        /// <param name="velocity">The velocity (direction * speed) to march</param>
        /// <param name="constantAcceleration">The effect of gravity</param>
        /// <param name="resolution">The distance for each march to test</param>
        /// <param name="maxLength">The maximum length to test across</param>
        /// <param name="collisionLayers">The layers to test for collision with</param>
        /// <param name="hit">The hit information, if non then hit.transform will be null</param>
        /// <param name="path">A list of points along the path that where traversed includes the start and hit point</param>
        /// <param name="distance">The arc distance travailed</param>
        /// <returns>True if a collision occurred, false otherwise</returns>
        public static bool Raycast(
            Vector3 start, 
            Vector3 velocity, 
            Vector3 constantAcceleration,
            float resolution, 
            float maxLength, 
            LayerMask collisionLayers,
            out RaycastHit hit,
            out List<(Vector3 position, Vector3 velocity, float time)> path,
            out float distance)
        {
            path = new List<(Vector3, Vector3, float)> { (start, velocity, 0f) };
            var currentPos = start;
            var currentVel = velocity;
            var timeSum = 0f;
            distance = 0f;
            hit = default;

            if (currentVel.magnitude < Mathf.Epsilon)
                return false;

            Ray ray = new Ray(currentPos, currentVel.normalized);

            while (!Physics.Raycast(ray, out hit, resolution, collisionLayers)
                && Vector3.Distance(start, currentPos) < maxLength)
            {
                float t = resolution / currentVel.magnitude;
                timeSum += t;
                var prevPos = currentPos;
                currentVel += t * constantAcceleration;
                currentPos += t * currentVel;
                distance += Vector3.Distance(prevPos, currentPos);
                path.Add((currentPos, currentVel, timeSum));

                ray = new Ray(currentPos, currentVel.normalized);
            }

            if (hit.transform != null)
            {
                var hitPoint = ray.GetPoint(hit.distance);
                float hitTime = timeSum + (hitPoint - currentPos).magnitude / currentVel.magnitude;
                path.Add((hitPoint, currentVel, hitTime));
                return true;
            }

            return false;
        }

        /// <summary>
        /// marches a ray cast along the trajectory checking for impact and tracing the path
        /// </summary>
        /// <param name="start">The world point to start from</param>
        /// <param name="velocity">The velocity (direction * speed) to march</param>
        /// <param name="constantAcceleration">The effect of gravity</param>
        /// <param name="resolution">The distance for each march to test</param>
        /// <param name="maxLength">The maximum length to test across</param>
        /// <param name="collisionLayers">The layers to test for collision with</param>
        /// <param name="hit">The hit information, if non then hit.transform will be null</param>
        /// <param name="path">A list of points along the path that where traversed includes the start and hit point</param>
        /// <param name="distance">The arc distance travailed</param>
        /// <returns>True if a collision occurred, false otherwise</returns>
        public static bool SphereCast(
            Vector3 start, 
            Collider startCollider, 
            Vector3 velocity, 
            Vector3 constantAcceleration,
            float radius, 
            float resolution, 
            float maxLength, 
            LayerMask collisionLayers,
            QueryTriggerInteraction queryTriggerInteraction,
            out RaycastHit hit,
            out List<(Vector3 position, Vector3 velocity, float time)> path,
            out float distance)
        {
            path = new List<(Vector3, Vector3, float)> { (start, velocity, 0f) };
            var currentPos = start;
            var currentVel = velocity;
            var timeSum = 0f;
            distance = 0f;
            hit = default;

            if (currentVel.magnitude < Mathf.Epsilon)
                return false;

            if (startCollider != null)
                startCollider.enabled = false;

            Ray ray = new Ray(currentPos, currentVel.normalized);

            while (!Physics.SphereCast(ray, radius, out hit, resolution, collisionLayers, queryTriggerInteraction)
                && Vector3.Distance(start, currentPos) < maxLength)
            {
                float t = resolution / currentVel.magnitude;
                timeSum += t;
                var prevPos = currentPos;
                currentVel += t * constantAcceleration;
                currentPos += t * currentVel;
                distance += Vector3.Distance(prevPos, currentPos);
                path.Add((currentPos, currentVel, timeSum));

                ray = new Ray(currentPos, currentVel.normalized);

                if (startCollider != null && !startCollider.enabled && distance > radius)
                    startCollider.enabled = true;
            }

            if (startCollider != null && !startCollider.enabled)
                startCollider.enabled = true;

            if (hit.transform != null)
            {
                var hitPoint = ray.GetPoint(hit.distance);
                float hitTime = timeSum + (hitPoint - currentPos).magnitude / currentVel.magnitude;
                path.Add((hitPoint, currentVel, hitTime));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Performs a single sphere cast along the velocity vector for up to maxLength.
        /// Temporarily disables startCollider to avoid self-collision.
        /// </summary>
        public static bool SphereCast(
            Vector3 start,
            Collider startCollider,
            Vector3 velocity,
            float radius,
            float resolution,       
            float maxLength,
            LayerMask collisionLayers,
            QueryTriggerInteraction queryTriggerInteraction,
            out RaycastHit hit,
            out List<(Vector3 position, Vector3 velocity, float time)> path,
            out float distance)
        {
            path = new List<(Vector3 position, Vector3 velocity, float time)>
    {
        (start, velocity, 0f),
    };

            distance = 0f;

            if (startCollider != null)
                startCollider.enabled = false;

            Ray ray = new Ray(start, velocity.normalized);

            bool hitSomething = Physics.SphereCast(ray, radius, out hit, maxLength, collisionLayers, queryTriggerInteraction);

            if (startCollider != null)
                startCollider.enabled = true;

            if (hitSomething && hit.transform != null)
            {
                var target = ray.GetPoint(hit.distance);
                float hitTime = (target - start).magnitude / velocity.magnitude;
                path.Add((target, velocity, hitTime));
                distance = (target - start).magnitude;
                return true;
            }
            else
            {
                distance = maxLength;
                return false;
            }
        }

        /// <summary>
        /// marches a ray cast along the trajectory checking for impact and tracing the path
        /// </summary>
        /// <param name="start">The world point to start from</param>
        /// <param name="velocity">The velocity (direction * speed) to march</param>
        /// <param name="constantAcceleration">The effect of gravity</param>
        /// <param name="resolution">The distance for each march to test</param>
        /// <param name="maxLength">The maximum length to test across</param>
        /// <param name="collisionLayers">The layers to test for collision with</param>
        /// <param name="hit">The hit information, if non then hit.transform will be null</param>
        /// <param name="path">A list of points along the path that where traversed includes the start and hit point</param>
        /// <param name="distance">The arc distance travailed</param>
        /// <returns>True if a collision occurred, false otherwise</returns>
        public static bool Raycast2D(
            Vector2 start,
            Vector2 velocity,
            Vector2 constantAcceleration,
            float resolution,
            float maxLength,
            LayerMask collisionLayers,
            out RaycastHit2D hit,
            out List<(Vector2 position, Vector2 velocity, float time)> path,
            out float distance)
        {
            path = new List<(Vector2 position, Vector2 velocity, float time)>
    {
        (start, velocity, 0f),
    };

            var currentPos = start;
            var currentVel = velocity;
            var timeSum = 0f;
            distance = 0f;

            hit = Physics2D.Raycast(currentPos, currentVel.normalized, resolution, collisionLayers);

            while (hit.collider == null
                && Vector2.Distance(start, currentPos) < maxLength)
            {
                var t = resolution / currentVel.magnitude;
                timeSum += t;
                var s = currentPos;
                currentVel += t * constantAcceleration;
                currentPos += t * currentVel;
                distance += Vector2.Distance(s, currentPos);
                path.Add((currentPos, currentVel, timeSum));

                hit = Physics2D.Raycast(currentPos, currentVel.normalized, resolution, collisionLayers);
            }

            if (hit.collider != null)
            {
                float hitTime = timeSum + ((hit.centroid - currentPos).magnitude / currentVel.magnitude);
                path.Add((hit.centroid, currentVel, hitTime));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// marches a ray cast along the trajectory checking for impact and tracing the path
        /// </summary>
        /// <param name="start">The world point to start from</param>
        /// <param name="velocity">The velocity (direction * speed) to march</param>
        /// <param name="constantAcceleration">The effect of gravity</param>
        /// <param name="resolution">The distance for each march to test</param>
        /// <param name="maxLength">The maximum length to test across</param>
        /// <param name="collisionLayers">The layers to test for collision with</param>
        /// <param name="hit">The hit information, if non then hit.transform will be null</param>
        /// <param name="path">A list of points along the path that where traversed includes the start and hit point</param>
        /// <param name="distance">The arc distance travailed</param>
        /// <returns>True if a collision occurred, false otherwise</returns>
        public static bool CircleCast(
            Vector2 start,
            Collider2D startCollider,
            Vector2 velocity,
            Vector2 constantAcceleration,
            float radius,
            float resolution,
            float maxLength,
            LayerMask collisionLayers,
            out RaycastHit2D hit,
            out List<(Vector2 position, Vector2 velocity, float time)> path,
            out float distance)
        {
            path = new List<(Vector2 position, Vector2 velocity, float time)>
    {
        (start, velocity, 0f),
    };

            resolution = Mathf.Max(resolution, 0.001f);
            var currentPos = start;
            var currentVel = velocity;
            var timeSum = 0f;
            distance = 0f;

            if (startCollider != null)
                startCollider.enabled = false;

            hit = Physics2D.CircleCast(currentPos, radius, currentVel.normalized, resolution, collisionLayers);

            while (hit.collider == null && distance < maxLength)
            {
                var t = resolution / currentVel.magnitude;
                timeSum += t;
                var s = currentPos;
                currentVel += t * constantAcceleration;
                currentPos += t * currentVel;
                distance += Vector2.Distance(s, currentPos);
                path.Add((currentPos, currentVel, timeSum));

                hit = Physics2D.CircleCast(currentPos, radius, currentVel.normalized, resolution, collisionLayers);
            }

            if (startCollider != null)
                startCollider.enabled = true;

            if (hit.collider != null)
            {
                var t = (hit.centroid - currentPos).magnitude / currentVel.magnitude;
                path.Add((hit.centroid, currentVel, timeSum + t));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Ported from GraphicsGems by Jochen Schwarze
        /// </remarks>
        /// <param name="value"></param>
        /// <returns></returns>
        private static double GetCubicRoot(double value)
        {
            if (value > 0.0)
            {
                return System.Math.Pow(value, 1.0 / 3.0);
            }
            else if (value < 0)
            {
                return -System.Math.Pow(-value, 1.0 / 3.0);
            }
            else
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Solves a cubic equation c0*x³ + c1*x² + c2*x + c3 = 0 using Cardano's method.
        /// </summary>
        /// <remarks>
        /// Ported from GraphicsGems by Jochen Schwarze.
        /// </remarks>
        /// <param name="c0">Coefficient of x³ (must be non-zero).</param>
        /// <param name="c1">Coefficient of x².</param>
        /// <param name="c2">Coefficient of x.</param>
        /// <param name="c3">Constant term.</param>
        /// <param name="s0">First root (output).</param>
        /// <param name="s1">Second root (output, NaN if not applicable).</param>
        /// <param name="s2">Third root (output, NaN if not applicable).</param>
        /// <returns>Number of real roots found (1, 2, or 3).</returns>
        private static int SolveCubic(double c0, double c1, double c2, double c3, out double s0, out double s1, out double s2)
        {
            s0 = double.NaN;
            s1 = double.NaN;
            s2 = double.NaN;

            // Normalize coefficients to monic form: x^3 + A x^2 + B x + C = 0
            double A = c1 / c0;
            double B = c2 / c0;
            double C = c3 / c0;

            // Substitute x = y - A/3 to remove quadratic term:
            // y^3 + p y + q = 0
            double sq_A = A * A;
            double p = (B - sq_A / 3.0) / 3.0;
            double q = (2.0 * A * sq_A / 27.0 - A * B / 3.0 + C) / 2.0;

            double cb_p = p * p * p;
            double D = q * q + cb_p; // Discriminant

            int numRoots;
            if (Math.Abs(D) < 1e-14) D = 0; // Handle floating-point precision issues

            if (D == 0)
            {
                if (q == 0)
                {
                    // One triple root
                    s0 = 0;
                    numRoots = 1;
                }
                else
                {
                    // One single and one double root
                    double u = GetCubicRoot(-q);
                    s0 = 2 * u;
                    s1 = -u;
                    numRoots = 2;
                }
            }
            else if (D < 0)
            {
                // Three distinct real roots (Casus irreducibilis)
                double phi = Math.Acos(-q / Math.Sqrt(-cb_p)) / 3.0;
                double t = 2 * Math.Sqrt(-p);

                s0 = t * Math.Cos(phi);
                s1 = -t * Math.Cos(phi + Math.PI / 3.0);
                s2 = -t * Math.Cos(phi - Math.PI / 3.0);
                numRoots = 3;
            }
            else
            {
                // One real root
                double sqrt_D = Math.Sqrt(D);
                double u = GetCubicRoot(-q + sqrt_D);
                double v = GetCubicRoot(-q - sqrt_D);
                s0 = u + v;
                numRoots = 1;
            }

            // Resubstitute to get roots of original equation
            double sub = A / 3.0;
            if (numRoots > 0) s0 -= sub;
            if (numRoots > 1) s1 -= sub;
            if (numRoots > 2) s2 -= sub;

            return numRoots;
        }

        /// <summary>
        /// Solves the quadratic equation c0*x² + c1*x + c2 = 0
        /// </summary>
        /// <remarks>
        /// Ported from GraphicsGems by Jochen Schwarze
        /// </remarks>
        /// <param name="c0">Coefficient of x² (must be non-zero)</param>
        /// <param name="c1">Coefficient of x</param>
        /// <param name="c2">Constant term</param>
        /// <param name="s0">First root (output)</param>
        /// <param name="s1">Second root (output)</param>
        /// <returns>
        /// Number of real roots found (0, 1, or 2)
        /// </returns>
        private static int SolveQuadric(double c0, double c1, double c2, out double s0, out double s1)
        {
            s0 = double.NaN;
            s1 = double.NaN;

            // Normalize coefficients to the form x² + 2px + q = 0
            double p = c1 / (2 * c0);
            double q = c2 / c0;

            // Discriminant
            double D = p * p - q;

            if (D == 0)
            {
                // One real root (double root)
                s0 = -p;
                return 1;
            }
            else if (D < 0)
            {
                // No real roots
                return 0;
            }
            else
            {
                // Two distinct real roots
                double sqrt_D = System.Math.Sqrt(D);
                s0 = -p + sqrt_D;
                s1 = -p - sqrt_D;
                return 2;
            }
        }

        /// <summary>
        /// Solves the quartic equation c0*x⁴ + c1*x³ + c2*x² + c3*x + c4 = 0
        /// </summary>
        /// <remarks>
        /// Ported from GraphicsGems by Jochen Schwarze
        /// </remarks>
        /// <param name="c0">Coefficient of x⁴ (must be non-zero)</param>
        /// <param name="c1">Coefficient of x³</param>
        /// <param name="c2">Coefficient of x²</param>
        /// <param name="c3">Coefficient of x</param>
        /// <param name="c4">Constant term</param>
        /// <param name="s0">First root (output)</param>
        /// <param name="s1">Second root (output)</param>
        /// <param name="s2">Third root (output)</param>
        /// <param name="s3">Fourth root (output)</param>
        /// <returns>
        /// Number of real roots found (0 to 4)
        /// </returns>
        private static int SolveQuartic(double c0, double c1, double c2, double c3, double c4, out double s0, out double s1, out double s2, out double s3)
        {
            s0 = double.NaN;
            s1 = double.NaN;
            s2 = double.NaN;
            s3 = double.NaN;

            double[] coeffs = new double[4];
            double z, u, v, sub;
            double A, B, C, D;
            double sq_A, p, q, r;
            int num;

            // Normalize coefficients: x⁴ + A x³ + B x² + C x + D = 0
            A = c1 / c0;
            B = c2 / c0;
            C = c3 / c0;
            D = c4 / c0;

            // Substitute x = y - A/4 to remove cubic term:
            // y⁴ + p y² + q y + r = 0
            sq_A = A * A;
            p = -3.0 / 8 * sq_A + B;
            q = 1.0 / 8 * sq_A * A - 1.0 / 2 * A * B + C;
            r = -3.0 / 256 * sq_A * sq_A + 1.0 / 16 * sq_A * B - 1.0 / 4 * A * C + D;

            if (r == 0)
            {
                // No absolute term: y(y³ + p y + q) = 0
                coeffs[3] = q;
                coeffs[2] = p;
                coeffs[1] = 0;
                coeffs[0] = 1;

                num = SolveCubic(coeffs[0], coeffs[1], coeffs[2], coeffs[3], out s0, out s1, out s2);
            }
            else
            {
                // Solve resolvent cubic
                coeffs[3] = 0.5 * r * p - 0.125 * q * q;
                coeffs[2] = -r;
                coeffs[1] = -0.5 * p;
                coeffs[0] = 1;

                SolveCubic(coeffs[0], coeffs[1], coeffs[2], coeffs[3], out s0, out s1, out s2);

                z = s0; // One real root from cubic

                // Build two quadratic equations
                u = z * z - r;
                v = 2 * z - p;

                if (u < 0) return 0; // No real roots if u negative
                u = (u == 0) ? 0 : System.Math.Sqrt(u);

                if (v < 0) return 0; // No real roots if v negative
                v = (v == 0) ? 0 : System.Math.Sqrt(v);

                coeffs[2] = z - u;
                coeffs[1] = q < 0 ? -v : v;
                coeffs[0] = 1;

                num = SolveQuadric(coeffs[0], coeffs[1], coeffs[2], out s0, out s1);

                coeffs[2] = z + u;
                coeffs[1] = q < 0 ? v : -v;
                coeffs[0] = 1;

                if (num == 0)
                    num += SolveQuadric(coeffs[0], coeffs[1], coeffs[2], out s0, out s1);
                else if (num == 1)
                    num += SolveQuadric(coeffs[0], coeffs[1], coeffs[2], out s1, out s2);
                else if (num == 2)
                    num += SolveQuadric(coeffs[0], coeffs[1], coeffs[2], out s2, out s3);
            }

            // Resubstitute to original variable
            sub = A / 4.0;

            if (num > 0) s0 -= sub;
            if (num > 1) s1 -= sub;
            if (num > 2) s2 -= sub;
            if (num > 3) s3 -= sub;

            return num;
        }

    }
}