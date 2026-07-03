using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    public class TurnSystemTests
    {
        // Minimal 2-prefecture world: land 0 plain, land 1 goldmine.
        // Faction 0 = player, faction 1 = AI.
        private static (TurnSystem turn, GameState state) BuildWorld()
        {
            var prefectures = new[]
            {
                new PrefectureData(0, "平原縣", new[] { 1 }, TraitType.None),
                new PrefectureData(1, "金山縣", new[] { 0 }, TraitType.GoldMine),
            };
            var config = new GameConfig();
            var state = new GameState
            {
                Day = 0,
                Factions =
                {
                    new Faction { Id = 0, Money = 100, IsAlive = true, IsPlayer = true },
                    new Faction { Id = 1, Money = 100, IsAlive = true, IsPlayer = false },
                },
                Lands =
                {
                    new LandState { DefId = 0, OwnerFactionId = 0, Garrison = 3 },
                    new LandState { DefId = 1, OwnerFactionId = 1, Garrison = 3 },
                },
            };
            return (new TurnSystem(config, prefectures), state);
        }

        [Test]
        public void StartNewDay_IncrementsDay()
        {
            var (turn, state) = BuildWorld();

            turn.StartNewDay(state);

            Assert.AreEqual(1, state.Day);
        }

        [Test]
        public void StartNewDay_PlainLand_Adds50()
        {
            var (turn, state) = BuildWorld();

            turn.StartNewDay(state);

            Assert.AreEqual(150, state.Factions[0].Money);
        }

        [Test]
        public void StartNewDay_GoldMine_Adds70()
        {
            var (turn, state) = BuildWorld();

            turn.StartNewDay(state);

            Assert.AreEqual(170, state.Factions[1].Money);
        }

        [Test]
        public void StartNewDay_MultipleLands_SumsIncome()
        {
            var (turn, state) = BuildWorld();
            state.Lands[1].OwnerFactionId = 0; // player owns both: 50 + 70

            turn.StartNewDay(state);

            Assert.AreEqual(220, state.Factions[0].Money);
        }

        [Test]
        public void StartNewDay_DeadFaction_GetsNoIncome()
        {
            var (turn, state) = BuildWorld();
            state.Factions[1].IsAlive = false;

            turn.StartNewDay(state);

            Assert.AreEqual(100, state.Factions[1].Money);
        }
    }
}
