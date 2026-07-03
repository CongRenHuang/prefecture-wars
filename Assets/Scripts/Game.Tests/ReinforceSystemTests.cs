using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    public class ReinforceSystemTests
    {
        // Moving garrison between two owned adjacent lands.
        // Sender must keep at least 1 unit (land can't be emptied).

        private static (ReinforceSystem sys, GameState state) BuildWorld()
        {
            var prefectures = new[]
            {
                new PrefectureData(0, "甲縣", new[] { 1 }, TraitType.None),
                new PrefectureData(1, "乙縣", new[] { 0, 2 }, TraitType.None),
                new PrefectureData(2, "丙縣", new[] { 1 }, TraitType.None), // NOT adjacent to 0
            };
            var state = new GameState
            {
                Factions =
                {
                    new Faction { Id = 0, Money = 100, IsAlive = true, IsPlayer = true },
                    new Faction { Id = 1, Money = 100, IsAlive = true, IsPlayer = false },
                },
                Lands =
                {
                    new LandState { DefId = 0, OwnerFactionId = 0, Garrison = 5 },
                    new LandState { DefId = 1, OwnerFactionId = 0, Garrison = 3 },
                    new LandState { DefId = 2, OwnerFactionId = 1, Garrison = 3 },
                },
            };
            return (new ReinforceSystem(new GameConfig(), prefectures), state);
        }

        [Test]
        public void Move_BetweenOwnedAdjacentLands_TransfersUnits()
        {
            var (sys, state) = BuildWorld();
            var ok = sys.Move(state, factionId: 0, fromLand: 0, toLand: 1, count: 3);

            Assert.IsTrue(ok);
            Assert.AreEqual(2, state.Lands[0].Garrison);
            Assert.AreEqual(6, state.Lands[1].Garrison);
        }

        [Test]
        public void Move_WouldEmptySender_Refused()
        {
            var (sys, state) = BuildWorld();
            var ok = sys.Move(state, factionId: 0, fromLand: 0, toLand: 1, count: 5);

            Assert.IsFalse(ok);
            Assert.AreEqual(5, state.Lands[0].Garrison);
        }

        [Test]
        public void Move_NotAdjacent_Refused()
        {
            var (sys, state) = BuildWorld();
            state.Lands[2].OwnerFactionId = 0; // own it, but 0 and 2 not adjacent

            var ok = sys.Move(state, factionId: 0, fromLand: 0, toLand: 2, count: 1);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Move_TargetNotOwned_Refused()
        {
            var (sys, state) = BuildWorld();
            state.Lands[1].OwnerFactionId = 1;

            var ok = sys.Move(state, factionId: 0, fromLand: 0, toLand: 1, count: 1);

            Assert.IsFalse(ok);
        }

        [Test]
        public void Move_WouldExceed25Cap_Refused()
        {
            var (sys, state) = BuildWorld();
            state.Lands[1].Garrison = 24;

            var ok = sys.Move(state, factionId: 0, fromLand: 0, toLand: 1, count: 2);

            Assert.IsFalse(ok);
            Assert.AreEqual(24, state.Lands[1].Garrison);
        }
    }
}
