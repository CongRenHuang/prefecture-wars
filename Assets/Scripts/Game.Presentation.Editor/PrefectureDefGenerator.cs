using System.IO;
using Game.Core.Systems;
using Game.Presentation.Defs;
using UnityEditor;
using UnityEngine;

namespace Game.Presentation.Editor
{
    /// <summary>
    /// Generates/updates the 47 PrefectureDef assets from the clean-room CSVs
    /// (Resources/adjacency_draft.csv + Resources/prefecture_traits.csv), via
    /// the same AdjacencyLoader/PrefectureTraitLoader the runtime uses — the
    /// CSVs stay the single source of truth; rerun after editing them.
    /// Idempotent: existing assets are refilled in place (GUIDs stable).
    /// </summary>
    public static class PrefectureDefGenerator
    {
        private const string AdjacencyPath = "Assets/Resources/adjacency_draft.csv";
        private const string TraitsPath = "Assets/Resources/prefecture_traits.csv";
        private const string OutputFolder = "Assets/Defs/Prefectures";

        [MenuItem("Tools/Prefecture Wars/Generate PrefectureDefs from CSV")]
        public static void Generate()
        {
            var adjacency = AssetDatabase.LoadAssetAtPath<TextAsset>(AdjacencyPath);
            var traitsCsv = AssetDatabase.LoadAssetAtPath<TextAsset>(TraitsPath);
            if (adjacency == null || traitsCsv == null)
            {
                Debug.LogError($"[PrefectureDefGenerator] CSV missing: " +
                               $"{AdjacencyPath} or {TraitsPath}");
                return;
            }

            var traits = PrefectureTraitLoader.FromCsv(traitsCsv.text);
            var map = AdjacencyLoader.FromCsv(adjacency.text, applyConfirmedDeviations: true, traits: traits);

            Directory.CreateDirectory(OutputFolder);

            int created = 0, updated = 0;
            foreach (var pref in map)
            {
                var path = $"{OutputFolder}/Pref_{pref.Id:D2}_{pref.Name}.asset";
                var def = AssetDatabase.LoadAssetAtPath<PrefectureDef>(path);
                if (def == null)
                {
                    def = ScriptableObject.CreateInstance<PrefectureDef>();
                    def.EditorInit(pref.Id, pref.Name, pref.NeighborIds, pref.Trait);
                    AssetDatabase.CreateAsset(def, path);
                    created++;
                }
                else
                {
                    def.EditorInit(pref.Id, pref.Name, pref.NeighborIds, pref.Trait);
                    EditorUtility.SetDirty(def);
                    updated++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PrefectureDefGenerator] {map.Count} prefectures → " +
                      $"{created} created, {updated} updated in {OutputFolder}.");
        }
    }
}
