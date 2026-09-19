namespace Rebirth.Character;

/// <summary>玩家角色：移动输入、武器攻击、升级属性应用。</summary>
public partial class Player : Combatant
{
    [Export] public CharacterData? Data { get; set; } // 角色配置数据

    AttackController? _attackController; // 自动攻击控制器
    readonly List<WeaponData> _ownedWeapons = []; // 本局持有的武器，按 E 切换

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
    public void Setup(CharacterData data, WeaponData? weaponOverride = null)
    {
        Data = data; // 设置玩家数据
        var weaponData = weaponOverride ?? data.StartingWeapon; // 开局选择优先于角色模板默认武器
        Stats = data.BuildStats(weaponData); // 选中武器的射程必须进入本局基础属性
        ApplyVisual(data.Color, data.Radius); // 应用视觉
        Health.Setup(Stats.GetValue(StatType.MaxHp)); // 设置健康

        // 直接配置 Weapon 节点，不 sole 依赖 AttackController 缓存
        GetNodeOrNull<Weapon>("Weapon")?.Configure(weaponData); // 配置武器
        _attackController ??= GetNodeOrNull<AttackController>("AttackController"); // 获取攻击控制器
        _attackController?.Setup(weaponData); // 设置攻击控制器
        GetNodeOrNull<SkillController>("SkillController")?.Clear();
        GetNodeOrNull<EquipmentLoadout>("EquipmentLoadout")?.Clear();
        _ownedWeapons.Clear();
        if (weaponData != null && !string.IsNullOrEmpty(weaponData.Id))
        {
            _ownedWeapons.Add(weaponData);
        }
    }

    public EquipmentLoadout? Loadout => GetNodeOrNull<EquipmentLoadout>("EquipmentLoadout");

    public IReadOnlyList<WeaponData> OwnedWeapons => _ownedWeapons;

    public bool HasWeapon(string weaponId) =>
        !string.IsNullOrEmpty(weaponId) && _ownedWeapons.Any(weapon => weapon.Id == weaponId);

    public bool CanGrantWeapon(WeaponData weapon) =>
        !string.IsNullOrEmpty(weapon.Id) && !HasWeapon(weapon.Id);

    /// <summary>把武器加入本局持有列表。同一 Id 不可重复持有，也不在这里切换当前装备。</summary>
    public bool TryGrantWeapon(WeaponData? weapon)
    {
        if (weapon == null || !CanGrantWeapon(weapon))
        {
            return false;
        }

        _ownedWeapons.Add(weapon);
        return true;
    }

    /// <summary>切换当前装备武器。攻击范围基础值跟武器走，商店/升级的范围修饰器仍叠加。</summary>
    public bool TryEquipWeapon(string weaponId)
    {
        var weapon = _ownedWeapons.FirstOrDefault(owned => owned.Id == weaponId);
        if (weapon == null)
        {
            return false;
        }

        GetNodeOrNull<Weapon>("Weapon")?.Configure(weapon);
        _attackController ??= GetNodeOrNull<AttackController>("AttackController");
        _attackController?.Setup(weapon);
        Stats.SetBase(StatType.AttackRange, weapon.Pattern?.Range ?? 78f);
        return true;
    }

    public bool HasSkill(string skillId) =>
        GetNodeOrNull<SkillController>("SkillController")?.Has(skillId) ?? false;

    public bool CanGrantSkill(SkillData skill)
    {
        var controller = GetNodeOrNull<SkillController>("SkillController");
        return controller != null && !controller.Has(skill.Id);
    }

    /// <summary>把技能交给 SkillController，Player 不负责释放或选敌。</summary>
    public bool TryGrantSkill(SkillData skill) =>
        GetNodeOrNull<SkillController>("SkillController")?.TryGrant(skill) ?? false;

    /// <summary>应用升级选项：添加属性修饰器，必要时按比例调整当前血量上限。</summary>
    public void ApplyUpgrade(UpgradeOptionData option)
    {
        var source = $"upgrade_{GameManager.Instance.Run.Level}_{option.Id}"; // 唯一来源 ID，便于日后移除
        ApplyStatModifier(option.ToModifier(source));
    }

    /// <summary>商店/升级共用：改属性。加最大生命时按比例缩放当前血量。</summary>
    public void ApplyStatModifier(StatModifier modifier)
    {
        Stats.AddModifier(modifier);
        SyncHealthToStats();
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
