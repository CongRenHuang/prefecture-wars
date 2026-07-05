using System;
using System.Collections.Generic;
using Game.Core.Data;

namespace Game.Presentation.Defs
{
    /// <summary>
    /// Converts PrefectureDef ScriptableObjects into plain Game.Core
    /// PrefectureData (SO-readonly rule: the simulation never touches the
    /// assets; every array is cloned). Validates that ids form a dense 0..n-1
    /// range because PrefectureData ids double as faction ids and land indices.
    /// </summary>
    public static class MapBuilder
    {
        public static IReadOnlyList<PrefectureData> Build(IReadOnlyList<PrefectureDef> defs)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));

            var result = new PrefectureData[defs.Count];
            foreach (var def in defs)
            {
                if (def == null)
                    throw new ArgumentException("null PrefectureDef in the list", nameof(defs));
                if (def.Id < 0 || def.Id >= defs.Count)
                    throw new ArgumentException(
                        $"PrefectureDef '{def.PrefName}' id {def.Id} outside 0..{defs.Count - 1}", nameof(defs));
                if (result[def.Id] != null)
                    throw new ArgumentException(
                        $"duplicate prefecture id {def.Id} ('{result[def.Id].Name}' vs '{def.PrefName}')", nameof(defs));

                // Clone the neighbor array — runtime must never share storage
                // with a ScriptableObject asset.
                var neighbors = def.NeighborIds == null
                    ? Array.Empty<int>()
                    : (int[])def.NeighborIds.Clone();

                result[def.Id] = new PrefectureData(def.Id, def.PrefName, neighbors, def.Trait);
            }
            return result;
        }
    }
}
