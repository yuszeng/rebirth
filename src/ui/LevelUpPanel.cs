namespace Rebirth.UI;

/// <summary>升级选择面板：暂停时弹出，展示动态生成的升级按钮。</summary>
public partial class LevelUpPanel : CanvasLayer
{
	VBoxContainer _box = null!; // 按钮容器
	Label _title = null!; // 标题标签

	public override void _Ready()
	{
		// 始终处理，保证暂停时也能响应
		ProcessMode = ProcessModeEnum.Always;
		// 初始化时面板隐藏
		Visible = false;

		// 创建半透明遮罩
		var overlay = new ColorRect
		{
			Color = new Color(0, 0, 0, 0.45f), // 半透明黑色
			Name = "Dim",
		};
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect); // 覆盖整个屏幕
		AddChild(overlay);

		// 创建居中的面板容器
		var panel = new PanelContainer
		{
			Name = "Panel",
		};
		panel.SetAnchorsPreset(Control.LayoutPreset.Center); // 居中
		panel.GrowHorizontal = Control.GrowDirection.Both;
		panel.GrowVertical = Control.GrowDirection.Both;
		// 面板尺寸
		panel.OffsetLeft = -230;
		panel.OffsetRight = 230;
		panel.OffsetTop = -140;
		panel.OffsetBottom = 140;
		AddChild(panel);

		// 创建用于承载按钮和标题的垂直容器
		_box = new VBoxContainer { Name = "VBox" };
		_box.AddThemeConstantOverride("separation", 12); // 按钮间距
		panel.AddChild(_box);

		// 添加标题标签
		_title = new Label
		{
			Text = "选择一项强化", // 固定标题文本
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		_box.AddChild(_title); // 添加标题标签

		// 注册事件：收到升级选项时回调
		EventBus.Instance.UpgradeOffered += OnOffered;
		EventBus.Instance.UpgradeChosen += OnUpgradeChosen;
	}

	public override void _ExitTree()
	{
		if (EventBus.Instance != null)
		{
			EventBus.Instance.UpgradeOffered -= OnOffered;
			EventBus.Instance.UpgradeChosen -= OnUpgradeChosen;
		}
	}

	void OnUpgradeChosen(UpgradeOptionData _)
	{
		// 选完且已离开 LevelUp 状态时隐藏（连升时 State 仍为 LevelUp，由 OnOffered 刷新按钮）
		if (GameManager.Instance.State != GameState.LevelUp)
		{
			Visible = false;
		}
	}

	/// <summary>收到升级选项后重建按钮列表。</summary>
	void OnOffered(IReadOnlyList<UpgradeOptionData> options)
	{
		Visible = true;
		// 清除旧按钮，保留标题
		foreach (var child in _box.GetChildren())
		{
			if (child != _title)
			{
				child.QueueFree();
			}
		}

		foreach (var option in options)
		{
			var captured = option; // 闭包捕获，避免循环变量被覆盖
			var button = new Button
			{
				Text = $"{option.DisplayName}\n{option.Description}",
				CustomMinimumSize = new Vector2(420, 64),
			};
			button.Pressed += () => GameManager.Instance.ChooseUpgrade(captured);
			_box.AddChild(button);
		}
	}
}
