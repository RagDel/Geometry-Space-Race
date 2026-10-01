using System;
using UnityEngine;

namespace GeometrySpaceRace.Domain
{
    public readonly struct DashSnapshot
    {
        public readonly int Charges, Capacity;
        public readonly bool Active;
        public readonly Vector2 Direction;
        public readonly float Progress, ActiveRemaining, RechargeRemaining, RechargeDuration, RechargeFraction;
        public DashSnapshot(int charges, bool active, Vector2 direction, float progress, float activeRemaining,
            float rechargeRemaining, float rechargeDuration)
        {
            Charges = charges; Capacity = DashController.MaximumCharges; Active = active; Direction = direction;
            Progress = progress; ActiveRemaining = activeRemaining; RechargeRemaining = rechargeRemaining;
            RechargeDuration = rechargeDuration;
            RechargeFraction = charges == Capacity ? 1f : Mathf.Clamp01(1f - rechargeRemaining / rechargeDuration);
        }
    }

    /// <summary>Three independently spent charges, recharged sequentially on the committed gameplay clock.</summary>
    public sealed class DashController
    {
        public const int MaximumCharges = 3;
        public const float Distance = 10f;
        public const float Duration = .30f;
        public const float MaximumSpeed = 1.875f * Distance / Duration;
        private const double Tolerance = 1e-8;
        private double lastTime, activeStart, activeEnd, rechargeEnd;
        private float configuredRecharge = 4f, capturedRecharge = 4f;
        public int Charges { get; private set; } = MaximumCharges;
        public bool Active { get; private set; }
        public Vector2 Direction { get; private set; }

        public void Reset()
        {
            Charges = MaximumCharges; Active = false; Direction = Vector2.zero;
            lastTime = activeStart = activeEnd = rechargeEnd = 0d;
            configuredRecharge = capturedRecharge = 4f;
        }

        public void ApplyRechargeDuration(float duration, double now)
        {
            if (!(duration > 0f) || float.IsInfinity(duration)) throw new ArgumentOutOfRangeException(nameof(duration));
            ResolveAt(now);
            configuredRecharge = duration;
            if (Charges == MaximumCharges) capturedRecharge = duration;
        }

        public bool TryActivate(Vector2 direction, double now, bool allowed)
        {
            ResolveAt(now);
            if (!allowed || Active || Charges == 0 || !Finite(direction.x) || !Finite(direction.y) || direction.sqrMagnitude < .01f)
                return false;
            Direction = direction.normalized;
            Active = true; activeStart = now; activeEnd = now + Duration;
            if (Charges == MaximumCharges)
            {
                capturedRecharge = configuredRecharge;
                rechargeEnd = now + capturedRecharge;
            }
            Charges--;
            return true;
        }

        public void ResolveAt(double now)
        {
            ValidateTime(now);
            if (Active && now + Tolerance >= activeEnd) Active = false;
            // There can be at most three pending charges, even for a long diagnostic step.
            while (Charges < MaximumCharges && now + Tolerance >= rechargeEnd)
            {
                Charges++;
                if (Charges < MaximumCharges)
                {
                    capturedRecharge = configuredRecharge;
                    rechargeEnd += capturedRecharge;
                }
            }
            lastTime = now;
        }

        public void CancelActive(double now) { ResolveAt(now); Active = false; }

        public double NextTransitionTime(double now, float movementScale = 1f)
        {
            ValidateTime(now);
            return Math.Min(Active ? now + (activeEnd - now) / movementScale : double.PositiveInfinity,
                Charges < MaximumCharges ? rechargeEnd : double.PositiveInfinity);
        }

        // Stretch only the active trajectory; charge recharge keeps its real active-play expiry.
        public void CommitMovementScale(float consumedSeconds, float movementScale)
        {
            if (!Active) return;
            double delay = consumedSeconds * (1d - movementScale);
            activeStart += delay; activeEnd += delay;
        }

        /// <summary>Quintic position has zero speed and acceleration at both ends and one central speed peak.</summary>
        public static float EvaluateProgress(float progress) => (float)EvaluateProgress((double)progress);

        internal static double EvaluateProgress(double progress)
        {
            double u = Math.Max(0d, Math.Min(1d, progress));
            return u * u * u * (10d + u * (-15d + 6d * u));
        }

        internal double ProgressAt(double now) => Active ? Math.Max(0d, Math.Min(1d, (now - activeStart) / Duration)) : 0d;

        /// <summary>Exact integral of the bell-shaped transverse velocity, including partial and slowed intervals.</summary>
        public Vector2 Displacement(double start, float duration)
        {
            if (!Active || duration <= 0f) return Vector2.zero;
            return Direction * (float)(Distance * (EvaluateProgress(ProgressAt(start) + duration / (double)Duration)
                - EvaluateProgress(ProgressAt(start))));
        }

        public DashSnapshot GetSnapshot(double now)
        {
            ValidateTime(now);
            float remaining = Active ? (float)Math.Max(0d, activeEnd - now) : 0f;
            return new DashSnapshot(Charges, Active && remaining > 0f, Direction,
                Active ? Mathf.Clamp01((float)((now - activeStart) / Duration)) : 0f, remaining,
                Charges < MaximumCharges ? (float)Math.Max(0d, rechargeEnd - now) : 0f, capturedRecharge);
        }

        private void ValidateTime(double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now) || now < lastTime - Tolerance)
                throw new ArgumentOutOfRangeException(nameof(now), "Dash time must be finite and monotonic.");
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
