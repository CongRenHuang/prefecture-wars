using System;
using System.Linq;
using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    /// <summary>
    /// Integration smoke: 5-prefecture map, all factions AI-driven, run days
    /// until one faction owns everything. Proves the core loop terminates and
    /// composes — same headless path MVP-2 uses for AI-vs-AI battles.
    /// </summary>
    public class FullGameSmokeTests
    {
        private static PrefectureData[] MiniMap() => new[]
        {
            new PrefectureData(0, "北海道", new[] { 1 }, TraitType.GoldMine),
            new PrefectureData(1, "青森", new[] { 0, 2, 3 }, TraitType.None),
            new PrefectureData(2, "岩手", new[] { 1, 3, 4 }, TraitType.None),
            new PrefectureData(3, "秋田", new[] { 1, 2, 4 }, TraitType.None),
            new PrefectureData(4, "宮城", new[] { 2, 3 }, TraitType.None),
        };

        [Test]
        public void AiVsAi_FiveCounties_ReachesUnificationWithinDayLimit()
        {
            var config = new GameConfig();
            var map = MiniMap();
            var state = GameFactory.NewGame(config, map, playerPrefectureId: 0);
            var turn = new TurnSystem(config, map);
            var rng = new SeededRandom(12345);
            var ai = new SimpleAi(config, map, rng);

            const int dayLimit = 500;
            bool unified = false;

            for (int day = 0; day < dayLimit; day++)
            {
                turn.StartNewDay(state);
                foreach (var faction in state.Factions)
                    if (faction.IsAlive) ai.TakeTurn(state, faction.Id);
                var outcome = turn.Settlement(state);

                if (outcome != GameOutcome.Ongoing)
                {
                    unified = true;
                    break;
                }
            }

            Assert.IsTrue(unified, $"game did not end within {dayLimit} days (stalemate?)");
        }

        // A ring of N identical prefectures. With no trait asymmetry every
        // faction plays identically, so garrisons stay equal and the strict-'>'
        // capture rule can never fire — this is exactly why the full 47-map
        // stalemated. One goldmine gives its owner faster income, so it caps
        // first and starts capturing, which cascades.
        private static PrefectureData[] Ring(int n, int goldmineId = -1)
        {
            var arr = new PrefectureData[n];
            for (int i = 0; i < n; i++)
            {
                var neigh = new[] { (i - 1 + n) % n, (i + 1) % n };
                Array.Sort(neigh);
                arr[i] = new PrefectureData(i, "P" + i, neigh,
                    i == goldmineId ? TraitType.GoldMine : TraitType.None);
            }
            return arr;
        }

        private static int RunAndCountAlive(PrefectureData[] map, int dayLimit)
        {
            var config = new GameConfig();
            var state = GameFactory.NewGame(config, map, playerPrefectureId: 0);
            var turn = new TurnSystem(config, map);
            var ai = new SimpleAi(config, map, new SeededRandom(12345));

            for (int day = 0; day < dayLimit; day++)
            {
                turn.StartNewDay(state);
                foreach (var faction in state.Factions)
                    if (faction.IsAlive) ai.TakeTurn(state, faction.Id);
                if (turn.Settlement(state) != GameOutcome.Ongoing) break;
            }
            return state.Factions.Count(f => f.IsAlive);
        }

        [Test]
        public void SymmetricRing_NoTraits_Stalemates()
        {
            // No faction can ever out-grow a neighbor → nobody is eliminated.
            Assert.AreEqual(6, RunAndCountAlive(Ring(6), dayLimit: 300),
                "a fully symmetric ring must stalemate (this is the 47-map bug)");
        }

        [Test]
        public void SymmetricRing_OneGoldmine_BreaksStalemate()
        {
            // The goldmine owner's income edge breaks symmetry → captures happen.
            Assert.Less(RunAndCountAlive(Ring(6, goldmineId: 0), dayLimit: 300), 6,
                "a goldmine should let its owner start conquering neighbors");
        }
    }
}
