namespace Rebirth.Character;

/// <summary>属性修饰器。percent 为 0.1 表示 +10%。</summary>
[GlobalClass]
public partial class StatModifier : Resource
{
    [Export] public string SourceId { get; set; } = ""; // 来源标识（升级/装备/Buff）
    [Export] public StatType Stat { get; set; } = StatType.Attack; // 影响的属性类型
    [Export] public float Flat { get; set; } // 固定加值
    [Export] public float Percent { get; set; } // 百分比加值
}
