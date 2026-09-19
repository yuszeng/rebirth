namespace Rebirth.Content;

/// <summary>攻击模式预设。底层执行会展开成 Delivery / HitShape / HitPolicy 组合。</summary>
public enum AttackPatternKind
{
    InstantCircleAroundSelf = 0, // 瞬发圆形：立即结算自身周围目标
    PointTarget, // 点射：选中目标后即时命中
    SweptSector, // 碰撞扇形：朝目标方向挥扫，由 VFX Hitbox 过程命中
    Projectile, // 投掷物：生成飞行体，飞行命中后结算
    SweptCircleAroundSelf, // 碰撞圆形：自身周围持续一小段时间，由 VFX Hitbox 命中
    InstantSector, // 瞬发扇形：朝目标方向立即结算扇面目标
    PiercingProjectile, // 贯穿突刺：朝目标方向用前方碰撞区域命中多个不同目标
    ScatterProjectile, // 散射：朝目标方向按扇形发射多枚投射物
    ChainJump, // 连锁：首轮目标命中后，继续跳向附近未命中的目标
    SustainedBeam, // 持续光束：一段时间内沿直线按间隔结算伤害
    DelayedExplosion, // 延迟爆炸：锁定落点并预警，延迟后造成圆形伤害
    OrbitingSatellites, // 环绕卫星：固定生成围绕攻击者旋转的伤害实体
}
