namespace Rebirth.Content;

/// <summary>攻击如何被放到场上；只描述生成/运动方式，不描述命中规则。</summary>
public enum AttackDeliveryKind
{
    Preset = 0, // 使用旧 AttackPatternKind 映射，便于资源渐进迁移
    Instant, // 立即结算一次
    Projectile, // 生成飞行体
    AttachedToSource, // 附着在施放者身上，持续短时间检测
    DelayedAtTarget, // 锁定目标位置，延迟后结算
    PersistentOrbit, // 常驻环绕施放者
    Chain, // 命中后跳转到附近目标
}
