namespace Game.Core.Data
{
    /// <summary>
    /// Land traits from docs/traits.csv. MVP-0 only needs GoldMine (income);
    /// remaining values reserved so prefecture data can be filled in early.
    /// </summary>
    public enum TraitType
    {
        None = 0,
        GoldMine,   // 金山: income 50 → 70
        Shrine,     // 神社
        Forest,     // 森林
        Swamp,      // 沼地
        Castle,     // 城
        River,      // 河川
        NinjaVillage,   // 忍の里
        SwordSmith,     // 刀鍛冶
        SpiritGround,   // 霊場
        GunSmith,       // 鉄砲鍛冶
    }
}
