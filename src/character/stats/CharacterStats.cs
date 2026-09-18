namespace Rebirth.Character;

/// <summary>最终值 = (base + flat) * (1 + percent)</summary>
public sealed class CharacterStats
{
    readonly Dictionary<StatType, float> _base = []; // 基础属性表
    readonly List<StatModifier> _modifiers = []; // 叠加的修饰器列表

    public void SetBase(StatType stat, float value) => _base[stat] = value;

    public float GetBase(StatType stat) => _base.GetValueOrDefault(stat);

    public void AddModifier(StatModifier modifier) => _modifiers.Add(modifier);

    /// <summary>按来源 ID 移除修饰器（如卸下装备、撤销 Buff）。</summary>
    public void RemoveBySource(string sourceId) =>
        _modifiers.RemoveAll(m => m.SourceId == sourceId);

    /// <summary>计算某属性的最终值：先加 flat，再乘 percent。</summary>
    public float GetValue(StatType stat)
    {
        var baseValue = GetBase(stat); // 基础值
        var flat = 0f; // 固定加值总和
        var percent = 0f; // 百分比加值总和（0.1 = +10%）
        foreach (var modifier in _modifiers)
        {
            if (modifier.Stat != stat)
            {
                continue;
            }

            flat += modifier.Flat;
            percent += modifier.Percent;
        }

        return (baseValue + flat) * (1f + percent);
    }
}
