namespace Rebirth.UI;

/// <summary>升级选择面板：暂停时弹出，展示动态生成的升级按钮。</summary>
public partial class LevelUpPanel : CanvasLayer
{
	VBoxContainer _options = null!; // 只放动态选项，和标题/刷新分开
	Label _title = null!;
	Button _refresh = null!;

	public override void _Ready()
	{
		// 始终处理，保证暂停时也能响应
		ProcessMode = ProcessModeEnum.Always;
		Layer = 15; // 高于商店，避免回合结束时两块界面叠在一起
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
		panel.OffsetTop = -180;
		panel.OffsetBottom = 180;
		AddChild(panel);

		var box = new VBoxContainer { Name = "VBox" };
		box.AddThemeConstantOverride("separation", 12);
		panel.AddChild(box);

		_title = new Label
		{
			Text = "选择一项强化",
			HorizontalAlignment = HorizontalAlignment.Center,
		};
		box.AddChild(_title);

		_options = new VBoxContainer { Name = "Options" };
		_options.AddThemeConstantOverride("separation", 12);
		box.AddChild(_options);

		_refresh = new Button
		{
			Text = "刷新选项",
			CustomMinimumSize = new Vector2(420, 44),
		};
		// 走 GameManager 重新加权抽取，而不是把整个升级池塞给 UI
		_refresh.Pressed += () => GameManager.Instance.RefreshUpgradeOptions();
		box.AddChild(_refresh);

		// 注册事件：收到升级选项时回调
		EventBus.Instance.UpgradeOffered += OnOffered; // 订阅升级选项事件
		EventBus.Instance.UpgradeChosen += OnUpgradeChosen; // 订阅升级选择事件
	}

	public override void _ExitTree()
	{
		if (EventBus.Instance != null)
		{
			EventBus.Instance.UpgradeOffered -= OnOffered; // 取消订阅升级选项事件
			EventBus.Instance.UpgradeChosen -= OnUpgradeChosen; // 取消订阅升级选择事件
		}
	}

	void OnUpgradeChosen(UpgradeOptionData _) // 升级选择事件处理
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
		foreach (var child in _options.GetChildren())
		{
			child.QueueFree();
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
			_options.AddChild(button);
		}
	}
}
