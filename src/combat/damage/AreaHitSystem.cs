namespace Rebirth.Combat;

/// <summary>范围判定形状。后续可加矩形、环等，不要在各武器里各自算一遍。</summary>
public enum AreaHitKind
{
    PointTargets, // 由 TargetingSystem 点选，不是范围
    Sector, // 扇形
    Circle, // 圆形
}

/// <summary>一次范围判定的几何参数。Direction 只对扇形有意义。</summary>
public readonly struct AreaShape
{
    public AreaHitKind Kind { get; init; }
    public Vector2 Origin { get; init; }
    public float OriginRadius { get; init; }
    public float Radius { get; init; }
    public Vector2 Direction { get; init; }
    public float ArcDegrees { get; init; }

    public static AreaShape Circle(Vector2 origin, float originRadius, float radius) => new()
    {
        Kind = AreaHitKind.Circle,
        Origin = origin,
        OriginRadius = originRadius,
        Radius = radius,
        Direction = Vector2.Right,
        ArcDegrees = 360f,
    };

    public static AreaShape Sector(
        Vector2 origin,
        float originRadius,
        float radius,
        Vector2 direction,
        float arcDegrees) => new()
    {
        Kind = AreaHitKind.Sector,
        Origin = origin,
        OriginRadius = originRadius,
        Radius = radius,
        Direction = direction.LengthSquared() < 0.0001f ? Vector2.Right : direction,
        ArcDegrees = arcDegrees,
    };
}

/// <summary>范围选敌与批量扣血。扣血仍走 DamageSystem，这里只负责「谁在形状里」。</summary>
public static class AreaHitSystem
{
    /// <summary>筛出形状内、未死亡、且不是 exclude 的 Combatant。</summary>
    public static List<Combatant> Query(AreaShape shape, IEnumerable<Node> candidates, Combatant? exclude = null)
    {
        var hits = new List<Combatant>();
        foreach (var node in candidates)
        {
            if (node is not Combatant combatant || !GodotObject.IsInstanceValid(combatant))
            {
                continue;
            }

            if (combatant == exclude || combatant.Health == null || combatant.Health.IsDead)
            {
                continue;
            }

            if (Overlaps(shape, combatant))
            {
                hits.Add(combatant);
            }
        }

        return hits;
    }

    /// <summary>对已查出的目标逐个走伤害管道。</summary>
    public static void Apply(DamageRequest template, IReadOnlyList<Combatant> targets)
    {
        foreach (var target in targets)
        {
            DamageSystem.Apply(new DamageRequest
            {
                Source = template.Source,
                Target = target,
                Amount = template.Amount,
                Tags = template.Tags,
            });
        }
    }

    /// <summary>
    /// 用「攻击半径 + 双方碰撞半径」判断重叠。
    /// 扇形额外按圆心角过滤，并用目标半径换算成角度余量，避免贴边的怪中心刚出扇面就漏伤。
    /// </summary>
    static bool Overlaps(AreaShape shape, Combatant combatant)
    {
        var offset = combatant.GlobalPosition - shape.Origin;
        var distance = offset.Length();
        var reach = shape.Radius + shape.OriginRadius + combatant.Radius;
        if (distance > reach)
        {
            return false;
        }

        if (shape.Kind != AreaHitKind.Sector)
        {
            return true;
        }

        if (distance <= 0.001f)
        {
            return true;
        }

        var halfArc = Mathf.DegToRad(Mathf.Max(shape.ArcDegrees, 0f)) * 0.5f;
        var angleDelta = Mathf.Abs(Mathf.AngleDifference(offset.Angle(), shape.Direction.Angle()));
        var bodyPad = Mathf.Asin(Mathf.Clamp(combatant.Radius / distance, 0f, 1f));
        return angleDelta <= halfArc + bodyPad;
    }
}
