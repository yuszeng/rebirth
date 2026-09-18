namespace Rebirth.Combat;

/// <summary>一次伤害请求的参数包，统一传入 DamageSystem。</summary>
public sealed class DamageRequest
{
    public Node? Source { get; init; } // 伤害来源节点（攻击者）
    public Combatant? Target { get; init; } // 受伤目标
    public float Amount { get; init; } // 原始伤害数值
    public string[] Tags { get; init; } = []; // 伤害标签（weapon/melee/contact 等，供后续修正器过滤）
}
