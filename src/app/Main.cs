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

		// 加载场景预制体
		var playerScene = GD.Load<PackedScene>("res://scenes/player.tscn"); // 玩家场景
		var enemyScene = GD.Load<PackedScene>("res://scenes/enemy.tscn"); // 敌人场景

		// 加载角色、武器、敌人与刷怪配置（Ignore 避免 .tres 外改后仍用旧缓存）
		var swordData = LoadContent<WeaponData>("res://content/weapons/sword.tres");
		var playerData = LoadContent<CharacterData>("res://content/characters/wanderer.tres");
		var enemyData = LoadContent<EnemyData>("res://content/enemies/melee_grunt.tres");
		var spawnConfig = LoadContent<SpawnConfig>("res://content/spawn/basic_horde.tres");

		// 加载升级选项池（动态抽取，非写死三种）
		var upgradeAttack = LoadContent<UpgradeOptionData>("res://content/upgrades/attack.tres");
		var upgradeAttackSpeed = LoadContent<UpgradeOptionData>("res://content/upgrades/attack_speed.tres");
		var upgradeMoveSpeed = LoadContent<UpgradeOptionData>("res://content/upgrades/move_speed.tres");

		// 创建并配置玩家
		var player = playerScene.Instantiate<Player>(); // 玩家实例
		playerData.StartingWeapon ??= swordData; // 未配置初始武器时默认铁剑
		player.Data = playerData;
		player.GlobalPosition = Vector2.Zero; // 出生点：场景中心
		AddChild(player);
		player.Setup(playerData, swordData); // AddChild 后立即绑定武器（重开时 Autoload 仍存活）

		// 创建刷怪导演，负责按间隔在玩家周围生成敌人
		var director = new SpawnDirector
		{
			Name = "SpawnDirector",
			Config = spawnConfig,  // 刷怪配置
			EnemyScene = enemyScene, // 敌人场景
		};
		director.Config!.Enemy ??= enemyData; // 配置未指定敌人时使用默认近战小怪
		AddChild(director);

		// 挂载 UI 层
		AddChild(new Hud()); // 血条、经验、统计信息
		AddChild(new LevelUpPanel()); // 升级选项面板
		AddChild(new GameOverPanel()); // 结算面板
		AddChild(new PauseMenu()); // ESC 暂停菜单

		// 启动本局：传入玩家与升级池
		GameManager.Instance.BeginRun(
			player,
			[upgradeAttack, upgradeAttackSpeed, upgradeMoveSpeed],
			swordData);
	}

	/// <summary>从磁盘加载内容资源，重开场景时也读取最新 .tres。</summary>
	static T LoadContent<T>(string path) where T : Resource =>
		ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Ignore);

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
