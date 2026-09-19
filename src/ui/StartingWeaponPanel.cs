namespace Rebirth.UI;

/// <summary>每局开场的武器选择；只提交选择结果，不持有本局战斗数据。</summary>
public partial class StartingWeaponPanel : CanvasLayer
{
    GridContainer _options = null!;
    Action<WeaponData>? _onSelected;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 30;
        Visible = false;

        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.72f),
            Name = "Dim",
        };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer { Name = "Panel" };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        panel.OffsetLeft = -350;
        panel.OffsetRight = 350;
        panel.OffsetTop = -310;
        panel.OffsetBottom = 310;
        AddChild(panel);

        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 14);
        panel.AddChild(content);

        content.AddChild(new Label
        {
            Text = "选择初始武器",
            HorizontalAlignment = HorizontalAlignment.Center,
        });
        content.AddChild(new Label
        {
            Text = "本局将使用所选武器攻击；商店还可买到尚未拥有的武器，按 E 切换。",
            HorizontalAlignment = HorizontalAlignment.Center,
            Modulate = new Color(0.78f, 0.82f, 0.88f),
        });

        var scroll = new ScrollContainer
        {
            Name = "Scroll",
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };
        content.AddChild(scroll);

        _options = new GridContainer
        {
            Name = "Options",
            Columns = 2,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        _options.AddThemeConstantOverride("h_separation", 12);
        _options.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(_options);
    }

    public void Open(IReadOnlyList<WeaponData> weapons, Action<WeaponData> onSelected)
    {
        _onSelected = onSelected;
        Visible = true;
        foreach (var child in _options.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var weapon in weapons)
        {
            var captured = weapon;
            var button = new Button
            {
                Text = $"{weapon.DisplayName}\n{weapon.Description}",
                CustomMinimumSize = new Vector2(320, 92),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
            };
            button.Pressed += () => Select(captured);
            _options.AddChild(button);
        }
    }

    void Select(WeaponData weapon)
    {
        if (_onSelected == null)
        {
            return;
        }

        var callback = _onSelected;
        _onSelected = null; // 防止双击在同一帧重复启动本局
        Visible = false;
        callback(weapon);
    }
}
