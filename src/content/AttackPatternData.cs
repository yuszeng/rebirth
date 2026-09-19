namespace Rebirth.Content;

/// <summary>攻击模式参数。伤害数值由武器/技能提供，这里只描述生成、命中形状与命中策略。</summary>
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

    [ExportGroup("Composition")]
    [Export] public AttackDeliveryKind Delivery { get; set; } = AttackDeliveryKind.Preset;
    [Export] public AttackHitShapeKind HitShape { get; set; } = AttackHitShapeKind.Preset;
    [Export] public AttackHitPolicyKind HitPolicy { get; set; } = AttackHitPolicyKind.Preset;

    [ExportGroup("Piercing")]
    /// <summary>贯穿突刺最多命中的不同目标数；0 表示只受射程限制。ProjectileRadius 复用为突刺碰撞半宽。</summary>
    [Export(PropertyHint.Range, "0,64,1")] public int PierceCount { get; set; } = 3;

    [ExportGroup("Scatter")]
    [Export(PropertyHint.Range, "1,64,1")] public int ScatterCount { get; set; } = 5;
    [Export(PropertyHint.Range, "0,360,1")] public float ScatterArcDegrees { get; set; } = 45f;

    [ExportGroup("Chain")]
    /// <summary>首轮目标数使用 MaxTargets；此处表示首轮之后继续扩散的轮数。</summary>
    [Export(PropertyHint.Range, "0,16,1")] public int ChainJumps { get; set; } = 2;
    /// <summary>上一轮每个目标最多连接的附近新目标数；整次攻击不会重复命中同一目标。</summary>
    [Export(PropertyHint.Range, "1,16,1")] public int ChainTargetsPerJump { get; set; } = 1;
    [Export] public float ChainJumpRange { get; set; } = 120f;
    [Export(PropertyHint.Range, "0,1,0.05")] public float ChainDamageMultiplier { get; set; } = 0.75f;

    [ExportGroup("Beam")]
    [Export] public float BeamWidth { get; set; } = 18f;
    [Export] public float BeamTickInterval { get; set; } = 0.2f;
    /// <summary>光束持续时间使用通用 Duration；每个 tick 造成基础伤害乘以下倍率。</summary>
    [Export(PropertyHint.Range, "0,10,0.05")] public float BeamTickDamageScale { get; set; } = 0.25f;

    [ExportGroup("Delayed Explosion")]
    [Export] public float Delay { get; set; } = 0.8f;
    [Export] public float EffectRadius { get; set; } = 64f;

    [ExportGroup("Orbit")]
    [Export(PropertyHint.Range, "1,32,1")] public int OrbitCount { get; set; } = 3;
    [Export] public float OrbitRadius { get; set; } = 64f;
    [Export] public float SatelliteRadius { get; set; } = 10f;
    [Export] public float OrbitSpeedDegrees { get; set; } = 180f;
    [Export] public float OrbitHitInterval { get; set; } = 0.2f;
    /// <summary>同一 tick 内多个卫星不会重复伤害同一目标。</summary>
    [Export(PropertyHint.Range, "0,10,0.05")] public float OrbitHitDamageScale { get; set; } = 0.5f;
}
