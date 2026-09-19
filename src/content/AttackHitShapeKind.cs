namespace Rebirth.Content;

/// <summary>攻击用什么几何形状找目标；不关心目标被命中几次。</summary>
public enum AttackHitShapeKind
{
    Preset = 0, // 使用旧 AttackPatternKind 映射
    PointTargets, // 由 TargetingSystem 直接选目标
    Circle, // 圆形范围
    Sector, // 扇形范围
    Capsule, // 有宽度的线段，适合突刺和光束
    SatelliteCircles, // 多个环绕圆形碰撞单位
}
