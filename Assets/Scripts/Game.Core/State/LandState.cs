namespace Game.Core.State
{
    public class LandState
    {
        /// <summary>Index into the PrefectureData table.</summary>
        public int DefId { get; set; }

        public int OwnerFactionId { get; set; }

        /// <summary>MVP-0: single unit type, so garrison is one count.</summary>
        public int Garrison { get; set; }
    }
}
