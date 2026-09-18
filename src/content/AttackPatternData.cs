namespace Rebirth.Content;

/// <summary>攻击模式参数。伤害数值由武器/技能提供，这里只描述命中方式。</summary>
[GlobalClass]
public partial class AttackPatternData : Resource
{
    [Export] public AttackPatternKind Kind { get; set; } = AttackPatternKind.PointTarget;
    [Export] public float Range { get; set; } = 80f;
    [Export] public TargetingStrategy Targeting { get; set; } = TargetingStrategy.Nearest;
    [Export] public int MaxTargets { get; set; } = 1;
    [Export] public PackedScene? AttackVfx { get; set; }
    [Export] public Texture2D? AttackVfxTexture { get; set; }
    [Export] public float ArcDegrees { get; set; } = 110f;
    [Export] public float Duration { get; set; } = 0.14f;
    [Export] public float ProjectileSpeed { get; set; } = 360f;
    [Export] public float ProjectileRadius { get; set; } = 8f;
}
