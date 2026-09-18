namespace Rebirth.Combat;

/// <summary>自动攻击循环。未来技能释放器并列挂在角色上，而不是改 Player。</summary>
public partial class AttackController : Node
{
    [Export] public NodePath WeaponPath { get; set; } = new("../Weapon"); // 武器节点路径

    Combatant? _owner; // 所属战斗单位（父节点）
    Weapon? _weapon; // 武器实例
    WeaponData? _weaponData; // 缓存武器配置，场景重载后可重新绑定
    float _cooldown; // 攻击冷却剩余时间（秒）

    public override void _Ready()
    {
        BindNodes();
    }

    /// <summary>绑定父节点与武器，并应用已缓存的武器数据。</summary>
    public void Setup(WeaponData? weaponData)
    {
        _weaponData = weaponData; // 缓存武器配置
        BindNodes(); // 绑定父节点与武器
        _weapon?.Configure(weaponData); // 应用已缓存的武器数据
    }

    /// <summary>绑定父节点与武器，并应用已缓存的武器数据。</summary>
    void BindNodes()
    {
        _owner = GetParent() as Combatant;
        // 用父节点下的固定路径查找，比 ../Weapon 更可靠（重载场景后 NodePath 偶发解析失败）
        _weapon = GetParent()?.GetNodeOrNull<Weapon>("Weapon");
    }

    /// <summary>重开时若武器节点已重建但 Data 未绑定，每帧尝试补绑。</summary>
    void EnsureWeaponReady()
    {
        if (_weapon == null || !GodotObject.IsInstanceValid(_weapon)) // 如果武器不存在或无效则重新绑定
        {
            BindNodes();
        }

        if (_weapon?.Data == null && _weaponData != null && _weapon != null) // 如果武器数据不存在且缓存武器配置存在且武器存在则配置武器
        {
            _weapon.Configure(_weaponData); // 配置武器
        }
    }

    // 物理过程处理：每帧检查是否可以攻击
    public override void _PhysicsProcess(double delta)
    {
        if (GameManager.Instance.State != GameState.InRun)
        {
            return; // 如果不是本局进行中则直接返回
        }

        EnsureWeaponReady(); // 确保武器准备好

        if (_owner?.Health == null || _owner.Health.IsDead || _weapon?.Data == null)
        {
            return; // 如果所属战斗单位的健康状态不存在或已死亡或武器数据不存在则直接返回
        }

        _cooldown = Math.Max(_cooldown - (float)delta, 0f); // 冷却时间减少
        if (_cooldown > 0f)
        {
            return; // 冷却时间未结束时直接返回
        }

        var attackSpeed = Math.Max(_owner.Stats.GetValue(StatType.AttackSpeed), 0.05f); // 防止除零
        if (_weapon.TryAttack(_owner)) // 尝试攻击
        {
            _cooldown = 1f / attackSpeed; // 攻速越高冷却越短
        }
    }
}
