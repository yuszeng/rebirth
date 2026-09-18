namespace Rebirth.Content;

/// <summary>升级选项数据，导出为 .tres 资源。由 UpgradeService 加权抽取。</summary>
[GlobalClass]
public partial class UpgradeOptionData : Resource
{
    [Export] public string Id { get; set; } = ""; // 唯一标识
    [Export] public string DisplayName { get; set; } = ""; // 按钮标题
    [Export] public string Description { get; set; } = ""; // 按钮描述
    [Export] public StatType Stat { get; set; } = StatType.Attack; // 影响的属性
    [Export] public float Flat { get; set; } // 固定加值
    [Export] public float Percent { get; set; } // 百分比加值
    [Export] public float Weight { get; set; } = 1f; // 抽取权重

    /// <summary>转为 StatModifier，sourceId 由调用方传入以保证唯一。</summary>
    public StatModifier ToModifier(string sourceId)
    {
        return new StatModifier
        {
            SourceId = sourceId,
            Stat = Stat,
            Flat = Flat,
            Percent = Percent,
        };
    }
}
