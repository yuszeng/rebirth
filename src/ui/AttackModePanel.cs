namespace Rebirth.UI;

/// <summary>攻击方式选择面板：战斗中按 E 暂停并切换当前自动攻击方式。</summary>
public partial class AttackModePanel : CanvasLayer
{
    VBoxContainer _options = null!;
    Label _title = null!;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        Layer = 18;
        Visible = false;

        var overlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.5f),
            Name = "Dim",
        };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer { Name = "Panel" };
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        panel.OffsetLeft = -260;
        panel.OffsetRight = 260;
        panel.OffsetTop = -210;
        panel.OffsetBottom = 210;
        AddChild(panel);

        var box = new VBoxContainer { Name = "VBox" };
        box.AddThemeConstantOverride("separation", 12);
        panel.AddChild(box);

        _title = new Label
        {
            Text = "选择攻击方式",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        box.AddChild(_title);

        _options = new VBoxContainer { Name = "Options" };
        _options.AddThemeConstantOverride("separation", 10);
        box.AddChild(_options);

        var cancel = new Button
        {
            Text = "取消，继续当前攻击方式",
            CustomMinimumSize = new Vector2(480, 44),
        };
        cancel.Pressed += () => GameManager.Instance.CancelAttackModeSelection();
        box.AddChild(cancel);

        EventBus.Instance.AttackModeSelectionOpened += OnOpened;
        EventBus.Instance.AttackModeSelectionClosed += OnClosed;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null)
        {
            return;
        }

        EventBus.Instance.AttackModeSelectionOpened -= OnOpened;
        EventBus.Instance.AttackModeSelectionClosed -= OnClosed;
    }

    void OnClosed() => Visible = false;

    void OnOpened(AttackModeStock stock)
    {
        Visible = true;
        _title.Text = $"选择攻击方式（当前：{CurrentName(stock)}）";

        foreach (var child in _options.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var option in stock.Options)
        {
            var captured = option;
            var isCurrent = option.Id == stock.SelectedModeId;
            var prefix = isCurrent ? "当前 · " : "";
            var button = new Button
            {
                Text = $"{prefix}{option.DisplayName}\n{option.Description}",
                CustomMinimumSize = new Vector2(480, 64),
            };
            button.Pressed += () => GameManager.Instance.ChooseAttackMode(captured.Id);
            _options.AddChild(button);
        }
    }

    static string CurrentName(AttackModeStock stock) =>
        stock.Options.FirstOrDefault(option => option.Id == stock.SelectedModeId)?.DisplayName ?? "普通攻击";
}
