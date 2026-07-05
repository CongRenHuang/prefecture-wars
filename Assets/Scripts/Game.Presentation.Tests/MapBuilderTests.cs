using System;
using NUnit.Framework;
using Game.Core.Data;
using Game.Presentation.Defs;
using UnityEngine;

namespace Game.Presentation.Tests
{
    /// <summary>
    /// EditMode tests for MapBuilder (SO → PrefectureData copy). Uses in-memory
    /// ScriptableObject.CreateInstance — no assets on disk needed. Lives in its
    /// own Editor-only asmdef because it needs UnityEngine, unlike Game.Tests
    /// which also runs under the plain dotnet harness.
    /// </summary>
    public class MapBuilderTests
    {
        private static PrefectureDef Def(int id, string name, int[] neighbors,
            TraitType trait = TraitType.None)
        {
            var def = ScriptableObject.CreateInstance<PrefectureDef>();
            def.EditorInit(id, name, neighbors, trait);
            return def;
        }

        [Test]
        public void Build_CopiesFields_AndOrdersById()
        {
            var defs = new[] // deliberately out of order
            {
                Def(1, "乙縣", new[] { 0 }),
                Def(0, "甲縣", new[] { 1 }, TraitType.GoldMine),
            };

            var map = MapBuilder.Build(defs);

            Assert.AreEqual(2, map.Count);
            Assert.AreEqual("甲縣", map[0].Name);
            Assert.AreEqual(TraitType.GoldMine, map[0].Trait);
            Assert.AreEqual("乙縣", map[1].Name);
            CollectionAssert.AreEqual(new[] { 0 }, map[1].NeighborIds);
        }

        [Test]
        public void Build_ClonesNeighborArrays_SoRuntimeCannotTouchTheAsset()
        {
            var soArray = new[] { 1 };
            var defs = new[] { Def(0, "甲縣", soArray), Def(1, "乙縣", new[] { 0 }) };

            var map = MapBuilder.Build(defs);
            map[0].NeighborIds[0] = 999; // runtime mutation...

            Assert.AreEqual(1, soArray[0], "...must not reach the SO's array");
        }

        [Test]
        public void Build_NullNeighbors_BecomeEmpty()
        {
            var map = MapBuilder.Build(new[] { Def(0, "甲縣", null) });

            Assert.AreEqual(0, map[0].NeighborIds.Length);
        }

        [Test]
        public void Build_Throws_OnDuplicateIds()
        {
            var defs = new[] { Def(0, "甲縣", null), Def(0, "乙縣", null) };

            Assert.Throws<ArgumentException>(() => MapBuilder.Build(defs));
        }

        [Test]
        public void Build_Throws_OnIdOutsideRange()
        {
            var defs = new[] { Def(0, "甲縣", null), Def(5, "乙縣", null) };

            Assert.Throws<ArgumentException>(() => MapBuilder.Build(defs));
        }

        [Test]
        public void Build_Throws_OnNullEntry()
        {
            Assert.Throws<ArgumentException>(
                () => MapBuilder.Build(new PrefectureDef[] { null }));
        }
    }
}
