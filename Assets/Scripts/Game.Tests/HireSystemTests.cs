using NUnit.Framework;
using Game.Core.Config;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    public class HireSystemTests
    {
        // MVP-0: one unit type (sword). Price is 待實測 — lives in GameConfig
        // (HireCost) so calibration only touches config, not logic.

        private static (GameConfig config, GameState state) BuildWorld()
        {
            var config = new GameConfig(); // HireCost default, MaxUnitsPerLand = 25
            var state = new GameState
            {
                Factions = { new Faction { Id = 0, Money = 100, IsAlive = true, IsPlayer = true } },
                Lands = { new LandState { DefId = 0, OwnerFactionId = 0, Garrison = 3 } },
            };
            return (config, state);
        }

        [Test]
        public void Hire_EnoughMoney_AddsUnitAndCharges()
        {
            var (config, state) = BuildWorld();
            var ok = HireSystem.Hire(config, state, factionId: 0, landIndex: 0, count: 2);

            Assert.IsTrue(ok);
            Assert.AreEqual(5, state.Lands[0].Garrison);
            Assert.AreEqual(100 - config.HireCost * 2, state.Factions[0].Money);
        }

        [Test]
        public void Hire_NotEnoughMoney_RefusedNoChange()
        {
            var (config, state) = BuildWorld();
            state.Factions[0].Money = config.HireCost - 1;

            var ok = HireSystem.Hire(config, state, factionId: 0, landIndex: 0, count: 1);

            Assert.IsFalse(ok);
            Assert.AreEqual(3, state.Lands[0].Garrison);
            Assert.AreEqual(config.HireCost - 1, state.Factions[0].Money);
        }

        [Test]
        public void Hire_WouldExceed25Cap_Refused()
        {
            var (config, state) = BuildWorld();
            state.Factions[0].Money = 10_000;
            state.Lands[0].Garrison = 24;

            var ok = HireSystem.Hire(config, state, factionId: 0, landIndex: 0, count: 2);

            Assert.IsFalse(ok);
            Assert.AreEqual(24, state.Lands[0].Garrison);
            Assert.AreEqual(10_000, state.Factions[0].Money);
        }

        [Test]
        public void Hire_OnLandNotOwned_Refused()
        {
            var (config, state) = BuildWorld();
            state.Lands[0].OwnerFactionId = 1;

            var ok = HireSystem.Hire(config, state, factionId: 0, landIndex: 0, count: 1);

            Assert.IsFalse(ok);
        }
    }
}
