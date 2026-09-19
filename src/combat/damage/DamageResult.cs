namespace Rebirth.Combat;

/// <summary>一次伤害结算结果。飘字与后续效果只读这里，不在攻击脚本里重算。</summary>
public sealed class DamageResult
{
    public required DamageRequest Request { get; init; }
    public float Amount { get; init; }
    public bool IsCrit { get; init; }
    public bool IsDodged { get; init; }
}
