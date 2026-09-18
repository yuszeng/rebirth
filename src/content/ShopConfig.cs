namespace Rebirth.Content;

/// <summary>商店规则：货架数量与刷新价格，不含具体商品效果。</summary>
[GlobalClass]
public partial class ShopConfig : Resource
{
    [Export] public int OfferCount { get; set; } = 4; // 每次上架几个商品
    [Export] public int RefreshBasePrice { get; set; } = 5; // 首次刷新价格
    [Export] public int RefreshPriceStep { get; set; } = 5; // 每次刷新后加价
}
