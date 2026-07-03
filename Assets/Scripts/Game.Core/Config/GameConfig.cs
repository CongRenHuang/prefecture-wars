namespace Game.Core.Config
{
    /// <summary>
    /// Global balance parameters. Values marked 確認 (confirmed) in
    /// docs/校正指南_README.md; a ScriptableObject wrapper will feed this later.
    /// Money is int and must support non-multiples of 50 (POW facility pays
    /// amounts with a 5-yen remainder).
    /// </summary>
    public class GameConfig
    {
        public int IncomePerLand { get; set; } = 50;
        public int IncomeGoldMine { get; set; } = 70;
        public int MaxUnitsPerLand { get; set; } = 25;
        public int StartMoney { get; set; } = 100;
        public int StartUnitsPerLand { get; set; } = 3;

        /// <summary>Sword unit price — 待實測 (placeholder; calibrate in-game later).</summary>
        public int HireCost { get; set; } = 20;
    }
}
