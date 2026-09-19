namespace Rebirth.Content;

/// <summary>装备栏槽位。武器槽只接受 WeaponData，其余槽接受 EquipmentData。</summary>
public enum EquipmentSlotKind
{
    Head,
    Hands,
    Chest,
    Pants,
    Shoes,
    Weapon,
}

public static class EquipmentSlots
{
    public static readonly EquipmentSlotKind[] All =
    [
        EquipmentSlotKind.Head,
        EquipmentSlotKind.Hands,
        EquipmentSlotKind.Chest,
        EquipmentSlotKind.Pants,
        EquipmentSlotKind.Shoes,
        EquipmentSlotKind.Weapon,
    ];

    public static string DisplayName(EquipmentSlotKind slot) => slot switch
    {
        EquipmentSlotKind.Head => "头部",
        EquipmentSlotKind.Hands => "手部",
        EquipmentSlotKind.Chest => "胸部",
        EquipmentSlotKind.Pants => "裤子",
        EquipmentSlotKind.Shoes => "鞋子",
        EquipmentSlotKind.Weapon => "武器",
        _ => slot.ToString(),
    };

    public static bool IsArmor(EquipmentSlotKind slot) => slot != EquipmentSlotKind.Weapon;
}
