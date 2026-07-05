using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    /// <summary>
    /// Tests for PlayerCommands — the validated click-to-invade / hire API the
    /// Presentation layer calls. Rules covered: ownership, adjacency,
    /// keep-1-home, grayed-out (once per land per day), capture vs repel.
    /// </summary>
    public class PlayerCommandsTests
    {
        // 0-1 adjacent, 1-2 adjacent, 0-2 NOT adjacent.
        private static (GameState state, PrefectureData[] prefs) BuildWorld(
            int playerGarrison = 5, int enemyGarrison = 2)
        {
            var prefs = new[]
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
                    new LandState { DefId = 0, OwnerFactionId = 0, Garrison = playerGarrison },
                    new LandState { DefId = 1, OwnerFactionId = 1, Garrison = enemyGarrison },
                    new LandState { DefId = 2, OwnerFactionId = 2, Garrison = 3 },
                },
            };
            return (state, prefs);
        }

        [Test]
        public void TryInvade_Valid_CapturesAndMarksActed()
        {
            var (state, prefs) = BuildWorld(playerGarrison: 5, enemyGarrison: 2);
            var acted = new HashSet<int>();

            var err = PlayerCommands.TryInvade(state, prefs, 0, 0, 1, acted, out var result);

            Assert.AreEqual(PlayerCommands.InvadeError.None, err);
            Assert.IsTrue(result.Captured);
            Assert.AreEqual(0, state.Lands[1].OwnerFactionId);
            Assert.AreEqual(2, state.Lands[1].Garrison); // 4 sent - 2 defenders
            Assert.AreEqual(1, state.Lands[0].Garrison); // 1 left home
            Assert.IsTrue(acted.Contains(0));
        }

        [Test]
        public void TryInvade_Valid_CanBeRepelled()
        {
            var (state, prefs) = BuildWorld(playerGarrison: 3, enemyGarrison: 10);
            var acted = new HashSet<int>();

            var err = PlayerCommands.TryInvade(state, prefs, 0, 0, 1, acted, out var result);

            Assert.AreEqual(PlayerCommands.InvadeError.None, err, "a losing attack is still legal");
            Assert.IsFalse(result.Captured);
            Assert.AreEqual(1, state.Lands[1].OwnerFactionId); // defender holds
            Assert.AreEqual(8, state.Lands[1].Garrison);       // 10 - 2 attackers
            Assert.IsTrue(acted.Contains(0));
        }

        [Test]
        public void TryInvade_SourceNotOwned()
        {
            var (state, prefs) = BuildWorld();

            var err = PlayerCommands.TryInvade(state, prefs, 0, 1, 0, new HashSet<int>(), out _);

            Assert.AreEqual(PlayerCommands.InvadeError.SourceNotOwned, err);
        }

        [Test]
        public void TryInvade_SourceAlreadyActed()
        {
            var (state, prefs) = BuildWorld();
            var acted = new HashSet<int> { 0 };

            var err = PlayerCommands.TryInvade(state, prefs, 0, 0, 1, acted, out _);

            Assert.AreEqual(PlayerCommands.InvadeError.SourceAlreadyActed, err);
            Assert.AreEqual(5, state.Lands[0].Garrison, "no units moved");
        }

        [Test]
        public void TryInvade_NotEnoughUnits()
        {
            var (state, prefs) = BuildWorld(playerGarrison: 1);

            var err = PlayerCommands.TryInvade(state, prefs, 0, 0, 1, new HashSet<int>(), out _);

            Assert.AreEqual(PlayerCommands.InvadeError.NotEnoughUnits, err);
        }

        [Test]
        public void TryInvade_TargetNotEnemy_WhenOwn()
        {
            var (state, prefs) = BuildWorld();
            state.Lands[1].OwnerFactionId = 0; // now own land

            var err = PlayerCommands.TryInvade(state, prefs, 0, 0, 1, new HashSet<int>(), out _);

            Assert.AreEqual(PlayerCommands.InvadeError.TargetNotEnemy, err);
        }

        [Test]
        public void TryInvade_NotAdjacent()
        {
            var (state, prefs) = BuildWorld();

            var err = PlayerCommands.TryInvade(state, prefs, 0, 0, 2, new HashSet<int>(), out _);

            Assert.AreEqual(PlayerCommands.InvadeError.NotAdjacent, err);
            Assert.AreEqual(5, state.Lands[0].Garrison, "no units moved");
        }

        [Test]
        public void HireMax_SpendsMoney_UpToCapAndBudget()
        {
            var (state, _) = BuildWorld(playerGarrison: 5);
            var config = new GameConfig(); // HireCost 20, cap 25; money 100 → 5 affordable

            int hired = PlayerCommands.HireMax(config, state, 0, 0);

            Assert.AreEqual(5, hired);
            Assert.AreEqual(10, state.Lands[0].Garrison);
            Assert.AreEqual(0, state.Factions[0].Money);
        }

        [Test]
        public void HireMax_RespectsLandCap()
        {
            var (state, _) = BuildWorld(playerGarrison: 24);
            state.Factions[0].Money = 1000;
            var config = new GameConfig();

            int hired = PlayerCommands.HireMax(config, state, 0, 0);

            Assert.AreEqual(1, hired, "only 1 slot free below the 25 cap");
            Assert.AreEqual(25, state.Lands[0].Garrison);
        }

        [Test]
        public void HireMax_NotOwnedLand_HiresNothing()
        {
            var (state, _) = BuildWorld();
            var config = new GameConfig();

            Assert.AreEqual(0, PlayerCommands.HireMax(config, state, 0, landIndex: 1));
        }
    }
}
