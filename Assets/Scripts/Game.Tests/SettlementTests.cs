using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    public class SettlementTests
    {
        // 3-prefecture world, 3 factions (faction 0 = player), one land each.
        private static (TurnSystem turn, GameState state) BuildWorld()
        {
            var prefectures = new[]
            {
                new PrefectureData(0, "甲縣", new[] { 1 }, TraitType.None),
                new PrefectureData(1, "乙縣", new[] { 0, 2 }, TraitType.None),
                new PrefectureData(2, "丙縣", new[] { 1 }, TraitType.None),
            };
            var state = new GameState
            {
                Factions =
                {
                    new Faction { Id = 0, Money = 100, IsAlive = true, IsPlayer = true },
                    new Faction { Id = 1, Money = 100, IsAlive = true, IsPlayer = false },
                    new Faction { Id = 2, Money = 100, IsAlive = true, IsPlayer = false },
                },
                Lands =
                {
                    new LandState { DefId = 0, OwnerFactionId = 0, Garrison = 3 },
                    new LandState { DefId = 1, OwnerFactionId = 1, Garrison = 3 },
                    new LandState { DefId = 2, OwnerFactionId = 2, Garrison = 3 },
                },
            };
            return (new TurnSystem(new GameConfig(), prefectures), state);
        }

        [Test]
        public void Settlement_FactionWithNoLand_IsEliminated()
        {
            var (turn, state) = BuildWorld();
            state.Lands[2].OwnerFactionId = 0; // faction 2 lost its only land

            var result = turn.Settlement(state);

            Assert.IsFalse(state.Factions[2].IsAlive);
            Assert.AreEqual(GameOutcome.Ongoing, result);
        }

        [Test]
        public void Settlement_FactionWithLand_StaysAlive()
        {
            var (turn, state) = BuildWorld();

            var result = turn.Settlement(state);

            Assert.IsTrue(state.Factions[0].IsAlive);
            Assert.IsTrue(state.Factions[1].IsAlive);
            Assert.IsTrue(state.Factions[2].IsAlive);
            Assert.AreEqual(GameOutcome.Ongoing, result);
        }

        [Test]
        public void Settlement_PlayerLosesAllLand_GameOver()
        {
            var (turn, state) = BuildWorld();
            state.Lands[0].OwnerFactionId = 1; // player's only land taken

            var result = turn.Settlement(state);

            Assert.IsFalse(state.Factions[0].IsAlive);
            Assert.AreEqual(GameOutcome.PlayerDefeated, result);
        }

        [Test]
        public void Settlement_PlayerOwnsAll_Victory()
        {
            var (turn, state) = BuildWorld();
            state.Lands[1].OwnerFactionId = 0;
            state.Lands[2].OwnerFactionId = 0;

            var result = turn.Settlement(state);

            Assert.AreEqual(GameOutcome.PlayerVictory, result);
        }
    }
}
