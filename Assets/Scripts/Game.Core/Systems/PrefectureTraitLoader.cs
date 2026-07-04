using System.Collections.Generic;
using Game.Core.Data;

namespace Game.Core.Systems
{
    /// <summary>
    /// Parses docs/prefecture_traits.csv (pref_id,name,trait,confidence) into a
    /// prefecture-name → TraitType map. Blank trait cells are omitted (the caller
    /// defaults them to <see cref="TraitType.None"/>). Keyed by name, not id, so
    /// it stays correct regardless of the adjacency table's row order.
    ///
    /// Clean-room / public-source data; confidence tiers per docs/校正指南_README.md.
    /// Pure C#: the Unity layer feeds the CSV as a TextAsset string, so this runs
    /// under both dotnet and the Unity Test Runner.
    /// </summary>
    public static class PrefectureTraitLoader
    {
        public static Dictionary<string, TraitType> FromCsv(string csv)
        {
            var result = new Dictionary<string, TraitType>();
            if (string.IsNullOrEmpty(csv)) return result;

            var lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            bool headerSkipped = false;
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimStart('﻿'); // strip UTF-8 BOM
                if (line.Length == 0) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }

                var cols = line.Split(',');
                if (cols.Length < 3) continue;
                var name = cols[1].Trim();
                if (name.Length == 0) continue;

                var trait = ParseTrait(cols[2].Trim());
                if (trait != TraitType.None) result[name] = trait;
            }
            return result;
        }

        private static TraitType ParseTrait(string s)
        {
            switch (s)
            {
                case "金山": return TraitType.GoldMine;
                case "神社": return TraitType.Shrine;
                case "森林": return TraitType.Forest;
                case "沼地": return TraitType.Swamp;
                case "城": return TraitType.Castle;
                case "河川": return TraitType.River;
                case "忍の里": return TraitType.NinjaVillage;
                case "刀鍛冶": return TraitType.SwordSmith;
                case "霊場": return TraitType.SpiritGround;
                case "鉄砲鍛冶": return TraitType.GunSmith;
                default: return TraitType.None;
            }
        }
    }
}
