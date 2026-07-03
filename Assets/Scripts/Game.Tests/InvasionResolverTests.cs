using NUnit.Framework;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    public class InvasionResolverTests
    {
        // MVP-0 rules (pseudo-code doc §4): pure number compare.
        // attacker > garrison  → capture, remnant = attacker - garrison
        // attacker <= garrison → defender holds

        private static LandState Land(int owner, int garrison) =>
            new LandState { DefId = 0, OwnerFactionId = owner, Garrison = garrison };

        [Test]
        public void Resolve_AttackerStronger_CapturesLand()
        {
            var land = Land(owner: 1, garrison: 3);
            var result = InvasionResolver.Resolve(attackerFactionId: 0, attackerUnits: 5, land);

            Assert.IsTrue(result.Captured);
            Assert.AreEqual(0, land.OwnerFactionId);
            Assert.AreEqual(2, land.Garrison); // 5 - 3 remnant garrisons the land
        }

        [Test]
        public void Resolve_AttackerEqual_DefenderHolds()
        {
            var land = Land(owner: 1, garrison: 4);
            var result = InvasionResolver.Resolve(attackerFactionId: 0, attackerUnits: 4, land);

            Assert.IsFalse(result.Captured);
            Assert.AreEqual(1, land.OwnerFactionId);
        }

        [Test]
        public void Resolve_AttackerWeaker_DefenderHoldsWithLosses()
        {
            var land = Land(owner: 1, garrison: 5);
            var result = InvasionResolver.Resolve(attackerFactionId: 0, attackerUnits: 2, land);

            Assert.IsFalse(result.Captured);
            Assert.AreEqual(1, land.OwnerFactionId);
            Assert.AreEqual(3, land.Garrison); // defender loses attacker's count
            Assert.AreEqual(0, result.AttackerSurvivors); // attackers wiped
        }

        [Test]
        public void Resolve_DefenderHolds_GarrisonNeverBelowOne()
        {
            // Even a total wipe leaves the land held; garrison floor is 1 so
            // ownership can only change via a winning invasion, not attrition.
            var land = Land(owner: 1, garrison: 3);
            InvasionResolver.Resolve(attackerFactionId: 0, attackerUnits: 3, land);

            Assert.GreaterOrEqual(land.Garrison, 1);
            Assert.AreEqual(1, land.OwnerFactionId);
        }
    }
}
