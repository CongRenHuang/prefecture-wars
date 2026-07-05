namespace Game.Core.Systems
{
    public enum GameOutcome
    {
        Ongoing,
        PlayerDefeated,
        PlayerVictory,

        /// <summary>
        /// Spectator games only (no player faction): one AI faction owns every
        /// land. Player games end via PlayerVictory/PlayerDefeated instead.
        /// </summary>
        Unified,
    }
}
