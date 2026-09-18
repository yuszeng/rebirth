namespace Rebirth.Progression;

/// <summary>当前货架快照，只给 UI 展示。空槽表示已卖出。</summary>
public sealed class ShopStock
{
    public int CombatRound { get; init; }
    public int Gold { get; init; }
    public int RefreshPrice { get; init; }
    public IReadOnlyList<ShopItemData?> Slots { get; init; } = [];
}
