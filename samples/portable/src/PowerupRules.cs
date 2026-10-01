namespace GeometrySpaceRace.Domain
{
    public enum PowerupKind { Heart, Rocket, Hourglass, Laser }

    /// <summary>Shared pickup units and limits; durations use active play time, not movement time.</summary>
    public static class PowerupRules
    {
        public const float RocketMultiplier = 2f;
        public const float RocketDuration = 5f;
        public const float HourglassMovementScale = .5f;
        public const float HourglassDuration = 8f;
        public const float LaserChargeDuration = 3f;
        public const float LaserRange = 1000f;
        public const float PickupRadius = 6.75f;
        public const int HourglassUnlockLevel = 35;
    }
}
