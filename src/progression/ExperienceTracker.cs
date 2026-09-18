namespace Rebirth.Progression;

/// <summary>经验获取与升级判定。升级曲线由 RequiredFor 控制。</summary>
public sealed class ExperienceTracker
{
    /// <summary>增加经验并处理连升，返回本次升了几级。</summary>
    public int Grant(RunState run, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        run.Xp += amount;
        var levels = 0; // 本次连升等级数
        while (run.Xp >= run.XpToNext)
        {
            run.Xp -= run.XpToNext; // 扣除当前级所需经验，溢出保留
            run.Level += 1;
            run.PendingLevelUps += 1; // 每升一级待选 +1
            run.XpToNext = RequiredFor(run.Level); // 更新下一级需求
            levels += 1;
        }

        return levels;
    }

    /// <summary>升到 level 级所需经验。公式：10 + (level-1)*8。</summary>
    public static int RequiredFor(int level) => 10 + (level - 1) * 8;
}
