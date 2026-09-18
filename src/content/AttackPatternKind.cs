namespace Rebirth.Content;

/// <summary>底层攻击模式。技能、武器、未来魔法都按模式执行，不按具体名称分支。</summary>
public enum AttackPatternKind
{
    InstantCircleAroundSelf = 0, // 瞬发圆形：立即结算自身周围目标
    PointTarget, // 点射：选中目标后即时命中
    SweptSector, // 碰撞扇形：朝目标方向挥扫，由 VFX Hitbox 过程命中
    Projectile, // 投掷物：生成飞行体，飞行命中后结算
    SweptCircleAroundSelf, // 碰撞圆形：自身周围持续一小段时间，由 VFX Hitbox 命中
    InstantSector, // 瞬发扇形：朝目标方向立即结算扇面目标
}
