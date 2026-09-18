namespace Rebirth.Core;

/// <summary>只属于这一世的进度。跨转生数据不得放这里。</summary>
public partial class RunState : RefCounted
{
    public bool IsAlive { get; set; } = true; // 玩家是否仍存活
    public float ElapsedSeconds { get; set; } // 本局已存活秒数
    public int KillCount { get; set; } // 击杀数
    public int Level { get; set; } = 1; // 当前等级
    public int Xp { get; set; } // 当前等级内的经验值
    public int XpToNext { get; set; } = 10; // 升到下一级所需经验
    public int Gold { get; set; } // 本局累计金币
    public int PendingLevelUps { get; set; } // 待选择的升级次数（连升时 > 1）

    /// <summary>将本局数据转为只读结算快照。</summary>
    public RunResult ToResult()
    {
        return new RunResult
        {
            SurvivedSeconds = ElapsedSeconds,
            KillCount = KillCount,
            Level = Level,
            Gold = Gold,
        };
    }
}
