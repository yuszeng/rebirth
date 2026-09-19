namespace Rebirth.Character;

/// <summary>本局护甲栏：持有、穿上、替换、分层外观。武器仍由 Player / AttackController 处理。</summary>
public partial class EquipmentLoadout : Node
{
    Combatant? _owner;
    readonly List<EquipmentData> _owned = [];
    readonly Dictionary<EquipmentSlotKind, EquipmentData> _equippedArmor = [];
    readonly Dictionary<string, Polygon2D> _layers = [];

    public IReadOnlyList<EquipmentData> Owned => _owned;

    public override void _Ready()
    {
        _owner = GetParent() as Combatant;
        EnsureLayers();
        RefreshVisuals();
    }

    public EquipmentData? GetEquippedArmor(EquipmentSlotKind slot) =>
        _equippedArmor.GetValueOrDefault(slot);

    public void Clear()
    {
        _owned.Clear();
        _equippedArmor.Clear();
        RefreshVisuals();
    }

    public bool Has(string equipmentId) =>
        !string.IsNullOrEmpty(equipmentId) && _owned.Any(item => item.Id == equipmentId);

    public bool CanGrant(EquipmentData? data) =>
        data is { IsArmorPiece: true } && !Has(data.Id);

    public bool TryGrant(EquipmentData? data)
    {
        if (data == null || !CanGrant(data))
        {
            return false;
        }

        _owned.Add(data);
        return true;
    }

    /// <summary>对应槽为空才自动穿上，避免商店购买直接顶掉已穿装备。</summary>
    public bool TryAutoEquipIfEmpty(EquipmentData data) =>
        Has(data.Id) && GetEquippedArmor(data.Slot) == null && TryEquip(data.Id);

    public bool TryEquip(string equipmentId)
    {
        var item = _owned.FirstOrDefault(owned => owned.Id == equipmentId);
        if (item == null || !item.IsArmorPiece)
        {
            return false;
        }

        var current = GetEquippedArmor(item.Slot);
        if (current?.Id == item.Id)
        {
            return true;
        }

        if (current != null)
        {
            RemoveArmorModifiers(current);
            _equippedArmor.Remove(item.Slot);
        }

        _equippedArmor[item.Slot] = item;
        _owner ??= GetParent() as Combatant;
        if (_owner != null)
        {
            _owner.Stats.AddModifier(item.ToModifier(SourceId(item)));
            _owner.SyncHealthToStats();
        }

        RefreshVisuals();
        return true;
    }

    static string SourceId(EquipmentData item) => $"equip_{item.Slot}_{item.Id}";

    void RemoveArmorModifiers(EquipmentData item)
    {
        if (_owner == null)
        {
            return;
        }

        _owner.Stats.RemoveBySource(SourceId(item));
        _owner.SyncHealthToStats();
    }

    void EnsureLayers()
    {
        var parent = GetParent();
        if (parent == null)
        {
            return;
        }

        CreateLayer(parent, "VisualHead", 4);
        CreateLayer(parent, "VisualHandL", 3);
        CreateLayer(parent, "VisualHandR", 3);
        CreateLayer(parent, "VisualPants", 1);
        CreateLayer(parent, "VisualShoeL", 2);
        CreateLayer(parent, "VisualShoeR", 2);
    }

    void CreateLayer(Node parent, string name, int zIndex)
    {
        if (_layers.ContainsKey(name))
        {
            return;
        }

        var existing = parent.GetNodeOrNull<Polygon2D>(name);
        if (existing != null)
        {
            _layers[name] = existing;
            return;
        }

        var poly = new Polygon2D
        {
            Name = name,
            ZIndex = zIndex,
            Visible = false,
        };
        parent.AddChild(poly);
        _layers[name] = poly;
    }

    void RefreshVisuals()
    {
        _owner ??= GetParent() as Combatant;
        if (_owner == null)
        {
            return;
        }

        EnsureLayers();
        var fallback = _owner is Player player && player.Data != null
            ? player.Data.Color
            : new Color(0.32f, 0.52f, 0.98f);
        var chest = GetEquippedArmor(EquipmentSlotKind.Chest);
        _owner.RecolorBody(chest?.VisualColor ?? fallback);

        var radius = _owner.Radius;
        ShowDisc("VisualHead", EquipmentSlotKind.Head, new Vector2(0f, -radius * 0.92f), radius * 0.4f);
        ShowDisc("VisualPants", EquipmentSlotKind.Pants, new Vector2(0f, radius * 0.38f), radius * 0.52f);

        var hands = GetEquippedArmor(EquipmentSlotKind.Hands);
        ShowOptionalDisc("VisualHandL", hands, new Vector2(-radius * 0.98f, 0.12f * radius), radius * 0.26f);
        ShowOptionalDisc("VisualHandR", hands, new Vector2(radius * 0.98f, 0.12f * radius), radius * 0.26f);

        var shoes = GetEquippedArmor(EquipmentSlotKind.Shoes);
        ShowOptionalDisc("VisualShoeL", shoes, new Vector2(-radius * 0.38f, radius * 0.92f), radius * 0.22f);
        ShowOptionalDisc("VisualShoeR", shoes, new Vector2(radius * 0.38f, radius * 0.92f), radius * 0.22f);
    }

    void ShowDisc(string layerName, EquipmentSlotKind slot, Vector2 offset, float radius)
    {
        ShowOptionalDisc(layerName, GetEquippedArmor(slot), offset, radius);
    }

    void ShowOptionalDisc(string layerName, EquipmentData? item, Vector2 offset, float radius)
    {
        if (!_layers.TryGetValue(layerName, out var poly))
        {
            return;
        }

        if (item == null)
        {
            poly.Visible = false;
            return;
        }

        poly.Visible = true;
        poly.Color = item.VisualColor;
        poly.Position = offset;
        poly.Polygon = Combatant.MakeDisc(radius);
    }
}
