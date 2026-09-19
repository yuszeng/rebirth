namespace Rebirth.Content;

/// <summary>护甲模板。不含武器；武器仍用 WeaponData。</summary>
[GlobalClass]
public partial class EquipmentData : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export] public string Description { get; set; } = "";
    [Export] public EquipmentSlotKind Slot { get; set; } = EquipmentSlotKind.Head;
    [Export] public StatType Stat { get; set; } = StatType.Defense;
    [Export] public float Flat { get; set; }
    [Export] public float Percent { get; set; }
    [Export] public Color VisualColor { get; set; } = new(0.72f, 0.62f, 0.42f);

    public bool IsArmorPiece => EquipmentSlots.IsArmor(Slot) && !string.IsNullOrEmpty(Id);

    public StatModifier ToModifier(string sourceId) => new()
    {
        SourceId = sourceId,
        Stat = Stat,
        Flat = Flat,
        Percent = Percent,
    };
}
