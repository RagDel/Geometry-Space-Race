using GeometrySpaceRace.Domain;
using GeometrySpaceRace.Presentation;

// Behavioral contracts against the extracted production classes. No alternate
// shuffle, random-number generator, timer implementation, or Unity stand-in.
internal static class Program
{
    private static int assertions;

    private static int Main()
    {
        (string Name, Action Verify)[] checks =
        [
            ("Sky bags are permutations without consecutive repeats", SkyBags),
            ("Sky lookups reproduce across seeds and query orders", SkyQueryOrder),
            ("Sky selection stays fixed through each ten-checkpoint section", SkyCadence),
            ("Starting skies cover all ten choices across 100 seeds", SkySeedDiversity),
            ("Appearance cadence can be configured independently", IndependentCadence),
            ("Sky input bounds reject invalid requests", SkyInputBounds),
            ("Pickup reset clears every temporary effect", PickupReset),
            ("Pickup durations use the supplied active-play clock", PickupClock),
            ("Repeat pickup collection refreshes its own duration", PickupRefresh),
            ("Laser charge resolves once at or beyond expiry", LaserSingleResolution),
            ("Next pickup transition follows the earliest pending expiry", PickupTransitions),
            ("Pickup clocks reject backwards and non-finite time", PickupClockRejection),
        ];

        int failed = 0;
        foreach ((string name, Action verify) in checks)
        {
            try
            {
                verify();
                Console.WriteLine($"PASS {name}");
            }
            catch (Exception error)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {name}: {error.Message}");
            }
        }

