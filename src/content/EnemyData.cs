namespace Rebirth.Content;

/// <summary>敌人模板数据，导出为 .tres 资源。</summary>
[GlobalClass]
public partial class EnemyData : Resource
{
    [Export] public string Id { get; set; } = "melee_grunt"; // 唯一标识
    [Export] public string DisplayName { get; set; } = "游荡者"; // 显示名称
    [Export] public string[] Tags { get; set; } = ["melee", "basic"]; // 分类标签（供目标策略等使用）
    [Export] public float MaxHp { get; set; } = 24f; // 最大生命
    [Export] public float MoveSpeed { get; set; } = 95f; // 移动速度
    [Export] public float Attack { get; set; } = 8f; // 接触伤害
    [Export] public float AttackInterval { get; set; } = 0.9f; // 接触攻击间隔（秒）
    [Export] public float ContactRange { get; set; } = 28f; // 接触判定距离
    [Export] public int Xp { get; set; } = 6; // 击杀奖励经验
    [Export] public int Gold { get; set; } = 1; // 击杀奖励金币
    [Export] public float Radius { get; set; } = 14f; // 碰撞/视觉半径
    [Export] public Color Color { get; set; } = new(0.86f, 0.28f, 0.32f); // 外观颜色

    public bool HasTag(string tag) => Tags.Contains(tag);

    /// <summary>从导出字段构建敌人 CharacterStats。</summary>
    public CharacterStats BuildStats()
    {
        var stats = new CharacterStats();
        stats.SetBase(StatType.MaxHp, MaxHp);
        stats.SetBase(StatType.Attack, Attack);
        stats.SetBase(StatType.MoveSpeed, MoveSpeed);
        stats.SetBase(StatType.AttackRange, ContactRange);
        stats.SetBase(StatType.Defense, 0f);
        stats.SetBase(StatType.CritRate, 0f);
        stats.SetBase(StatType.CritDamage, 2f);
        stats.SetBase(StatType.Dodge, 0f);
        return stats;
    }
}
