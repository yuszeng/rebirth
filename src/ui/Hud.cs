namespace Rebirth.UI;

/// <summary>战斗 HUD：血条、经验条（含数字）、右侧属性、统计信息。</summary>
public partial class Hud : CanvasLayer
{
	Label _stats = null!; // 文字统计行
	Label _hpValue = null!; // 血条上的数字
	Label _xpValue = null!; // 经验条上的数字
	Label _attributes = null!; // 右侧当前属性
	ProgressBar _hp = null!; // 血量进度条
	ProgressBar _xp = null!; // 经验进度条

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always; // 升级/暂停时仍刷新
		var root = CreateRoot();
		_hp = CreateBar(root, new Color(0.78f, 0.22f, 0.28f), new Vector2(24, 24), out _hpValue); // 创建血量进度条
		_xp = CreateBar(root, new Color(0.28f, 0.55f, 0.92f), new Vector2(24, 55), out _xpValue); // 创建经验进度条
		_stats = new Label
		{
			Position = new Vector2(24, 76),
			Size = new Vector2(900, 40),
		};
		root.AddChild(_stats);
		_attributes = CreateAttributePanel(root);

		EventBus.Instance.RunStarted += OnHudEvent;
		EventBus.Instance.DamageApplied += OnDamageApplied;
		EventBus.Instance.XpGained += OnHudPairEvent;
		EventBus.Instance.GoldGained += OnHudPairEvent;
		EventBus.Instance.LevelUp += OnHudLevelEvent;
	}

	public override void _ExitTree()
	{
		// 必须退订：Hud 随场景销毁，EventBus 是 Autoload
		if (EventBus.Instance == null)
		{
			return;
		}

		EventBus.Instance.RunStarted -= OnHudEvent;
		EventBus.Instance.DamageApplied -= OnDamageApplied;
		EventBus.Instance.XpGained -= OnHudPairEvent;
		EventBus.Instance.GoldGained -= OnHudPairEvent;
		EventBus.Instance.LevelUp -= OnHudLevelEvent;
	}

	public override void _Process(double _delta) => Refresh(); // 每帧刷新存活时间与属性

	void OnHudEvent(RunState _) => Refresh();
	void OnDamageApplied(DamageRequest _, float _amount) => Refresh();
	void OnHudPairEvent(int _a, int _b) => Refresh();
	void OnHudLevelEvent(int _) => Refresh();

	/// <summary>创建根容器。</summary>
	Control CreateRoot()
	{
		var root = new Control { Name = "Root" };
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(root);
		return root;
	}

	/// <summary>创建带居中数字的进度条。</summary>
	static ProgressBar CreateBar(Control parent, Color fill, Vector2 position, out Label valueLabel)
	{
		var bar = new ProgressBar
		{
			Position = position,
			Size = new Vector2(280, 18),
			ShowPercentage = false,
		};
		bar.AddThemeColorOverride("fill", fill);

		valueLabel = new Label
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore, // 不要挡住条本身
		};
		valueLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bar.AddChild(valueLabel);
		parent.AddChild(bar);
		return bar;
	}

	/// <summary>屏幕右侧属性面板，只读当前玩家 CharacterStats。</summary>
	static Label CreateAttributePanel(Control parent)
	{
		var panel = new PanelContainer
		{
			Name = "Attributes",
		};
		panel.SetAnchorsPreset(Control.LayoutPreset.TopRight);
		panel.GrowHorizontal = Control.GrowDirection.Begin;
		panel.OffsetLeft = -268;
		panel.OffsetRight = -24;
		panel.OffsetTop = 24;
		panel.OffsetBottom = 236;
		parent.AddChild(panel);

		var label = new Label
		{
			Name = "AttributeText",
		};
		panel.AddChild(label);
		return label;
	}

	/// <summary>刷新 HUD 显示。</summary>
	void Refresh()
	{
		var run = GameManager.Instance.Run;
		var player = Player.FindAlive() ?? GetTree().GetFirstNodeInGroup("player") as Player;
		var hpText = "-/-";
		if (player?.Health != null)
		{
			_hp.MaxValue = player.Health.Maximum;
			_hp.Value = player.Health.Current;
			hpText = $"{Mathf.RoundToInt(player.Health.Current)}/{Mathf.RoundToInt(player.Health.Maximum)}";
		}

		_hpValue.Text = hpText;
		_xp.MaxValue = Math.Max(run.XpToNext, 1);
		_xp.Value = run.Xp;
		_xpValue.Text = $"{run.Xp}/{run.XpToNext}";

		var total = (int)run.ElapsedSeconds;
		var remain = Mathf.Max(0, Mathf.CeilToInt(run.RoundDurationSeconds - run.RoundElapsedSeconds));
		_stats.Text =
			$"Lv.{run.Level}    金币 {run.Gold}    击杀 {run.KillCount}    回合 {run.CombatRound} 剩余 {remain / 60:00}:{remain % 60:00}    总时长 {total / 60:00}:{total % 60:00}" +
			(run.PendingLevelUps > 0 ? $"    待选升级 {run.PendingLevelUps}" : "");
		_attributes.Text = FormatAttributes(player);
	}

	static string FormatAttributes(Player? player)
	{
		if (player?.Stats == null)
		{
			return "属性\n-";
		}

		var stats = player.Stats;
		var weapon = player.GetNodeOrNull<Weapon>("Weapon")?.Data;
		var weaponName = weapon?.DisplayName ?? "-";
		var maxTargets = weapon?.MaxTargets ?? 0;
		return
			$"当前属性\n" +
			$"生命 {Mathf.RoundToInt(stats.GetValue(StatType.MaxHp))}\n" +
			$"攻击 {stats.GetValue(StatType.Attack):0.#}\n" +
			$"攻速 {stats.GetValue(StatType.AttackSpeed):0.00}/秒\n" +
			$"移速 {Mathf.RoundToInt(stats.GetValue(StatType.MoveSpeed))}\n" +
			$"范围 {Mathf.RoundToInt(stats.GetValue(StatType.AttackRange))}\n" +
			$"武器 {weaponName}（最多 {maxTargets} 目标）";
	}
}
