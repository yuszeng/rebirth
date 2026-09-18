namespace Rebirth.UI;

/// <summary>回合结束后的商店界面：购买、刷新、离开。不保存进度。</summary>
public partial class ShopPanel : CanvasLayer
{
    Label _title = null!;
    Label _gold = null!;
    GridContainer _grid = null!;
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
        panel.OffsetLeft = -320;
        panel.OffsetRight = 320;
        panel.OffsetTop = -240;
        panel.OffsetBottom = 240;
        AddChild(panel);

        var box = new VBoxContainer { Name = "VBox" };
        box.AddThemeConstantOverride("separation", 14);
        panel.AddChild(box);

        _title = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        box.AddChild(_title);

        _gold = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        box.AddChild(_gold);

        _grid = new GridContainer
        {
            Name = "Slots",
            Columns = 2,
        };
        _grid.AddThemeConstantOverride("h_separation", 12);
        _grid.AddThemeConstantOverride("v_separation", 12);
        box.AddChild(_grid);

        _refresh = new Button { CustomMinimumSize = new Vector2(0, 44) };
        _refresh.Pressed += () => GameManager.Instance.RefreshShop();
        box.AddChild(_refresh);

        var leave = new Button
        {
            Text = "离开商店，开始下一回合",
            CustomMinimumSize = new Vector2(0, 48),
        };
        leave.Pressed += () => GameManager.Instance.LeaveShop();
        box.AddChild(leave);

        var restart = new Button { Text = "重新开始本局" };
        restart.Pressed += () => GameManager.Instance.Restart();
        box.AddChild(restart);

        EventBus.Instance.ShopOpened += OnStockChanged;
        EventBus.Instance.ShopChanged += OnStockChanged;
        EventBus.Instance.ShopClosed += OnClosed;
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

        foreach (var child in _grid.GetChildren())
        {
            child.QueueFree();
        }

        for (var i = 0; i < stock.Slots.Count; i++)
        {
            var slotIndex = i;
            var item = stock.Slots[i];
            var button = new Button
            {
                CustomMinimumSize = new Vector2(280, 88),
            };
            if (item == null)
            {
                button.Text = "已售出";
                button.Disabled = true;
            }
            else
            {
                button.Text = $"{item.DisplayName}\n{item.Description}\n{item.Price} 金币";
                button.Disabled = stock.Gold < item.Price;
                button.Pressed += () => GameManager.Instance.BuyShopItem(slotIndex);
            }

            _grid.AddChild(button);
        }
    }
}
