using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
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
    }
}
