namespace Rebirth.Combat;

/// <summary>最小刷怪器。未来 Wave 扩展本节点，而不是在 Main 里另写一套。</summary>
public partial class SpawnDirector : Node
{
    [Export] public SpawnConfig? Config { get; set; } // 刷怪参数配置
    [Export] public PackedScene? EnemyScene { get; set; } // 敌人预制体

    float _elapsed; // 距上次刷怪已过时间（秒）
    readonly List<Enemy> _alive = []; // 当前存活敌人引用（用于上限控制）

    // 物理过程处理：每帧检查是否可以刷怪   
    public override void _PhysicsProcess(double delta)
    {
        if (GameManager.Instance.State != GameState.InRun || Config?.Enemy == null || EnemyScene == null)
        {
            return;
        }

        Prune(); // 清理已死亡或已销毁的引用
        _elapsed += (float)delta;
        if (_alive.Count >= Config.MaxAlive || _elapsed < Config.Interval)
        {
            return;
        }

        _elapsed = 0f;
        SpawnOne();
    }

    /// <summary>在玩家周围随机距离与角度生成一只敌人。</summary>
    void SpawnOne()
    {
        if (Player.FindAlive() is not Player player)
        {
            return;
        }

        if (Config?.Enemy is not EnemyData data || EnemyScene!.Instantiate() is not Enemy enemy)
        {
            return;
        }

        // 贴边时环形刷点会落到墙外，必须与 ArenaBounds 内沿对齐
        enemy.GlobalPosition = ArenaBounds.PickSpawnAround(
            player.GlobalPosition,
            Config.SpawnDistanceMin,
            Config.SpawnDistanceMax,
            data.Radius);
        GetParent().AddChild(enemy);
        enemy.Setup(data);
        _alive.Add(enemy);
    }

    /// <summary>移除无效或已死亡的敌人引用，避免 _alive 无限增长。</summary>
    void Prune()
    {
        _alive.RemoveAll(e => !GodotObject.IsInstanceValid(e) || e.Health.IsDead);
    }
}
