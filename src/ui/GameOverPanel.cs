namespace Rebirth.UI;

/// <summary>本局结算面板：展示存活时间、击杀、等级、金币。</summary>
public partial class GameOverPanel : CanvasLayer
{
	Label _label = null!; // 结算文字

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always; // 始终处理，保证暂停时也能响应
		Visible = false; // 初始时隐藏

		var overlay = new ColorRect { Color = new Color(0, 0, 0, 0.55f) }; // 深色遮罩
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect); // 覆盖整个屏幕
		AddChild(overlay);

		var panel = new PanelContainer(); // 创建居中的面板容器
		panel.SetAnchorsPreset(Control.LayoutPreset.Center);
		panel.GrowHorizontal = Control.GrowDirection.Both; // 水平填充      
		panel.GrowVertical = Control.GrowDirection.Both;
		panel.OffsetLeft = -220;
		panel.OffsetRight = 220;
		panel.OffsetTop = -160;
		panel.OffsetBottom = 160;
		AddChild(panel);

		var box = new VBoxContainer();
		box.AddThemeConstantOverride("separation", 16);
		panel.AddChild(box);

		_label = new Label { HorizontalAlignment = HorizontalAlignment.Center };
		box.AddChild(_label);

		var button = new Button { Text = "重新开始" };
		button.Pressed += () => GameManager.Instance.Restart(); // 重新开始游戏
		box.AddChild(button);

		EventBus.Instance.RunEnded += OnEnded; // 注册本局结束事件
	}

	public override void _ExitTree()
	{
		if (EventBus.Instance != null)
		{
			EventBus.Instance.RunEnded -= OnEnded;
		}
	}

	void OnEnded(RunResult result)
	{
		Visible = true; // 显示面板
		var seconds = (int)result.SurvivedSeconds;
		_label.Text =
			$"本世结束\n\n存活 {seconds / 60:00}:{seconds % 60:00}\n击杀 {result.KillCount}\n等级 {result.Level}\n金币 {result.Gold}\n\n按 R 或点击按钮重新开始"; // 设置结算文字
	}
}
