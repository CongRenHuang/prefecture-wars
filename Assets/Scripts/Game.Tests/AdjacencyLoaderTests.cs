using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Game.Core.Data;
using Game.Core.Systems;

namespace Game.Tests
{
    /// <summary>
    /// Tests for AdjacencyLoader — parses the clean-room adjacency table into
    /// PrefectureData, enforces graph symmetry, and applies the 確認-tier
    /// in-game deviations (docs/校正指南_README.md, CLAUDE.md).
    /// Fixtures are inline CSV so the same tests run under both dotnet and the
    /// Unity Test Runner (no dependency on a docs/ file path).
    /// </summary>
    public class AdjacencyLoaderTests
    {
        // Mirrors docs/adjacency_draft.csv header + row shape:
        //   pref, neighbors(現實基準 pipe-separated), original_deviation, status
        private const string Header =
            "pref,neighbors(現實基準),original_deviation(原作已知偏差),status\n";

        private static PrefectureData Find(IReadOnlyList<PrefectureData> prefs, string name)
            => prefs.First(p => p.Name == name);

        private static HashSet<string> NeighborNames(IReadOnlyList<PrefectureData> prefs, string name)
        {
            var byId = prefs.ToDictionary(p => p.Id, p => p.Name);
            return Find(prefs, name).NeighborIds.Select(id => byId[id]).ToHashSet();
        }

        private static bool Adjacent(IReadOnlyList<PrefectureData> prefs, string a, string b)
            => NeighborNames(prefs, a).Contains(b) && NeighborNames(prefs, b).Contains(a);

        [Test]
        public void ParsesRows_WithSequentialIds()
        {
            var csv = Header +
                      "青森,岩手,,x\n" +
                      "岩手,青森,,x\n" +
                      "秋田,,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(prefs.Count, Is.EqualTo(3));
            Assert.That(prefs[0].Name, Is.EqualTo("青森"));
            Assert.That(prefs[0].Id, Is.EqualTo(0));
            Assert.That(prefs[2].Name, Is.EqualTo("秋田"));
            Assert.That(prefs[2].Id, Is.EqualTo(2));
        }

        [Test]
        public void SkipsBom_AndBlankLines()
        {
            var csv = "﻿" + Header + "\n青森,岩手,,x\n\n岩手,青森,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(prefs.Count, Is.EqualTo(2));
            Assert.That(prefs[0].Name, Is.EqualTo("青森"));
        }

        [Test]
        public void EnforcesSymmetry_WhenNeighborListedOnlyOneWay()
        {
            // 青森 lists 岩手; 岩手 lists nobody. Loader must add the reverse edge.
            var csv = Header +
                      "青森,岩手,,x\n" +
                      "岩手,,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(Adjacent(prefs, "青森", "岩手"), Is.True);
        }

        [Test]
        public void SkipsUnknownNeighborName_WithoutCrashing()
        {
            var csv = Header + "青森,ZZ不存在,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(prefs.Count, Is.EqualTo(1));
            Assert.That(prefs[0].NeighborIds, Is.Empty);
        }

        [Test]
        public void EmptyNeighbors_YieldsIsolatedPrefecture()
        {
            // 沖縄 case: no confirmed land link (推定 sea-link stays out).
            var csv = Header +
                      "沖縄,,現實無陸路;推定鹿児島,x\n" +
                      "鹿児島,宮崎,,x\n" +
                      "宮崎,鹿児島,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(Find(prefs, "沖縄").NeighborIds, Is.Empty);
        }

        [Test]
        public void Deviation_OsakaKagawa_AddedWhenEnabled()
        {
            // Base data: neither lists the other. Confirmed deviation links them.
            var csv = Header +
                      "大阪,京都,原作中『大阪–香川』隣接[確認],x\n" +
                      "京都,大阪,,x\n" +
                      "香川,徳島,,x\n" +
                      "徳島,香川,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(Adjacent(prefs, "大阪", "香川"), Is.True);
        }

        [Test]
        public void Deviation_OsakaKagawa_AbsentWhenDisabled()
        {
            var csv = Header +
                      "大阪,京都,,x\n" +
                      "京都,大阪,,x\n" +
                      "香川,徳島,,x\n" +
                      "徳島,香川,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv, applyConfirmedDeviations: false);

            Assert.That(Adjacent(prefs, "大阪", "香川"), Is.False);
        }

        [Test]
        public void Deviation_YamaguchiFukuoka_RemovedEvenIfDataListsThem()
        {
            // Guard: real-world data omits this edge, but if a source ever adds
            // it the confirmed deviation must strip it back out.
            var csv = Header +
                      "山口,広島|福岡,原作中『山口–福岡』不隣接[確認],x\n" +
                      "福岡,山口|佐賀,原作中『山口–福岡』不隣接[確認],x\n" +
                      "広島,山口,,x\n" +
                      "佐賀,福岡,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(Adjacent(prefs, "山口", "福岡"), Is.False);
            // sanity: other edges survive the removal
            Assert.That(Adjacent(prefs, "山口", "広島"), Is.True);
        }

        [Test]
        public void Deviation_MysteryIslands_LinkHokkaidoIshikawaNagasaki()
        {
            // 三座小島 connect 北海道/石川/長崎 → all three mutually adjacent.
            var csv = Header +
                      "北海道,青森,謎之島小島連接點[確認],x\n" +
                      "青森,北海道,,x\n" +
                      "石川,富山,謎之島小島連接點[確認],x\n" +
                      "富山,石川,,x\n" +
                      "長崎,佐賀,謎之島小島連接點[確認],x\n" +
                      "佐賀,長崎,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);

            Assert.That(Adjacent(prefs, "北海道", "石川"), Is.True);
            Assert.That(Adjacent(prefs, "石川", "長崎"), Is.True);
            Assert.That(Adjacent(prefs, "北海道", "長崎"), Is.True);
        }

        [Test]
        public void GraphIsSymmetric_ForEveryEdge()
        {
            var csv = Header +
                      "青森,岩手|秋田,,x\n" +
                      "岩手,青森,,x\n" +
                      "秋田,青森|岩手,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);
            var byId = prefs.ToDictionary(p => p.Id);

            foreach (var p in prefs)
                foreach (var nId in p.NeighborIds)
                    Assert.That(byId[nId].NeighborIds, Contains.Item(p.Id),
                        $"{p.Name}->{byId[nId].Name} not mirrored");
        }

        [Test]
        public void NeighborIds_AreSortedAndDeduplicated()
        {
            // duplicate neighbor listed twice must collapse to one edge.
            var csv = Header +
                      "青森,秋田|岩手|秋田,,x\n" +
                      "岩手,青森,,x\n" +
                      "秋田,青森,,x\n";

            var prefs = AdjacencyLoader.FromCsv(csv);
            var aomori = Find(prefs, "青森");

            Assert.That(aomori.NeighborIds, Is.Ordered);
            Assert.That(aomori.NeighborIds.Length, Is.EqualTo(aomori.NeighborIds.Distinct().Count()));
        }
    }
}
