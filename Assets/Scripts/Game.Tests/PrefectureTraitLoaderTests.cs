using System.Linq;
using NUnit.Framework;
using Game.Core.Data;
using Game.Core.Systems;

namespace Game.Tests
{
    /// <summary>
    /// Tests for PrefectureTraitLoader (JP trait string → TraitType, keyed by
    /// name) and its integration with AdjacencyLoader (traits overlaid onto the
    /// parsed PrefectureData). Inline CSV fixtures so the same tests run under
    /// both dotnet and the Unity Test Runner.
    /// </summary>
    public class PrefectureTraitLoaderTests
    {
        private const string TraitHeader = "pref_id,name,trait,confidence\n";
        private const string AdjHeader =
            "pref,neighbors(現實基準),original_deviation(原作已知偏差),status\n";

        [Test]
        public void ParsesJapaneseTraits_ToEnum()
        {
            var csv = TraitHeader +
                      "0,北海道,金山,x\n" +
                      "1,青森,城,x\n" +
                      "2,岩手,神社,x\n" +
                      "3,宮城,忍の里,x\n";

            var traits = PrefectureTraitLoader.FromCsv(csv);

            Assert.AreEqual(TraitType.GoldMine, traits["北海道"]);
            Assert.AreEqual(TraitType.Castle, traits["青森"]);
            Assert.AreEqual(TraitType.Shrine, traits["岩手"]);
            Assert.AreEqual(TraitType.NinjaVillage, traits["宮城"]);
        }

        [Test]
        public void BlankTraitCell_IsOmitted()
        {
            var csv = TraitHeader +
                      "0,北海道,金山,x\n" +
                      "1,青森,,待實測\n";

            var traits = PrefectureTraitLoader.FromCsv(csv);

            Assert.IsTrue(traits.ContainsKey("北海道"));
            Assert.IsFalse(traits.ContainsKey("青森"), "blank trait should default to None (absent)");
        }

        [Test]
        public void SkipsBom_AndHeader()
        {
            var csv = "﻿" + TraitHeader + "0,北海道,金山,x\n";

            var traits = PrefectureTraitLoader.FromCsv(csv);

            Assert.AreEqual(1, traits.Count);
            Assert.AreEqual(TraitType.GoldMine, traits["北海道"]);
        }

        [Test]
        public void UnknownTraitString_IsOmitted()
        {
            var csv = TraitHeader + "0,北海道,謎トラップ,x\n";

            var traits = PrefectureTraitLoader.FromCsv(csv);

            Assert.IsFalse(traits.ContainsKey("北海道"));
        }

        [Test]
        public void EmptyOrNullCsv_YieldsEmptyMap()
        {
            Assert.AreEqual(0, PrefectureTraitLoader.FromCsv(null).Count);
            Assert.AreEqual(0, PrefectureTraitLoader.FromCsv("").Count);
        }

        [Test]
        public void AdjacencyLoader_OverlaysTraits_ByName()
        {
            var adj = AdjHeader +
                      "北海道,青森,,x\n" +
                      "青森,北海道,,x\n";
            var traits = PrefectureTraitLoader.FromCsv(
                TraitHeader + "0,北海道,金山,x\n"); // 青森 omitted

            var prefs = AdjacencyLoader.FromCsv(adj, applyConfirmedDeviations: false, traits: traits);

            Assert.AreEqual(TraitType.GoldMine, prefs.First(p => p.Name == "北海道").Trait);
            Assert.AreEqual(TraitType.None, prefs.First(p => p.Name == "青森").Trait,
                "prefecture absent from the trait map defaults to None");
        }

        [Test]
        public void AdjacencyLoader_NoTraits_DefaultsAllToNone()
        {
            var adj = AdjHeader + "北海道,青森,,x\n青森,北海道,,x\n";

            var prefs = AdjacencyLoader.FromCsv(adj); // traits: null

            Assert.IsTrue(prefs.All(p => p.Trait == TraitType.None));
        }
    }
}
