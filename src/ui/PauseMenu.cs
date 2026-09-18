namespace Rebirth.UI;

/// <summary>ESC 暂停菜单：继续游戏或重开本局。</summary>
public partial class PauseMenu : CanvasLayer
{
    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // 树暂停时仍要接收按钮与显示
        Layer = 20;
        Visible = false;

        var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.55f) };
        overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(overlay);

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        panel.OffsetLeft = -180;
        panel.OffsetRight = 180;
        panel.OffsetTop = -120;
        panel.OffsetBottom = 120;
        AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 16);
        panel.AddChild(box);

        box.AddChild(new Label
        {
            Text = "暂停",
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        var resume = new Button { Text = "回到游戏" };
        resume.Pressed += () => GameManager.Instance.ResumeFromPause();
        box.AddChild(resume);

        var restart = new Button { Text = "重新开始" };
        restart.Pressed += () => GameManager.Instance.Restart();
        box.AddChild(restart);

        EventBus.Instance.PauseChanged += OnPauseChanged;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.PauseChanged -= OnPauseChanged;
        }
    }

    void OnPauseChanged(bool paused) => Visible = paused;
}
