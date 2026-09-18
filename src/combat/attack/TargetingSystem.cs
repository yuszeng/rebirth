namespace Rebirth.Combat;

/// <summary>目标选择策略。未实现的策略回退到最近目标。</summary>
public enum TargetingStrategy
{
    Nearest, // 最近
    LowestHp, // 血量最低
    HighestHp, // 血量最高
    BossPriority, // 优先 Boss（Phase 2+）
    NearestElite, // 最近精英（Phase 2+）
    Random, // 随机
}

/// <summary>目标选择与具体武器解耦。未实现的策略回退到最近目标。</summary>
public static class TargetingSystem
{
    /// <summary>从候选节点中筛选射程内目标，按策略排序后取前 maxTargets 个。</summary>
    public static List<Combatant> Select(
        Vector2 origin,
        float originRadius,
        IEnumerable<Node> candidates,
        float attackRange,
        TargetingStrategy strategy,
        int maxTargets)
    {
        var inRange = new List<Combatant>(); // 射程内存活目标
        foreach (var node in candidates)
        {
            // 重开当帧旧节点可能仍留在组里，访问已释放实例会打断整次攻击
            if (node is not Combatant combatant || !GodotObject.IsInstanceValid(combatant))
            {
                continue;
            }

            if (combatant.Health == null || combatant.Health.IsDead)
            {
                continue;
            }

            // 射程按「攻击距离 + 双方半径」计算，避免贴身怪因碰撞分离被判出圈
            var reach = attackRange + originRadius + combatant.Radius;
            if (origin.DistanceTo(combatant.GlobalPosition) > reach)
            {
                continue;
            }

            inRange.Add(combatant);
        }

        switch (strategy)
        {
            case TargetingStrategy.LowestHp:
                inRange.Sort((a, b) => a.Health.Current.CompareTo(b.Health.Current));
                break;
            case TargetingStrategy.HighestHp:
                inRange.Sort((a, b) => b.Health.Current.CompareTo(a.Health.Current));
                break;
            case TargetingStrategy.Random:
                inRange = GameRng.Instance.Shuffle(inRange);
                break;
            default:
                // 默认及 BossPriority / NearestElite 暂按距离排序
                inRange.Sort((a, b) =>
                    origin.DistanceSquaredTo(a.GlobalPosition).CompareTo(origin.DistanceSquaredTo(b.GlobalPosition)));
                break;
        }

        var take = Math.Max(1, maxTargets);
        if (take < inRange.Count)
        {
            inRange.RemoveRange(take, inRange.Count - take);
        }

        return inRange;
    }
}
