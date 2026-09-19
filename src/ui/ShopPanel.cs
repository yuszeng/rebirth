namespace Rebirth.UI;

/// <summary>回合结束后的商店：货架 + 装备栏 + 背包。不保存进度。</summary>
public partial class ShopPanel : CanvasLayer
{
    Label _title = null!;
    Label _gold = null!;
    GridContainer _grid = null!;
    VBoxContainer _slotList = null!;
    VBoxContainer _backpackList = null!;
    Button _refresh = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 12;
        Visible = false;

        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.55f),
            Name = "Dim",
        };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer { Name = "Panel" };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        panel.OffsetLeft = -520;
        panel.OffsetRight = 520;
        panel.OffsetTop = -300;
        panel.OffsetBottom = 300;
        AddChild(panel);

        var root = new VBoxContainer { Name = "Root" };
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);

        _title = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(_title);

        _gold = new Label { HorizontalAlignment = HorizontalAlignment.Center };
        root.AddChild(_gold);

        var columns = new HBoxContainer { Name = "Columns" };
        columns.AddThemeConstantOverride("separation", 16);
        columns.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        columns.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        root.AddChild(columns);

        var shopColumn = CreateColumn(columns, "货架");
        _grid = new GridContainer
        {
            Name = "Slots",
            Columns = 2,
        };
        _grid.AddThemeConstantOverride("h_separation", 8);
        _grid.AddThemeConstantOverride("v_separation", 8);
        shopColumn.AddChild(_grid);

        _refresh = new Button { CustomMinimumSize = new Vector2(0, 40) };
        _refresh.Pressed += () => GameManager.Instance.RefreshShop();
        shopColumn.AddChild(_refresh);

        var slotColumn = CreateColumn(columns, "装备栏");
        _slotList = new VBoxContainer { Name = "EquipmentSlots" };
        _slotList.AddThemeConstantOverride("separation", 6);
        slotColumn.AddChild(_slotList);

        var bagColumn = CreateColumn(columns, "背包（已购买）");
        var bagScroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(280, 320),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        bagColumn.AddChild(bagScroll);
        _backpackList = new VBoxContainer { Name = "Backpack" };
        _backpackList.AddThemeConstantOverride("separation", 6);
        _backpackList.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        bagScroll.AddChild(_backpackList);

        var leave = new Button
        {
            Text = "离开商店，开始下一回合",
            CustomMinimumSize = new Vector2(0, 44),
        };
        leave.Pressed += () => GameManager.Instance.LeaveShop();
        root.AddChild(leave);

        var restart = new Button { Text = "重新开始本局" };
        restart.Pressed += () => GameManager.Instance.Restart();
        root.AddChild(restart);

        EventBus.Instance.ShopOpened += OnStockChanged;
        EventBus.Instance.ShopChanged += OnStockChanged;
        EventBus.Instance.ShopClosed += OnClosed;
    }

    static VBoxContainer CreateColumn(HBoxContainer parent, string title)
    {
        var box = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        box.AddThemeConstantOverride("separation", 8);
        parent.AddChild(box);
        box.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        return box;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null)
        {
            return;
        }

        EventBus.Instance.ShopOpened -= OnStockChanged;
        EventBus.Instance.ShopChanged -= OnStockChanged;
        EventBus.Instance.ShopClosed -= OnClosed;
    }

    void OnClosed() => Visible = false;

    void OnStockChanged(ShopStock stock)
    {
        Visible = true;
        _title.Text = $"第 {stock.CombatRound} 回合结束 · 商店";
        _gold.Text = $"金币 {stock.Gold}";
        _refresh.Text = $"刷新货架（{stock.RefreshPrice} 金币）";
        _refresh.Disabled = stock.Gold < stock.RefreshPrice;
        RebuildShopGrid(stock);
        RebuildEquipmentSlots(stock);
        RebuildBackpack(stock);
    }

    void RebuildShopGrid(ShopStock stock)
    {
        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        for (var i = 0; i < stock.Slots.Count; i++)
        {
            var slotIndex = i;
            var item = stock.Slots[i];
            var button = new Button { CustomMinimumSize = new Vector2(220, 84) };
            if (item == null)
            {
                button.Text = "已售出";
                button.Disabled = true;
            }
            else
            {
                var ownedWeapon = item.Weapon != null && GameManager.Instance.Run.OwnedWeaponIds.Contains(item.Weapon.Id);
                var ownedArmor = item.Equipment != null && GameManager.Instance.Run.OwnedEquipmentIds.Contains(item.Equipment.Id);
                if (ownedWeapon || ownedArmor)
                {
                    button.Text = $"{item.DisplayName}\n已拥有\n{item.Price} 金币";
                    button.Disabled = true;
                }
                else
                {
                    var kind = item.Weapon != null ? "武器" : item.GrantsEquipment ? "护甲" : "属性";
                    button.Text = $"{item.DisplayName}\n[{kind}] {item.Description}\n{item.Price} 金币";
                    button.Disabled = stock.Gold < item.Price;
                    button.Pressed += () => GameManager.Instance.BuyShopItem(slotIndex);
                }
            }

            _grid.AddChild(button);
        }
    }

    void RebuildEquipmentSlots(ShopStock stock)
    {
        foreach (var child in _slotList.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var slot in stock.EquipmentSlots)
        {
            var label = new Label
            {
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(200, 0),
                Text = slot.IsEmpty
                    ? $"{slot.SlotName}：空"
                    : $"{slot.SlotName}：{slot.DisplayName}",
            };
            _slotList.AddChild(label);
        }
    }

    void RebuildBackpack(ShopStock stock)
    {
        foreach (var child in _backpackList.GetChildren())
        {
            child.QueueFree();
        }

        if (stock.Backpack.Count == 0)
        {
            _backpackList.AddChild(new Label { Text = "还没有可装备的物品" });
            return;
        }

        foreach (var item in stock.Backpack)
        {
            var button = new Button
            {
                CustomMinimumSize = new Vector2(260, 56),
                Text = item.IsEquipped
                    ? $"{item.DisplayName}\n{item.SlotName} · 已装备"
                    : $"{item.DisplayName}\n{item.SlotName} · 点击装备",
                Disabled = item.IsEquipped,
            };
            var itemId = item.Id;
            var isWeapon = item.IsWeapon;
            if (!item.IsEquipped)
            {
                button.Pressed += () => GameManager.Instance.EquipFromBackpack(itemId, isWeapon);
            }

            _backpackList.AddChild(button);
        }
    }
}
