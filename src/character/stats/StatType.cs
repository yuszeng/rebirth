namespace Rebirth.Character;

/// <summary>角色可修饰的属性枚举。Phase 1 主要使用前几项。</summary>
public enum StatType
{
    MaxHp, // 最大生命值
    Attack, // 攻击力
    AttackSpeed, // 攻击速度（次/秒）
    MoveSpeed, // 移动速度
    AttackRange, // 攻击/接触范围
    Defense, // 防御（Phase 2+）
    CritRate, // 暴击率
    CritDamage, // 暴击伤害倍率
    Dodge, // 闪避
    MaxMana, // 最大法力
    ManaRegen, // 法力回复
    SkillCooldown, // 技能冷却缩减
    Luck, // 幸运（影响掉落/随机）
    Lifesteal, // 生命偷取
    ProjectileSpeed, // 投射物速度
    Area, // 范围加成
    Duration, // 持续时间加成
    DamageBonus, // 通用伤害加成
}
