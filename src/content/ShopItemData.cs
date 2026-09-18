namespace Rebirth.Content;

/// <summary>可购买项。商店只读写展示/价格/权重；效果由购买后的应用方处理。</summary>
[GlobalClass]
public partial class ShopItemData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public int Price { get; set; } = 10;
    [Export] public float Weight { get; set; } = 1f;
    [Export] public StatType Stat { get; set; } = StatType.Attack;
    [Export] public float Flat { get; set; }
    [Export] public float Percent { get; set; }
    [Export] public SkillData? Skill { get; set; }

    public bool GrantsSkill => Skill != null;

    public StatModifier ToModifier(string sourceId) => new()
    {
        SourceId = sourceId,
        Stat = Stat,
        Flat = Flat,
        Percent = Percent,
    };
}
