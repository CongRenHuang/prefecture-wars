using System;
using Game.Core.State;

namespace Game.Core.Systems
{
    /// <summary>
    /// MVP-0 invasion: pure number comparison (pseudo-code doc §4).
    /// MVP-1 replaces the internals with BattleSimCore.Simulate(); the
    /// result contract stays the same.
    /// </summary>
    public static class InvasionResolver
    {
        public readonly struct Result
        {
            public bool Captured { get; }
            public int AttackerSurvivors { get; }

            public Result(bool captured, int attackerSurvivors)
            {
                Captured = captured;
                AttackerSurvivors = attackerSurvivors;
            }
        }

        public static Result Resolve(int attackerFactionId, int attackerUnits, LandState land)
        {
            if (attackerUnits > land.Garrison)
            {
                // Attacker wins: remnant garrisons the captured land.
                int remnant = attackerUnits - land.Garrison;
                land.OwnerFactionId = attackerFactionId;
                land.Garrison = remnant;
                return new Result(captured: true, attackerSurvivors: remnant);
            }

            // Defender holds: loses one per attacker, but ownership can only
            // change via a winning invasion — garrison floor is 1.
            land.Garrison = Math.Max(1, land.Garrison - attackerUnits);
            return new Result(captured: false, attackerSurvivors: 0);
        }
    }
}
