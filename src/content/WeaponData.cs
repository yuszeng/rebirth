namespace Rebirth.Content;

/// <summary>武器模板数据，导出为 .tres 资源。</summary>
[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public string Id { get; set; } = "sword"; // 唯一标识
    [Export] public string DisplayName { get; set; } = "铁剑"; // 显示名称
    [Export] public float BonusDamage { get; set; } // 附加伤害（加在角色 Attack 上）
    [Export] public float AttackRange { get; set; } = 78f; // 默认攻击范围
    [Export] public TargetingStrategy Targeting { get; set; } = TargetingStrategy.Nearest; // 点选策略（也用于扇形瞄准）
    [Export] public int MaxTargets { get; set; } = 3; // 点选时最多命中几个；范围伤害不截断
    [Export] public string[] DamageTags { get; set; } = ["weapon", "melee"]; // 伤害标签
    [Export] public AreaHitKind HitShape { get; set; } = AreaHitKind.PointTargets; // 命中形状
    [Export] public AttackVisualKind Visual { get; set; } = AttackVisualKind.Beam; // 无 AttackVfx 时的回退表现
    [Export] public PackedScene? AttackVfx { get; set; } // 攻击特效场景，换刀光优先换这个
    [Export] public Texture2D? AttackVfxTexture { get; set; } // 覆盖场景里的 Blade 贴图
    [Export] public float SlashArcDegrees { get; set; } = 110f; // 扇形判定与挥砍张角
    [Export] public float SlashDuration { get; set; } = 0.14f; // 特效时长（秒）
}
