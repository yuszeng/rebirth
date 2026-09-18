namespace Rebirth.Core;

/// <summary>一局结束时的只读结算。未来转生据此更新 PersistentState。</summary>
public partial class RunResult : RefCounted
{
    public float SurvivedSeconds { get; set; } // 存活时长（秒）
    public int KillCount { get; set; } // 击杀数
    public int Level { get; set; } = 1; // 最终等级
    public int Gold { get; set; } // 获得金币
}
