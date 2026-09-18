namespace Rebirth.Combat;

/// <summary>一次攻击模式执行请求。调用方只负责组装上下文，不关心具体命中形状。</summary>
public sealed class AttackPatternRequest
{
    public required Node2D Host { get; init; }
    public required Combatant Source { get; init; }
    public required AttackPatternData Pattern { get; init; }
    public required float Damage { get; init; }
    public string[] DamageTags { get; init; } = [];
    public IEnumerable<Node>? Candidates { get; init; }
    public float? RangeOverride { get; init; }
    public bool Clockwise { get; init; } = true;
}
