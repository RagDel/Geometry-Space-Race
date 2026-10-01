using System;
using UnityEngine;

namespace GeometrySpaceRace.Domain
{
    /// <summary>A nonbanked cross-section of the cylindrical course.</summary>
    public readonly struct CourseFrame
    {
        // Absolute Center is diagnostic only at large distances. Use ToWorld for
        // rendering/physics: it subtracts the origin in double precision first.
        public readonly Vector3 Center, Tangent, Right, Up;
        public readonly Quaternion Rotation;
        /// <summary>True centreline curvature in local right/up axes, in 1/metres.</summary>
        public readonly Vector2 Curvature;
        /// <summary>World centreline arc metres per longitudinal course metre.</summary>
        public readonly float MetricScale;

        internal CourseFrame(Vector3 center, Vector3 tangent, Vector3 right, Vector3 up, Vector2 curvature, float metric)
        {
            Center = center; Tangent = tangent; Right = right; Up = up;
            Rotation = Quaternion.LookRotation(tangent, up);
            Curvature = curvature; MetricScale = metric;
        }
    }

    /// <summary>
    /// O(1), history-free seeded bends. S remains the existing longitudinal score
    /// coordinate, not exact arc length. Centreline arc excess is at most 7.4%.
    /// Cross-sections have radius 30m and remain perpendicular to the centreline.
    /// </summary>
    public sealed class CylinderCourse
    {
        public const float Radius = 30f, CenterY = 9f;
        public const float MaximumMetricScale = 1.074f;
        public const float MaximumCurvature = .000590f;
        public const float HeadingLagSeconds = .03f;
        public const double MinimumRampDistance = 60000d;
        private const double Tau = Math.PI * 2d;
        private const double XPeriod = 4000d, YPeriod = 6000d;
        private const double XAmplitude = 220d, YAmplitude = 160d;
        private const double InitialStrength = .25d;
        // For L>=60000, |a'|<=.75*1.875/L and
        // |a''|<=.75*(10/sqrt(3))/L^2. For either axis of amplitude A,
        // |C'axis|<=A*(k+|a'|), |C''axis|<=A*(k^2+2k|a'|+|a''|).
        // These yield metric<=1.073478826 and |C''|<=.000588626945.
        // True curvature=|C' x C''|/|C'|^3 <= |C''| because |C'|>=1.
        // Bounds include the complete progression ramp, independent of seed.
        private readonly double phaseX, phaseY, startX, startY;
        public double RampDistance { get; }

        public CylinderCourse(double levelDistance = 2500d, int seed = 0, int capLevel = 140)
        {
            if (!Finite(levelDistance) || levelDistance <= 0d) throw new ArgumentOutOfRangeException(nameof(levelDistance));
            if (capLevel <= 0 || !Finite(levelDistance * capLevel)) throw new ArgumentOutOfRangeException(nameof(capLevel));
            RampDistance = Math.Max(MinimumRampDistance, levelDistance * capLevel);
            phaseX = Phase(unchecked((uint)seed) ^ 0xBC564A91u);
            phaseY = Phase(unchecked((uint)seed) ^ 0x69531D2Fu);
            startX = XAmplitude * InitialStrength * Math.Sin(phaseX);
            startY = YAmplitude * InitialStrength * Math.Sin(phaseY);
        }

        public CourseFrame Sample(double absoluteS)
        {
            Evaluate(absoluteS, out double x, out double y, out double dx, out double dy, out double ddx, out double ddy);
            double horizontal = Math.Sqrt(1d + dx * dx);
            double metric = Math.Sqrt(1d + dx * dx + dy * dy);
            var tangent = new Vector3((float)(dx / metric), (float)(dy / metric), (float)(1d / metric));
            var right = new Vector3((float)(1d / horizontal), 0f, (float)(-dx / horizontal));
            var up = new Vector3((float)(-dx * dy / (metric * horizontal)), (float)(horizontal / metric), (float)(-dy / (metric * horizontal)));
            // Dotting C'' into either normal removes its tangent component;
            // divide by |C'|^2 to obtain true curvature per arc metre.
            double metricSquared = metric * metric;
            var curvature = new Vector2((float)(ddx / (horizontal * metricSquared)),
                (float)((ddy * horizontal - ddx * dx * dy / horizontal) / (metric * metricSquared)));
            return new CourseFrame(new Vector3((float)x, CenterY + (float)y, (float)absoluteS),
                tangent, right, up, curvature, (float)metric);
        }

