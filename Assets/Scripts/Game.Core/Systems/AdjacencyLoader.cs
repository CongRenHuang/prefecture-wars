using System;
using System.Collections.Generic;
using Game.Core.Data;

namespace Game.Core.Systems
{
    /// <summary>
    /// Parses the clean-room adjacency table (docs/adjacency_draft.csv) into
    /// PrefectureData. Base neighbors come from the real-world column; the
    /// confirmed (確認) in-game deviations are applied as explicit overrides.
    ///
    /// Why overrides instead of parsing the deviation column: those cells are
    /// free-text notes with 確認/推定 tags (docs/校正指南_README.md), not
    /// machine-readable. The confirmed deviations are few and listed in
    /// CLAUDE.md, so they are hardcoded by prefecture name here. 推定/待實測
    /// notes (e.g. 沖縄's sea-link to 鹿児島) are deliberately NOT applied.
    ///
    /// Pure C#: the Unity layer feeds the CSV as a TextAsset string; this stays
    /// UnityEngine-free so it runs under both dotnet and the Unity Test Runner.
    /// </summary>
    public static class AdjacencyLoader
    {
        // Confirmed (確認) edges to add. 三座小島 (mystery islands) connect
        // 北海道/石川/長崎, expressed as pairwise links.
        private static readonly (string A, string B)[] AddEdges =
        {
            ("大阪", "香川"),
            ("北海道", "石川"),
            ("石川", "長崎"),
            ("北海道", "長崎"),
        };

        // Confirmed (確認) edges to remove. Base data already omits 山口–福岡;
        // this is a guard so a future data change can't reintroduce it.
        private static readonly (string A, string B)[] RemoveEdges =
        {
            ("山口", "福岡"),
        };

        public static IReadOnlyList<PrefectureData> FromCsv(
            string csv, bool applyConfirmedDeviations = true)
        {
            if (csv == null) throw new ArgumentNullException(nameof(csv));

            var names = new List<string>();
            var nameToId = new Dictionary<string, int>();
            var rawNeighbors = new List<string[]>();

            var lines = csv.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
            bool headerSkipped = false;
            foreach (var rawLine in lines)
            {
                var line = rawLine.TrimStart('﻿'); // strip UTF-8 BOM
                if (line.Length == 0) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }

                // neighbors column is pipe-separated and never contains commas,
                // so a plain comma split is safe for the first two fields.
                var cols = line.Split(',');
                var name = cols[0].Trim();
                if (name.Length == 0) continue;
                var neigh = cols.Length > 1 ? cols[1].Trim() : string.Empty;

                nameToId[name] = names.Count;
                names.Add(name);
                rawNeighbors.Add(neigh.Length == 0
                    ? Array.Empty<string>()
                    : neigh.Split('|'));
            }

            int n = names.Count;
            var adj = new HashSet<int>[n];
            for (int i = 0; i < n; i++) adj[i] = new HashSet<int>();

            void AddEdge(int a, int b)
            {
                if (a == b) return;
                adj[a].Add(b);
                adj[b].Add(a); // symmetry enforced here
            }

            for (int i = 0; i < n; i++)
            {
                foreach (var nb in rawNeighbors[i])
                {
                    var name = nb.Trim();
                    if (name.Length == 0) continue;
                    if (nameToId.TryGetValue(name, out int j))
                        AddEdge(i, j);
                    // unknown name silently skipped (data may reference a
                    // prefecture outside the loaded subset)
                }
            }

            if (applyConfirmedDeviations)
            {
                foreach (var (a, b) in AddEdges)
                    if (nameToId.TryGetValue(a, out int ia) &&
                        nameToId.TryGetValue(b, out int ib))
                        AddEdge(ia, ib);

                foreach (var (a, b) in RemoveEdges)
                    if (nameToId.TryGetValue(a, out int ia) &&
                        nameToId.TryGetValue(b, out int ib))
                    {
                        adj[ia].Remove(ib);
                        adj[ib].Remove(ia);
                    }
            }

            var result = new PrefectureData[n];
            for (int i = 0; i < n; i++)
            {
                var neighborIds = new int[adj[i].Count];
                adj[i].CopyTo(neighborIds);
                Array.Sort(neighborIds);
                result[i] = new PrefectureData(i, names[i], neighborIds, TraitType.None);
            }
            return result;
        }
    }
}
