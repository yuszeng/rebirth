namespace Rebirth.Progression;

/// <summary>商店页装备栏一格，只给 UI 展示。</summary>
public sealed class EquipmentSlotView
{
    public EquipmentSlotKind Slot { get; init; }
    public string SlotName { get; init; } = "";
    public string ItemId { get; init; } = "";
    public string DisplayName { get; init; } = "空";
    public string Description { get; init; } = "";
    public bool IsEmpty => string.IsNullOrEmpty(ItemId);
}

/// <summary>本局背包中的一件可装备物（武器或护甲）。属性商品不出现在这里。</summary>
public sealed class BackpackItemView
{
    public string Id { get; init; } = "";
    public bool IsWeapon { get; init; }
    public EquipmentSlotKind Slot { get; init; }
    public string SlotName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public bool IsEquipped { get; init; }
}
