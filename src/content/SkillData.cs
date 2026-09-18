namespace Rebirth.Content;

/// <summary>技能模板。新增技能优先加 .tres，而不是改释放器里的技能名。</summary>
[GlobalClass]
public partial class SkillData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public float Cooldown { get; set; } = 3f;
    [Export] public float Damage { get; set; }
    [Export] public float AttackScale { get; set; } = 1f;
    [Export] public AttackPatternData? Pattern { get; set; }
    [Export] public string[] DamageTags { get; set; } = ["skill"];
}
