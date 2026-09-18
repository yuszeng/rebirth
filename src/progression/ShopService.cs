namespace Rebirth.Progression;

/// <summary>商店货架：加权抽商品、刷新、占位售出。不解析属性/技能内部逻辑。</summary>
public sealed class ShopService
{
    public const string DefaultPoolDirectory = "res://content/shop";

    public List<ShopItemData> Pool { get; set; } = [];
    public ShopConfig Config { get; set; } = new();

    readonly List<ShopItemData?> _slots = [];
    int _refreshCount;

    public int RefreshPrice =>
        Math.Max(0, Config.RefreshBasePrice + _refreshCount * Config.RefreshPriceStep);

    public static List<ShopItemData> LoadPool(string directory = DefaultPoolDirectory) =>
        ContentDirectory.LoadAll<ShopItemData>(directory);

    /// <summary>进入商店时重新上架，刷新次数归零。</summary>
    public void OpenNewVisit()
    {
        _refreshCount = 0;
        FillSlots();
    }

    public ShopStock Snapshot(RunState run) => new()
    {
        CombatRound = run.CombatRound,
        Gold = run.Gold,
        RefreshPrice = RefreshPrice,
        Slots = _slots.ToList(),
    };

    /// <summary>花费金币换一批货。金币不足则失败。</summary>
    public bool TryRefresh(Wallet wallet, RunState run)
    {
        if (!wallet.TrySpend(run, RefreshPrice))
        {
            return false;
        }

        _refreshCount += 1;
        FillSlots();
        return true;
    }

    /// <summary>购买指定槽位。成功后该槽清空并返回商品。</summary>
    public ShopItemData? TryBuy(int slotIndex, Wallet wallet, RunState run)
    {
        if (slotIndex < 0 || slotIndex >= _slots.Count)
        {
            return null;
        }

        var item = _slots[slotIndex];
        if (item == null || !wallet.TrySpend(run, item.Price))
        {
            return null;
        }

        _slots[slotIndex] = null;
        return item;
    }

    void FillSlots()
    {
        _slots.Clear();
        var offerCount = Math.Max(0, Config.OfferCount);
        if (Pool.Count == 0 || offerCount == 0)
        {
            return;
        }

        var remaining = Pool.ToList();
        var take = Math.Min(offerCount, remaining.Count);
        for (var i = 0; i < take; i++)
        {
            var weights = remaining.Select(item => item.Weight).ToList();
            var picked = GameRng.Instance.WeightedPick(remaining, weights);
            _slots.Add(picked);
            remaining.Remove(picked);
        }
    }
}