        Console.WriteLine($"{checks.Length - failed}/{checks.Length} checks passed; {assertions} assertions evaluated.");
        return failed == 0 ? 0 : 1;
    }

    private static void SkyBags()
    {
        foreach (int seed in new[] { 0, 20260920, int.MinValue })
        {
            var selection = new SkySelection(seed, 10, 10);
            int previous = -1;
            for (int bag = 0; bag < 64; bag++)
            {
                var seen = new HashSet<int>();
                for (int index = 0; index < 10; index++)
                {
                    int value = selection.ForCheckpoint((bag * 10L + index) * 10);
                    Require(value >= 0 && value < 10, "Sky index is outside the ten textures.");
                    Require(seen.Add(value), "A shuffle bag repeats a texture.");
                    Require(value != previous, "Neighboring sections repeat a texture, including a bag boundary.");
                    previous = value;
                }
                Equal(10, seen.Count, "A complete bag must contain every texture.");
            }
        }
    }

    private static void SkyQueryOrder()
    {
        foreach (int seed in new[] { 0, 20260920, int.MinValue })
        {
            var ordered = new SkySelection(seed, 10, 10);
            var randomAccess = new SkySelection(seed, 10, 10);
            int[] expected = new int[640];
            for (int index = 0; index < expected.Length; index++)
                expected[index] = ordered.ForCheckpoint(index * 10L);

            // Descending requests cross cached bags in a different order.
            for (int index = expected.Length - 1; index >= 0; index--)
            {
                randomAccess.ForCheckpoint(long.MaxValue);
                Equal(expected[index], randomAccess.ForCheckpoint(index * 10L), "Query order changed a seeded result.");
            }
        }
    }

    private static void SkyCadence()
    {
        foreach (int seed in new[] { 0, 20260920, int.MinValue })
        {
            var selection = new SkySelection(seed, 10, 10);
            for (int section = 0; section < 640; section++)
            {
                long start = section * 10L;
                int expected = selection.ForCheckpoint(start);
                for (int offset = 1; offset < 10; offset++)
                    Equal(expected, selection.ForCheckpoint(start + offset), "Texture changed before its cadence boundary.");
            }
        }
    }

    private static void SkySeedDiversity()
    {
        var choices = new HashSet<int>();
        for (int seed = 0; seed < 100; seed++)
            choices.Add(new SkySelection(seed).ForCheckpoint(0));
        Equal(10, choices.Count, "The starting sky choices do not cover all textures.");
    }

    private static void IndependentCadence()
    {
        var historicalGround = new SkySelection(71, 8);
        Equal(historicalGround.ForCheckpoint(0), historicalGround.ForCheckpoint(34), "Default cadence must remain 35.");
        Require(historicalGround.ForCheckpoint(35) != historicalGround.ForCheckpoint(34), "Default cadence missed its boundary.");

        var sky = new SkySelection(71, 10, 10);
        Equal(sky.ForCheckpoint(0), sky.ForCheckpoint(9), "Configured sky cadence must remain ten.");
        Require(sky.ForCheckpoint(10) != sky.ForCheckpoint(9), "Configured sky cadence missed its boundary.");
        Equal(sky.ForCheckpoint(34), sky.ForCheckpoint(35), "The 35-checkpoint hull boundary must not force a sky change.");

        var perCheckpoint = new SkySelection(71, 10, 1);
        Require(perCheckpoint.ForCheckpoint(1) != perCheckpoint.ForCheckpoint(0), "A one-checkpoint cadence did not change.");
    }

    private static void SkyInputBounds()
    {
        foreach (int count in new[] { 0, 1, 33 })
            Throws<ArgumentOutOfRangeException>(() => new SkySelection(0, count), "Invalid texture count was accepted.");
        foreach (int cadence in new[] { 0, -1 })
            Throws<ArgumentOutOfRangeException>(() => new SkySelection(0, 10, cadence), "Invalid cadence was accepted.");
        Throws<ArgumentOutOfRangeException>(() => new SkySelection(0).ForCheckpoint(-1), "Negative checkpoint was accepted.");
        foreach (int count in new[] { 2, 32 })
        {
            int value = new SkySelection(0, count, 1).ForCheckpoint(long.MaxValue);
            Require(value >= 0 && value < count, "A supported count or maximum checkpoint was rejected.");
        }
    }

    private static void PickupReset()
    {
        var state = new PickupState();
        AssertInactive(state);
        state.Collect(PowerupKind.Rocket, 10);
        state.Collect(PowerupKind.Hourglass, 10);
        state.Collect(PowerupKind.Laser, 10);
        state.Synchronize(11);
        state.Reset();
        AssertInactive(state);
        state.Collect(PowerupKind.Rocket, 0);
        Near(5, state.RocketRemaining, "Reset must start a new clock epoch.");
    }

    private static void AssertInactive(PickupState state)
    {
        Require(!state.RocketActive && !state.LaserCharging, "An effect remained active after reset.");
        Near(0, state.RocketRemaining, "Rocket timer remained after reset.");
        Near(0, state.SlowRemaining, "Hourglass timer remained after reset.");
        Near(0, state.LaserChargeRemaining, "Laser timer remained after reset.");
        Near(1, state.SpeedMultiplier, "Reset must remove Rocket's multiplier.");
        Near(1, state.MovementScale, "Reset must remove Hourglass's movement scale.");
        Require(double.IsPositiveInfinity(state.NextTransitionTime(0)), "Reset retained a pending transition.");
    }

    private static void PickupClock()
    {
        var state = new PickupState();
        state.Collect(PowerupKind.Rocket, 0);
        state.Collect(PowerupKind.Hourglass, 0);
        state.Collect(PowerupKind.Laser, 0);
        Near(2, state.SpeedMultiplier, "Rocket should double the requested speed.");
        Near(.5, state.MovementScale, "Hourglass should halve movement scale.");
        state.Synchronize(2);
        Near(3, state.RocketRemaining, "Rocket duration must consume two supplied seconds.");
        Near(6, state.SlowRemaining, "Hourglass duration must consume two supplied seconds.");
        Near(1, state.LaserChargeRemaining, "Slowed motion must not stretch the laser charge timer.");

        for (int query = 0; query < 20; query++)
        {
            state.Synchronize(2);
            Near(3, state.RocketRemaining, "Repeated same-time queries advanced the clock.");
            Near(6, state.SlowRemaining, "Repeated same-time queries advanced Hourglass.");
            Near(1, state.LaserChargeRemaining, "Repeated same-time queries advanced the laser.");
        }
        state.Synchronize(8);
        Near(0, state.RocketRemaining, "Elapsed Rocket timer must clamp to zero.");
        Near(0, state.SlowRemaining, "Hourglass should end after eight active-play seconds.");
        Near(1, state.SpeedMultiplier, "Expired Rocket multiplier remained active.");
        Near(1, state.MovementScale, "Expired Hourglass movement scale remained active.");
    }

    private static void PickupRefresh()
    {
        var state = new PickupState();
        state.Collect(PowerupKind.Rocket, 0);
        state.Collect(PowerupKind.Hourglass, 0);
        state.Collect(PowerupKind.Laser, 0);
        state.Collect(PowerupKind.Rocket, 2);
        Near(5, state.RocketRemaining, "Repeat Rocket must refresh to five seconds without stacking durations.");
        Near(6, state.SlowRemaining, "Rocket refresh changed Hourglass's expiry.");
        Near(1, state.LaserChargeRemaining, "Rocket refresh changed the laser expiry.");
        state.Collect(PowerupKind.Hourglass, 2);
        Near(8, state.SlowRemaining, "Repeat Hourglass must refresh to eight seconds.");
        state.Collect(PowerupKind.Laser, 2);
        Near(3, state.LaserChargeRemaining, "Repeat Laser must refresh its charge.");
        state.Synchronize(3);
        Require(!state.ResolveLaser(), "The refreshed laser fired at its superseded deadline.");
        state.Synchronize(5);
        Require(state.ResolveLaser(), "The refreshed laser missed its new deadline.");
        Near(2, state.RocketRemaining, "Laser resolution changed Rocket's expiry.");
        Near(5, state.SlowRemaining, "Laser resolution changed Hourglass's expiry.");
    }

    private static void LaserSingleResolution()
    {
        var state = new PickupState();
        state.Collect(PowerupKind.Laser, 0);
        state.Synchronize(2.9);
        Require(!state.ResolveLaser(), "Laser fired before charge completed.");
        Require(state.LaserCharging, "A premature resolution request cancelled the laser.");
        state.Synchronize(3);
        Require(state.ResolveLaser(), "Laser failed at its exact deadline.");
        Require(!state.ResolveLaser(), "The same laser charge resolved twice.");
        Require(!state.LaserCharging, "The resolved laser remains marked charging.");
        Near(0, state.LaserChargeRemaining, "The resolved laser retained a charge timer.");
        state.Collect(PowerupKind.Laser, 3);
        state.Synchronize(100);
        Require(state.ResolveLaser(), "A late resolver must still resolve a completed charge.");
        Require(!state.ResolveLaser(), "Late resolution fired twice.");
    }

    private static void PickupTransitions()
    {
        var state = new PickupState();
        Require(double.IsPositiveInfinity(state.NextTransitionTime(0)), "An empty state has a transition.");
        state.Collect(PowerupKind.Hourglass, 0);
        Near(8, state.NextTransitionTime(0), "Hourglass expiry was not scheduled.");
        state.Collect(PowerupKind.Rocket, 0);
        Near(5, state.NextTransitionTime(0), "Rocket's earlier expiry was not selected.");
        state.Collect(PowerupKind.Laser, 0);
        Near(3, state.NextTransitionTime(0), "Laser's earlier expiry was not selected.");
        state.Synchronize(3);
        Require(state.ResolveLaser(), "Laser should resolve at the next transition.");
        Near(5, state.NextTransitionTime(3), "The resolved laser masked Rocket's expiry.");
        state.Synchronize(5);
        Near(8, state.NextTransitionTime(5), "The expired Rocket masked Hourglass's expiry.");
        state.Synchronize(8);
        Require(double.IsPositiveInfinity(state.NextTransitionTime(8)), "Expired effects remained scheduled.");
    }

    private static void PickupClockRejection()
    {
        var state = new PickupState();
        state.Collect(PowerupKind.Rocket, 0);
        state.Synchronize(4);
        foreach (double invalid in new[] { 3d, -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Throws<ArgumentOutOfRangeException>(() => state.Synchronize(invalid), "Invalid clock was accepted.");
            Near(1, state.RocketRemaining, "A rejected clock mutated state.");
        }
        Throws<ArgumentOutOfRangeException>(() => state.Collect(PowerupKind.Hourglass, 3), "Collection accepted a backwards clock.");
        Near(0, state.SlowRemaining, "Rejected collection started a temporary effect.");
        state.Synchronize(4);
        Near(1, state.RocketRemaining, "A same-time synchronization must remain valid.");
    }

    private static void Require(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal(int expected, int actual, string message)
        => Require(expected == actual, $"{message} Expected {expected}, actual {actual}.");

    private static void Near(double expected, double actual, string message)
        => Require(double.IsFinite(actual) && Math.Abs(expected - actual) <= .00001,
            $"{message} Expected {expected}, actual {actual}.");

    private static void Throws<T>(Action action, string message) where T : Exception
    {
        assertions++;
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException(message);
    }
}