        /// <summary>
        /// Convert local course x/y/S to rebased world space. The origin's centre
        /// stays at (0,9,0); cross-section offsets retain exact metre dimensions.
        /// Signed S supports chase-camera samples before the starting line.
        /// </summary>
        public Vector3 ToWorld(Vector3 coursePosition, double originS)
        {
            if (!Finite(coursePosition.x) || !Finite(coursePosition.y) || !Finite(coursePosition.z))
                throw new ArgumentOutOfRangeException(nameof(coursePosition));
            CourseFrame frame = Sample(originS + coursePosition.z);
            Evaluate(originS + coursePosition.z, out double x, out double y, out _, out _, out _, out _);
            Evaluate(originS, out double ox, out double oy, out _, out _, out _, out _);
            // Do not subtract already-rounded absolute Vector3 positions. Keep
            // local Z directly, avoiding large-distance cancellation as well.
            return new Vector3((float)(x - ox), CenterY + (float)(y - oy), coursePosition.z)
                + frame.Right * coursePosition.x + frame.Up * (coursePosition.y - CenterY);
        }

        /// <summary>
        /// First-order outward velocity from a heading lag. The caller adds it
        /// to course-space transverse motion; there is no positional attraction.
        /// Speed is course metres/second, so the metric converts it to arc speed.
        /// </summary>
        public Vector2 HeadingLagDrift(double absoluteS, float speed, float lag = HeadingLagSeconds)
        {
            if (!Finite(speed) || speed < 0f) throw new ArgumentOutOfRangeException(nameof(speed));
            if (!Finite(lag) || lag < 0f) throw new ArgumentOutOfRangeException(nameof(lag));
            CourseFrame frame = Sample(absoluteS);
            double arcSpeed = speed * (double)frame.MetricScale;
            return frame.Curvature * (float)(-arcSpeed * arcSpeed * lag);
        }

        /// <summary>
        /// Preserve the former bend force throughout every previously reachable speed.
        /// Additional Rocket speed saturates the lag response at the supplied former per-chassis maximum;
        /// geometry still advances at the real speed. No centre attraction or steering boost.
        /// </summary>
        public Vector2 AssistedHeadingLagDrift(double absoluteS, float speed, float saturationSpeed)
        {
            if (!Finite(saturationSpeed) || saturationSpeed <= 0f) throw new ArgumentOutOfRangeException(nameof(saturationSpeed));
            if (!Finite(speed) || speed < 0f) throw new ArgumentOutOfRangeException(nameof(speed));
            return HeadingLagDrift(absoluteS, Math.Min(speed, saturationSpeed));
        }

        private void Evaluate(double s, out double x, out double y, out double dx, out double dy, out double ddx, out double ddy)
        {
            if (!Finite(s)) throw new ArgumentOutOfRangeException(nameof(s));
            double t = Math.Max(0d, Math.Min(1d, s / RampDistance));
            double smooth = t * t * t * (10d + t * (-15d + 6d * t));
            double a = InitialStrength + .75d * smooth;
            double da = .75d * 30d * t * t * (t - 1d) * (t - 1d) / RampDistance;
            double dda = .75d * 60d * t * (2d * t * t - 3d * t + 1d) / (RampDistance * RampDistance);
            Axis(s, XPeriod, XAmplitude, phaseX, a, da, dda, out x, out dx, out ddx);
            Axis(s, YPeriod, YAmplitude, phaseY, a, da, dda, out y, out dy, out ddy);
            x -= startX; y -= startY;
        }

        private static void Axis(double s, double period, double amplitude, double phase, double a, double da, double dda,
            out double position, out double derivative, out double secondDerivative)
        {
            double k = Tau / period;
            double angle = Tau * ((s % period) / period) + phase;
            double sin = Math.Sin(angle), cos = Math.Cos(angle);
            position = amplitude * a * sin;
            derivative = amplitude * (da * sin + a * k * cos);
            secondDerivative = amplitude * (dda * sin + 2d * da * k * cos - a * k * k * sin);
        }

        private static double Phase(uint value)
        {
            unchecked
            {
                value ^= value >> 16; value *= 0x7FEB352Du;
                value ^= value >> 15; value *= 0x846CA68Bu; value ^= value >> 16;
                return value * (Tau / 4294967296d);
            }
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
