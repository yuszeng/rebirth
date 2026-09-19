namespace Rebirth.App;

/// <summary>主场景入口：绘制竞技场、实例化玩家与刷怪器、挂载 UI 并启动本局。</summary>
public partial class Main : Node2D
{
	public override void _Ready()
	{
		Visible = true; // 场景根若在编辑器被隐藏，运行时会整屏全黑
		// 重开时 GameOver 可能留下 Paused=true，必须在挂载战斗逻辑前解除
		GetTree().Paused = false;

		DrawArena();

		// 场景按根脚本匹配；数据扫对应目录。角色/刷怪配置里已引用的武器和敌人优先
		var playerScene = ContentDirectory.FindSceneByRootScript("res://scenes", "Player.cs");
		var enemyScene = ContentDirectory.FindSceneByRootScript("res://scenes", "Enemy.cs");
		var playerData = ContentDirectory.RequireOne<CharacterData>("res://content/characters");
		var spawnConfig = ContentDirectory.RequireOne<SpawnConfig>("res://content/spawn");
		var weaponPool = ContentDirectory.LoadAll<WeaponData>("res://content/weapons");
		var fallbackWeapon = weaponPool.FirstOrDefault();
		var fallbackEnemy = ContentDirectory.LoadAll<EnemyData>("res://content/enemies").FirstOrDefault();
		if (playerScene == null || enemyScene == null || playerData == null || spawnConfig == null)
		{
			return;
		}

		// 创建并配置玩家
		var player = playerScene.Instantiate<Player>(); // 玩家实例
		player.Data = playerData;
		player.GlobalPosition = Vector2.Zero; // 出生点：场景中心
		AddChild(player);
		player.Setup(playerData, playerData.StartingWeapon ?? fallbackWeapon); // 选择前仅用于初始化角色节点

		// 创建刷怪导演，负责按间隔在玩家周围生成敌人
		var director = new SpawnDirector
		{
			Name = "SpawnDirector",
			Config = spawnConfig,  // 刷怪配置
			EnemyScene = enemyScene, // 敌人场景
		};
		director.Config!.Enemy ??= fallbackEnemy; // 配置未指定敌人时用敌人目录第一份
		AddChild(director);

		// 挂载 UI 层
		AddChild(new DamagePopupLayer()); // 敌人受击飘字
		AddChild(new Hud()); // 血条、经验、统计信息
		AddChild(new LevelUpPanel()); // 升级选项面板
		AddChild(new ShopPanel()); // 回合结束后的商店
		AddChild(new AttackModePanel()); // 战斗中的攻击方式选择
		var startingWeaponPanel = new StartingWeaponPanel();
		AddChild(startingWeaponPanel); // 开局选择层级高于其他战斗 UI
		AddChild(new GameOverPanel()); // 结算面板
		AddChild(new PauseMenu()); // ESC 暂停菜单

		var combatLoop = ContentDirectory.RequireOne<CombatLoopConfig>("res://content/run");
		var shopConfig = ContentDirectory.RequireOne<ShopConfig>("res://content/run");
		if (weaponPool.Count == 0)
		{
			GameLog.Error("未找到可选择的初始武器：res://content/weapons");
			return;
		}

		// Boot 阶段暂停世界，只让 Always 模式的选择面板响应；选定后 BeginRun 会解除暂停。
		GetTree().Paused = true;
		startingWeaponPanel.Open(weaponPool, selectedWeapon =>
		{
			GameManager.Instance.BeginRun(
				player,
				UpgradeService.LoadPool(),
				selectedWeapon,
				ShopService.LoadPool(),
				shopConfig,
				combatLoop);
		});
	}

	/// <summary>场外暗底 + 场内地板 + 细描边；物理墙无外观，避免四条粗杠。</summary>
	void DrawArena()
	{
		const float voidHalf = 8000f;
		var half = ArenaBounds.HalfExtent;
		var thickness = ArenaBounds.WallThickness;

		AddChild(MakeRect(voidHalf, new Color(0.05f, 0.055f, 0.06f), -20));
		AddChild(MakeRect(half, new Color(0.12f, 0.14f, 0.16f), -10));

		var border = new Line2D
		{
			Width = 6f,
			DefaultColor = new Color(0.62f, 0.7f, 0.8f, 0.85f),
			Closed = true,
			Antialiased = true,
			JointMode = Line2D.LineJointMode.Sharp,
			ZIndex = -5,
			Points =
			[
				new Vector2(-half, -half),
				new Vector2(half, -half),
				new Vector2(half, half),
				new Vector2(-half, half),
			],
		};
		AddChild(border);

		AddWall(new Vector2(0, -half - thickness / 2), new Vector2(half * 2 + thickness * 2, thickness));
		AddWall(new Vector2(0, half + thickness / 2), new Vector2(half * 2 + thickness * 2, thickness));
		AddWall(new Vector2(-half - thickness / 2, 0), new Vector2(thickness, half * 2));
		AddWall(new Vector2(half + thickness / 2, 0), new Vector2(thickness, half * 2));
	}

	static Polygon2D MakeRect(float half, Color color, int zIndex) => new()
	{
		Color = color,
		ZIndex = zIndex,
		Polygon =
		[
			new Vector2(-half, -half),
			new Vector2(half, -half),
			new Vector2(half, half),
			new Vector2(-half, half),
		],
	};

	/// <summary>只放 StaticBody2D 挡 MoveAndSlide，外观由 Line2D 负责。</summary>
	void AddWall(Vector2 center, Vector2 size)
	{
		var wall = new StaticBody2D
		{
			Position = center,
			CollisionLayer = ArenaBounds.WallLayer,
			CollisionMask = 0,
		};
		wall.AddChild(new CollisionShape2D
		{
			Shape = new RectangleShape2D { Size = size },
		});
		AddChild(wall);
	}
}
