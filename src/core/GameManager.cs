namespace Rebirth.Core;

/// <summary>全局游戏流程控制器：本局状态、经验/金币/升级、胜负判定。</summary>
public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; } = null!; // 单例引用

    public GameState State { get; private set; } = GameState.Boot; // 当前游戏状态
    public RunState Run { get; private set; } = new(); // 本局进度
    public PersistentState Persistent { get; } = new(); // 跨转生存档（Phase 1 暂不使用）
    public RunResult? LastResult { get; private set; } // 最近一次结算结果
    public bool IsUserPaused { get; private set; } // 玩家 ESC 暂停（区别于升级/结算暂停）

    readonly ExperienceTracker _xp = new(); // 经验计算
    readonly Wallet _wallet = new(); // 金币管理
    readonly UpgradeService _upgrades = new(); // 升级选项抽取
    Player? _player; // 当前玩家引用（用于应用升级）

    public override void _EnterTree() // 进入树时设置单例引用
    {
        Instance = this; // 设置单例引用
        ProcessMode = ProcessModeEnum.Always; // 暂停时仍能处理重启输入
    }

    public override void _Ready() // 准备时注册输入绑定和绑定自动加载事件
    {
        InputBindings.Ensure(); // 注册 WASD / 方向键 / R / ESC
        BindAutoloadEvents(); // 绑定自动加载事件
    }

    /// <summary>Autoload 在清 EventBus 后必须重新订阅，否则击杀奖励会丢失。</summary>
    void BindAutoloadEvents() // 绑定自动加载事件
    {
        EventBus.Instance.ActorDied -= OnActorDied; // 取消订阅死亡事件
        EventBus.Instance.ActorDied += OnActorDied; // 订阅死亡事件
    }

    /// <summary>开始新的一局。</summary>
    public void BeginRun(
        Player player,
        IEnumerable<UpgradeOptionData> upgradePool,
        WeaponData? startingWeapon = null,
        ulong seed = 0)
    {
        GameRng.Instance.Reseed(seed); // 重置随机种子（0 表示随机）
        Run = new RunState { XpToNext = ExperienceTracker.RequiredFor(1) }; // 初始化本局数据
        _upgrades.Pool = upgradePool.ToList(); // 设置升级选项池
        _player = player; // 设置当前玩家引用
        // 场景重载后 Autoload 仍存活，需重新绑定玩家属性与武器
        if (_player.Data != null)
        {
            _player.Setup(_player.Data, startingWeapon); // 设置玩家数据和武器
        }

        IsUserPaused = false; // 设置用户暂停为 false
        State = GameState.InRun; // 进入游戏状态
        GetTree().Paused = false; // 解除暂停（GameOver/升级后可能仍为 true）
        EventBus.Instance.EmitRunStarted(Run); // 触发本局开始事件
    }

    public override void _Process(double delta) // 处理帧更新
    {
        // 本局进行中时累计存活时间
        if (State == GameState.InRun && !GetTree().Paused)
        {
            Run.ElapsedSeconds += (float)delta; // 累计存活时间
        }

        // 游戏结束后按 R 快速重开
        if (State == GameState.GameOver && Input.IsActionJustPressed("restart"))
        {
            Restart(); // 重开游戏
        }

        // ESC：战斗中打开/关闭暂停菜单；升级与结算界面不抢占
        if (Input.IsActionJustPressed("pause") && State == GameState.InRun)
        {
            if (IsUserPaused)
            {
                ResumeFromPause();
            }
            else
            {
                PauseRun();
            }
        }
    }

    /// <summary>角色死亡回调：玩家死亡则结束本局，敌人死亡则结算奖励。</summary>
    void OnActorDied(Combatant victim, Node? source)
    {
        if (State != GameState.InRun) // 本局进行中时才处理死亡事件
        {
            return; // 非本局进行中时直接返回
        }

        if (victim is Player) // 玩家死亡则结束本局
        {
            FinishRun(); // 结束本局
            return;
        }

        if (victim is not Enemy enemy) // 敌人死亡则结算奖励
        {
            return; // 非敌人死亡时直接返回
        }

        Run.KillCount += 1; // 击杀计数增加
        if (enemy.Data == null)
        {
            return; // 敌人数据为空时直接返回
        }

        _wallet.Add(Run, enemy.Data.Gold); // 金币增加
        EventBus.Instance.EmitGoldGained(enemy.Data.Gold, Run.Gold);
        var levels = _xp.Grant(Run, enemy.Data.Xp); // 可能连升多级
        EventBus.Instance.EmitXpGained(enemy.Data.Xp, Run.Xp); // 触发获得经验事件
        if (levels > 0)
        {
            EventBus.Instance.EmitLevelUp(Run.Level); // 触发升级事件
            OfferUpgrade();
        }
    }

    /// <summary>暂停游戏并弹出升级选项；无可用选项则跳过。</summary>
    void OfferUpgrade()
    {
        if (Run.PendingLevelUps <= 0) // 没有待选升级时直接返回
        {
            return;
        }

        var options = _upgrades.Offer(); // 从池中加权随机抽取
        if (options.Count == 0)
        {
            Run.PendingLevelUps = 0; // 待选升级数量归零    
            State = GameState.InRun;
            GetTree().Paused = false;
            return;
        }

        IsUserPaused = false; // 升级界面优先于 ESC 暂停菜单
        State = GameState.LevelUp;
        GetTree().Paused = true; // 升级选择期间暂停战斗
        EventBus.Instance.EmitPauseChanged(false);
        EventBus.Instance.EmitUpgradeOffered(options);
    }

    /// <summary>玩家选定一项升级后应用，若还有待选则继续弹出。</summary>
    public void ChooseUpgrade(UpgradeOptionData option)
    {
        if (State != GameState.LevelUp) // 升级选择期间才处理选择事件
        {
            return; // 非升级选择期间直接返回
        }

        _player?.ApplyUpgrade(option); // 应用升级
        Run.PendingLevelUps = Math.Max(Run.PendingLevelUps - 1, 0); // 待选升级数量减少
        if (Run.PendingLevelUps > 0)
        {
            OfferUpgrade(); // 连升时保持 LevelUp，OnOffered 会刷新面板
            EventBus.Instance.EmitUpgradeChosen(option); // 触发玩家选择升级事件
            return; // 还有待选升级时继续弹出
        }

        State = GameState.InRun;
        GetTree().Paused = false;
        // 必须在 State 切回 InRun 之后再发事件，UI 才能正确判断并隐藏面板
        EventBus.Instance.EmitUpgradeChosen(option);
    }

    /// <summary>玩家死亡，生成本局结算并暂停。</summary>
    void FinishRun()
    {
        Run.IsAlive = false; // 设置本局已结束
        LastResult = Run.ToResult(); // 转换为结算结果
        IsUserPaused = false;
        State = GameState.GameOver; // 进入游戏结束状态
        GetTree().Paused = true; // 暂停游戏
        EventBus.Instance.EmitPauseChanged(false);
        EventBus.Instance.EmitRunEnded(LastResult); // 触发本局结束事件
    }

    /// <summary>玩家暂停战斗，弹出暂停菜单。</summary>
    public void PauseRun()
    {
        if (State != GameState.InRun || IsUserPaused)
        {
            return;
        }

        IsUserPaused = true;
        GetTree().Paused = true;
        EventBus.Instance.EmitPauseChanged(true);
    }

    /// <summary>从暂停菜单回到战斗。</summary>
    public void ResumeFromPause()
    {
        if (!IsUserPaused)
        {
            return;
        }

        IsUserPaused = false;
        if (State == GameState.InRun)
        {
            GetTree().Paused = false;
        }

        EventBus.Instance.EmitPauseChanged(false);
    }

    /// <summary>重载当前场景，回到 Boot 状态。</summary>
    public void Restart()
    {
        IsUserPaused = false;
        State = GameState.Boot;
        GetTree().Paused = false;
        // Autoload 的 EventBus 会带着上一局已销毁节点的订阅；先清空再挂回 GameManager
        EventBus.Instance.ClearSceneSubscriptions();
        BindAutoloadEvents();
        // 延迟重载，确保 unpause 先生效；否则新场景可能继承 Paused=true，_PhysicsProcess 全部停住
        CallDeferred(nameof(ReloadCurrentScene));
    }

    void ReloadCurrentScene()
    {
        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }
}
