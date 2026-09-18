namespace Rebirth.Content;

/// <summary>武器模板数据，导出为 .tres 资源。</summary>
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public string Id { get; set; } = "sword"; // 唯一标识
    [Export] public string DisplayName { get; set; } = "铁剑"; // 显示名称
    [Export] public float BonusDamage { get; set; } // 附加伤害（加在角色 Attack 上）
    [Export] public AttackPatternData? Pattern { get; set; } // 命中方式与攻击表现
    [Export] public string[] DamageTags { get; set; } = ["weapon", "melee"]; // 伤害标签
}
