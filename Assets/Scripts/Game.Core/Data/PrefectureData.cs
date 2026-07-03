namespace Game.Core.Data
{
    /// <summary>
    /// Static prefecture definition — read-only after construction.
    /// The Unity layer will build these from PrefectureDef ScriptableObjects;
    /// runtime state lives in LandState, never here (SO-readonly rule).
    /// </summary>
    public sealed class PrefectureData
    {
        public int Id { get; }
        public string Name { get; }
        public int[] NeighborIds { get; }
        public TraitType Trait { get; }

        public PrefectureData(int id, string name, int[] neighborIds, TraitType trait)
        {
            Id = id;
            Name = name;
            NeighborIds = neighborIds;
            Trait = trait;
        }
    }
}
