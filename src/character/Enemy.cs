namespace Rebirth.Character;

/// <summary>敌人：追逐玩家并在接触范围内造成伤害。</summary>
public partial class Enemy : Combatant
{
    public EnemyData? Data { get; private set; } // 敌人模板数据

    readonly ChaseMovement _movement = new(); // 追逐移动逻辑
    readonly ContactAttack _contact = new(); // 接触伤害逻辑
    WorldHealthBar? _healthBar; // 头顶血条


    public override void _Ready()
    {
        AddToGroup("enemies"); // 供武器目标选择查找
        CollisionLayer = 2; // 敌人碰撞层
        CollisionMask = 4; // 与玩家和其他敌人碰撞
        EnsureHealth(); // 确保健康组件
        EventBus.Instance.ActorDied += OnActorDied; // 订阅死亡事件
    }

    public override void _ExitTree() // 退出树时取消订阅死亡事件
    {
        if (EventBus.Instance != null) // 如果事件总线存在则取消订阅死亡事件
        {
            EventBus.Instance.ActorDied -= OnActorDied; // 取消订阅死亡事件
        }
    }

    /// <summary>根据 EnemyData 初始化属性与外观。</summary>
    public void Setup(EnemyData data) // 设置敌人数据
    {
        Data = data; // 设置敌人数据
        Stats = data.BuildStats(); // 构建属性
        ApplyVisual(data.Color, data.Radius); // 应用视觉
        Health.Setup(Stats.GetValue(StatType.MaxHp)); // 设置健康
        EnsureHealthBar(data.Radius); // 确保健康条
    }

    /// <summary>确保头顶血条存在并绑定当前 Health。</summary>
    void EnsureHealthBar(float radius)
    {
        _healthBar ??= GetNodeOrNull<WorldHealthBar>("HealthBar") ?? new WorldHealthBar { Name = "HealthBar" }; // 获取血条节点或创建新节点
        if (_healthBar.GetParent() == null) // 如果血条没有父节点则添加为子节点
        {
            AddChild(_healthBar); // 添加为子节点
        }

        _healthBar.Bind(Health, radius); // 绑定健康和半径
    }

    /// <summary>物理过程处理：移动和接触伤害。</summary>
    public override void _PhysicsProcess(double delta)
    {
        if (GameManager.Instance.State != GameState.InRun || Health.IsDead) // 如果游戏状态不是进行中或健康已死亡则直接返回
        {
            return;
        }

        if (Player.FindAlive() is not Player player) // 如果玩家不存在则直接返回
        {
            return;
        }

        _movement.Tick(this, player.GlobalPosition, Stats.GetValue(StatType.MoveSpeed)); // 移动
        // 接触范围 = 敌人接触距离 + 玩家半径，避免视觉重叠才扣血
        _contact.Tick((float)delta, this, player, Data!.AttackInterval, Data.ContactRange + player.Radius); // 接触
    }

    /// <summary>自身死亡时从场景树移除节点。</summary>
    void OnActorDied(Combatant victim, Node? _source)
    {
        if (victim == this) // 如果受害者是自己则释放节点
        {
            QueueFree(); // 释放节点
        }
    }
}
