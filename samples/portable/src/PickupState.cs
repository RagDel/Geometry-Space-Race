using System;

namespace GeometrySpaceRace.Domain
{
    /// <summary>Pickup durations use active real play time, independently of slowed motion.</summary>
    public sealed class PickupState
    {
        private double now, rocketEnd, slowEnd, laserEnd;
        public float RocketRemaining => (float)Math.Max(0, rocketEnd - now);
        public float SlowRemaining => (float)Math.Max(0, slowEnd - now);
        public float LaserChargeRemaining => LaserCharging ? (float)Math.Max(0, laserEnd - now) : 0;
        public bool RocketActive => RocketRemaining > 0;
        public bool LaserCharging { get; private set; }
        public float MovementScale => SlowRemaining > 0 ? PowerupRules.HourglassMovementScale : 1f;
        public float SpeedMultiplier => RocketActive ? PowerupRules.RocketMultiplier : 1f;
        public void Reset() { now = rocketEnd = slowEnd = laserEnd = 0; LaserCharging = false; }
        public void Collect(PowerupKind kind, double time)
        {
            Synchronize(time);
            if (kind == PowerupKind.Rocket) rocketEnd = time + PowerupRules.RocketDuration;
            if (kind == PowerupKind.Hourglass) slowEnd = time + PowerupRules.HourglassDuration;
            if (kind == PowerupKind.Laser) { laserEnd = time + PowerupRules.LaserChargeDuration; LaserCharging = true; }
        }
        public void Synchronize(double time)
        {
            if (double.IsNaN(time) || double.IsInfinity(time) || time < now - 1e-8)
                throw new ArgumentOutOfRangeException(nameof(time));
            now = time;
        }
        public bool ResolveLaser()
        {
            if (!LaserCharging || now + 1e-7 < laserEnd) return false;
            LaserCharging = false; return true;
        }
        public double NextTransitionTime(double time)
        {
            double next = double.PositiveInfinity;
            if (rocketEnd > time + 1e-8) next = Math.Min(next, rocketEnd);
            if (slowEnd > time + 1e-8) next = Math.Min(next, slowEnd);
            if (LaserCharging && laserEnd > time + 1e-8) next = Math.Min(next, laserEnd);
            return next;
        }
    }
}
