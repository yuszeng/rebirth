namespace Rebirth.Content;

/// <summary>攻击命中后如何限制次数和重复结算；伤害计算仍统一交给 DamageSystem。</summary>
public enum AttackHitPolicyKind
{
    Preset = 0, // 使用旧 AttackPatternKind 映射
    SingleHit, // 单个目标命中一次
    MultiHit, // 一次结算多个目标
    PerTargetOnce, // 持续过程里每个目标最多命中一次
    Pierce, // 沿运动或碰撞路径贯穿多个不同目标
    Tick, // 按固定间隔重复结算
    ChainJump, // 命中后跳向附近未命中过的目标
}
