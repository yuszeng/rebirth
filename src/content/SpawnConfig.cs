namespace Rebirth.Content;

/// <summary>刷怪参数配置，导出为 .tres 资源。</summary>
[GlobalClass]
public partial class SpawnConfig : Resource
{
    [Export] public EnemyData? Enemy { get; set; } // 刷出的敌人类型
    [Export] public float Interval { get; set; } = 1.15f; // 刷怪间隔（秒）
    [Export] public int MaxAlive { get; set; } = 40; // 同屏存活上限
    [Export] public float SpawnDistanceMin { get; set; } = 420f; // 相对玩家的最近刷怪距离
    [Export] public float SpawnDistanceMax { get; set; } = 560f; // 相对玩家的最远刷怪距离
}
