namespace Rebirth.Core;

/// <summary>只属于这一世的进度。跨转生数据不得放这里。</summary>
public partial class RunState : RefCounted
{
    public const string BasicAttackModeId = "basic"; // 普通攻击的稳定标识

    public bool IsAlive { get; set; } = true; // 玩家是否仍存活
    public float ElapsedSeconds { get; set; } // 本局已存活秒数
    public int KillCount { get; set; } // 击杀数
    public int Level { get; set; } = 1; // 当前等级
    public int Xp { get; set; } // 当前等级内的经验值
    public int XpToNext { get; set; } = 10; // 升到下一级所需经验
    public int Gold { get; set; } // 本局持有金币（商店会花掉）
    public int PendingLevelUps { get; set; } // 待选择的升级次数（连升时 > 1）
    public int CombatRound { get; set; } = 1; // 当前战斗回合（从 1 起）
    public float RoundElapsedSeconds { get; set; } // 本回合已战斗秒数
    public float RoundDurationSeconds { get; set; } = 60f; // 本回合时长，进商店后重置
    public List<string> OwnedWeaponIds { get; } = []; // 本局已拥有武器 Id（含开局选择），不跨转生
    public List<string> OwnedEquipmentIds { get; } = []; // 本局已购护甲 Id，不跨转生
    public Dictionary<EquipmentSlotKind, string> EquippedEquipmentIds { get; } = []; // 护甲槽 → 装备 Id；武器槽用 SelectedAttackModeId
    public string SelectedAttackModeId { get; set; } = BasicAttackModeId; // 当前攻击方式：已装备武器 Id

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
