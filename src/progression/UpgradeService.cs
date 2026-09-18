namespace Rebirth.Progression;

/// <summary>从数据池抽选项。UI 只展示返回值，不写死三种属性。</summary>
public sealed class UpgradeService
{
    public List<UpgradeOptionData> Pool { get; set; } = []; // 可选升级项全集
    public int OfferCount { get; set; } = 3; // 每次弹出几个选项

    /// <summary>加权随机抽取 OfferCount 个不重复升级项。</summary>
    public List<UpgradeOptionData> Offer()
    {
        var result = new List<UpgradeOptionData>(); // 本次提供的选项
        if (Pool.Count == 0)
        {
            return result;
        }

        var remaining = Pool.ToList(); // 尚未被选中的候选
        var take = Math.Min(OfferCount, remaining.Count); // 实际抽取数量
        for (var i = 0; i < take; i++)
        {
            var weights = remaining.Select(o => o.Weight).ToList(); // 与 remaining 对齐的权重
            var picked = GameRng.Instance.WeightedPick(remaining, weights);
            result.Add(picked);
            remaining.Remove(picked); // 不重复
        }

        return result;
    }
}
