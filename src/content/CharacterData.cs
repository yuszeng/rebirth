namespace Rebirth.Content;

/// <summary>玩家角色模板数据，导出为 .tres 资源。</summary>
[GlobalClass]
public partial class CharacterData : Resource
{
    [Export] public string Id { get; set; } = "player"; // 唯一标识
    [Export] public string DisplayName { get; set; } = "旅人"; // 显示名称
    [Export] public float Radius { get; set; } = 16f; // 碰撞/视觉半径
    [Export] public Color Color { get; set; } = new(0.32f, 0.52f, 0.98f); // 外观颜色
    [Export] public float BaseMaxHp { get; set; } = 100f; // 基础最大生命
    [Export] public float BaseAttack { get; set; } = 12f; // 基础攻击力
    [Export] public float BaseAttackSpeed { get; set; } = 1.2f; // 基础攻速（次/秒）
    [Export] public float BaseMoveSpeed { get; set; } = 220f; // 基础移速
    [Export] public WeaponData? StartingWeapon { get; set; } // 初始武器

    /// <summary>从导出字段构建 CharacterStats 基础属性。</summary>
    public CharacterStats BuildStats()
    {
        var stats = new CharacterStats();
        stats.SetBase(StatType.MaxHp, BaseMaxHp);
        stats.SetBase(StatType.Attack, BaseAttack);
        stats.SetBase(StatType.AttackSpeed, BaseAttackSpeed);
        stats.SetBase(StatType.MoveSpeed, BaseMoveSpeed);
        stats.SetBase(StatType.AttackRange, StartingWeapon?.AttackRange ?? 78f);
        return stats;
    }
}
