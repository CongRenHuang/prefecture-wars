using Game.Core.Data;
using UnityEngine;

namespace Game.Presentation.Defs
{
    /// <summary>
    /// Read-only prefecture definition asset (MVP plan: SO defs are data only).
    /// Runtime code must NEVER mutate these — Play Mode edits to a
    /// ScriptableObject persist to the asset. <see cref="MapBuilder"/> copies
    /// them into plain Game.Core PrefectureData for the simulation.
    ///
    /// Assets are generated from the clean-room CSVs by
    /// Tools > Prefecture Wars > Generate PrefectureDefs (Editor menu), so the
    /// CSVs stay the single source of truth.
    /// </summary>
    [CreateAssetMenu(menuName = "Prefecture Wars/Prefecture Def", fileName = "PrefectureDef")]
    public sealed class PrefectureDef : ScriptableObject
    {
        [SerializeField] private int id;
        [SerializeField] private string prefName;
        [SerializeField] private int[] neighborIds;
        [SerializeField] private TraitType trait;

        public int Id => id;
        public string PrefName => prefName;
        /// <summary>Do not mutate; MapBuilder clones it for runtime use.</summary>
        public int[] NeighborIds => neighborIds;
        public TraitType Trait => trait;

#if UNITY_EDITOR
        /// <summary>Editor-only: used by the CSV generator to (re)fill assets.</summary>
        public void EditorInit(int newId, string newName, int[] newNeighborIds, TraitType newTrait)
        {
            id = newId;
            prefName = newName;
            neighborIds = newNeighborIds;
            trait = newTrait;
        }
#endif
    }
}
