using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.Config;
using Game.Core.Data;
using Game.Core.Ports;
using Game.Core.State;
using Game.Core.Systems;

namespace Game.Tests
{
    /// <summary>Deterministic IRandom for tests: returns queued values.</summary>
    public class FakeRandom : IRandom
    {
        private readonly Queue<int> _ints = new Queue<int>();
        public FakeRandom(params int[] values) { foreach (var v in values) _ints.Enqueue(v); }
        public int Next(int minInclusive, int maxExclusive) =>
            _ints.Count > 0 ? _ints.Dequeue() : minInclusive;
        public double NextDouble() => 0.0;
    }

    public class SimpleAiTests
    {
        // MVP-0 AI (MVP doc §四): attack the weakest adjacent enemy land,
        // only if we outnumber it; send garrison - 1 (must leave 1 behind).

        private static (SimpleAi ai, GameState state) BuildWorld(int aiGarrison)
        {
            var prefectures = new[]
            {
                new PrefectureData(0, "AI縣", new[] { 1, 2 }, TraitType.None),
                new PrefectureData(1, "弱縣", new[] { 0 }, TraitType.None),
                new PrefectureData(2, "強縣", new[] { 0 }, TraitType.None),
            };
            var state = new GameState
            {
                Factions =
                {
                    new Faction { Id = 0, Money = 100, IsAlive = true, IsPlayer = false },
                    new Faction { Id = 1, Money = 100, IsAlive = true, IsPlayer = true },
                    new Faction { Id = 2, Money = 100, IsAlive = true, IsPlayer = true },
                },
                Lands =
                {
                    new LandState { DefId = 0, OwnerFactionId = 0, Garrison = aiGarrison },
                    new LandState { DefId = 1, OwnerFactionId = 1, Garrison = 2 },
                    new LandState { DefId = 2, OwnerFactionId = 2, Garrison = 10 },
                },
            };
            var ai = new SimpleAi(new GameConfig(), prefectures, new FakeRandom());
            return (ai, state);
        }

        [Test]
        public void TakeTurn_AttacksWeakestAdjacentEnemy_WhenStronger()
        {
            var (ai, state) = BuildWorld(aiGarrison: 6);

            ai.TakeTurn(state, factionId: 0);

            // sends 5 (6-1) against weakest (garrison 2) → captures, remnant 3
            Assert.AreEqual(0, state.Lands[1].OwnerFactionId);
            Assert.AreEqual(3, state.Lands[1].Garrison);
            Assert.AreEqual(1, state.Lands[0].Garrison); // 1 left behind
        }

        [Test]
        public void TakeTurn_NoWinnableTarget_DoesNotAttack()
        {
            var (ai, state) = BuildWorld(aiGarrison: 2); // can send 1, weakest has 2

            ai.TakeTurn(state, factionId: 0);

            // No invasion happened (hiring at home is allowed and tested separately).
            Assert.AreEqual(1, state.Lands[1].OwnerFactionId);
            Assert.AreEqual(2, state.Lands[2].OwnerFactionId);
        }

        [Test]
        public void TakeTurn_HiresWithSpareMoney()
        {
            var (ai, state) = BuildWorld(aiGarrison: 2);
            var config = new GameConfig();

            ai.TakeTurn(state, factionId: 0);

            // Couldn't attack → spends money on hiring at home
            Assert.Less(state.Factions[0].Money, 100);
            Assert.Greater(state.Lands[0].Garrison, 2);
        }
    }
}
