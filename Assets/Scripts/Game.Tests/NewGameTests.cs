using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Systems;

namespace Game.Tests
{
    public class NewGameTests
    {
        // Start state per docs/校正指南_README.md (確認 tier):
        // every prefecture starts as its own faction with 3 sword units and 100 yen.

        private static PrefectureData[] MiniMap() => new[]
        {
            // Tōhoku 5-prefecture slice, adjacency from docs/adjacency_draft.csv
            new PrefectureData(0, "北海道", new[] { 1 }, TraitType.GoldMine),
            new PrefectureData(1, "青森", new[] { 0, 2, 3 }, TraitType.None),
            new PrefectureData(2, "岩手", new[] { 1, 3, 4 }, TraitType.None),
            new PrefectureData(3, "秋田", new[] { 1, 2, 4 }, TraitType.None),
            new PrefectureData(4, "宮城", new[] { 2, 3 }, TraitType.None),
        };

        [Test]
        public void NewGame_OneFactionPerPrefecture_OwningItsOwnLand()
        {
            var state = GameFactory.NewGame(new GameConfig(), MiniMap(), playerPrefectureId: 2);

            Assert.AreEqual(5, state.Factions.Count);
            Assert.AreEqual(5, state.Lands.Count);
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(i, state.Lands[i].DefId);
                Assert.AreEqual(i, state.Lands[i].OwnerFactionId);
            }
        }

        [Test]
        public void NewGame_StartValues_100Yen3UnitsDay0()
        {
            var state = GameFactory.NewGame(new GameConfig(), MiniMap(), playerPrefectureId: 2);

            Assert.AreEqual(0, state.Day);
            foreach (var f in state.Factions)
            {
                Assert.AreEqual(100, f.Money);
                Assert.IsTrue(f.IsAlive);
            }
            foreach (var land in state.Lands)
                Assert.AreEqual(3, land.Garrison);
        }

        [Test]
        public void NewGame_OnlyChosenPrefectureIsPlayer()
        {
            var state = GameFactory.NewGame(new GameConfig(), MiniMap(), playerPrefectureId: 2);

            Assert.IsTrue(state.Factions[2].IsPlayer);
            Assert.AreEqual(1, state.Factions.FindAll(f => f.IsPlayer).Count);
        }
    }
}
