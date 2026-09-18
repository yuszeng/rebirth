namespace Rebirth.Character;

/// <summary>玩家角色：移动输入、武器攻击、升级属性应用。</summary>
public partial class Player : Combatant
{
    [Export] public CharacterData? Data { get; set; } // 角色配置数据

    AttackController? _attackController; // 自动攻击控制器

    /// <summary>查找当前仍有效的玩家。重开时组里可能短暂残留已释放实例。</summary>
    public static Player? FindAlive()
    {
        var tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null)
        {
            return null;
        }

        foreach (var node in tree.GetNodesInGroup("player"))
        {
            if (node is Player player && GodotObject.IsInstanceValid(player) && player.Health is { IsDead: false })
            {
                return player;
            }
        }

        return null;
    }

    public override void _Ready()
    {
        AddToGroup("player"); // 供刷怪器、敌人 AI 查找
        CollisionLayer = 1; // 玩家碰撞层
        CollisionMask = 4; // 只与敌人层碰撞
        EnsureHealth(); // 确保健康组件
        _attackController = GetNode<AttackController>("AttackController"); // 获取攻击控制器
        GetNode<Camera2D>("Camera2D").MakeCurrent(); // 激活跟随相机
        if (Data != null) // 如果数据存在则设置
        {
            Setup(Data); // 设置玩家数据
        }
    }

    /// <summary>根据 CharacterData 初始化属性、外观与武器。</summary>
    public void Setup(CharacterData data, WeaponData? weaponFallback = null)
    {
        Data = data; // 设置玩家数据
        var weaponData = data.StartingWeapon ?? weaponFallback; // 获取武器数据
        Stats = data.BuildStats(); // 从数据构建基础属性
        ApplyVisual(data.Color, data.Radius); // 应用视觉
        Health.Setup(Stats.GetValue(StatType.MaxHp)); // 设置健康

        // 直接配置 Weapon 节点，不 sole 依赖 AttackController 缓存
        GetNodeOrNull<Weapon>("Weapon")?.Configure(weaponData); // 配置武器
        _attackController ??= GetNodeOrNull<AttackController>("AttackController"); // 获取攻击控制器
        _attackController?.Setup(weaponData); // 设置攻击控制器
    }

    /// <summary>应用升级选项：添加属性修饰器，必要时按比例调整当前血量上限。</summary>
    public void ApplyUpgrade(UpgradeOptionData option)
    {
        var source = $"upgrade_{GameManager.Instance.Run.Level}_{option.Id}"; // 唯一来源 ID，便于日后移除
        Stats.AddModifier(option.ToModifier(source));
        var newMax = Stats.GetValue(StatType.MaxHp);
        if (!Mathf.IsEqualApprox(Health.Maximum, newMax))
        {
            Health.RetargetMaximum(newMax, preserveRatio: true); // 升级加血时保持当前血量比例
        }
    }

    // 物理过程处理：每帧更新移动速度
    public override void _PhysicsProcess(double _delta)
    {
        if (GameManager.Instance.State != GameState.InRun || Health.IsDead)
        {
            Velocity = Vector2.Zero; // 停止移动
            return;
        }

        var input = Input.GetVector("move_left", "move_right", "move_up", "move_down"); // WASD / 方向键
        Velocity = input * Stats.GetValue(StatType.MoveSpeed); // 更新移动速度
        MoveAndSlide();
    }
}
