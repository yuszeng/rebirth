namespace Rebirth.Core;

/// <summary>全局游戏流程控制器：战斗回合、升级、商店、胜负。不解析商品内部效果。</summary>
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
    readonly ShopService _shop = new(); // 商店货架
    Player? _player; // 当前玩家引用（用于应用升级/商品）
    bool _shopAfterUpgrade; // 回合已结束，升级全部选完后再进商店
    bool _roundTimeUp; // 本回合战斗时间已到，避免每帧重复请求进店
    bool _roundFieldCleared; // 本回合结束清场只做一次
    int _shopPurchaseSerial; // 保证商店修饰器 SourceId 不冲突

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
        IEnumerable<ShopItemData>? shopPool = null,
        ShopConfig? shopConfig = null,
        CombatLoopConfig? combatLoop = null,
        ulong seed = 0)
    {
        GameRng.Instance.Reseed(seed); // 重置随机种子（0 表示随机）
        var roundDuration = combatLoop?.RoundDurationSeconds > 0f
            ? combatLoop.RoundDurationSeconds
            : 60f;
        Run = new RunState
        {
            XpToNext = ExperienceTracker.RequiredFor(1),
            RoundDurationSeconds = roundDuration,
        };
        _upgrades.Pool = upgradePool.ToList();
        _shop.Pool = shopPool?.ToList() ?? [];
        _shop.Config = shopConfig ?? new ShopConfig();
        _player = player;
        _shopAfterUpgrade = false;
        _roundTimeUp = false;
        _roundFieldCleared = false;
        _shopPurchaseSerial = 0;
        // 场景重载后 Autoload 仍存活，需重新绑定玩家属性与武器
        if (_player.Data != null)
        {
            _player.Setup(_player.Data, startingWeapon);
        }

        foreach (var weapon in _player.OwnedWeapons)
        {
            if (!string.IsNullOrEmpty(weapon.Id) && !Run.OwnedWeaponIds.Contains(weapon.Id))
            {
                Run.OwnedWeaponIds.Add(weapon.Id);
            }
        }

        Run.SelectedAttackModeId = startingWeapon?.Id ?? RunState.BasicAttackModeId;

        IsUserPaused = false;
        State = GameState.InRun;
        GetTree().Paused = false;
        EventBus.Instance.EmitRunStarted(Run);
        EventBus.Instance.EmitCombatRoundStarted();
    }

    public override void _Process(double delta) // 处理帧更新
    {
        // 本局进行中时累计存活时间与回合计时
        if (State == GameState.InRun && !GetTree().Paused)
        {
            Run.ElapsedSeconds += (float)delta;
            Run.RoundElapsedSeconds += (float)delta;
            if (Run.RoundElapsedSeconds >= Run.RoundDurationSeconds)
            {
                Run.RoundElapsedSeconds = Run.RoundDurationSeconds;
                // 推迟到本帧击杀/升级处理之后，保证进店前先结算升级
                if (!_roundTimeUp)
                {
                    _roundTimeUp = true;
                    CallDeferred(nameof(RequestShop));
                }
            }
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

        if (Input.IsActionJustPressed("attack_mode_menu") && State == GameState.InRun)
        {
            OpenAttackModeSelection();
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
            // 战斗中只记账，升级选择推迟到回合结束、进商店之前
            EventBus.Instance.EmitLevelUp(Run.Level);
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
            Run.PendingLevelUps = 0;
            if (_shopAfterUpgrade)
            {
                OpenShop();
                return;
            }

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

    /// <summary>升级选择期间重新抽一组选项，不消耗待选次数。</summary>
    public void RefreshUpgradeOptions()
    {
        if (State != GameState.LevelUp)
        {
            return;
        }

        var options = _upgrades.Offer();
        if (options.Count == 0)
        {
            return;
        }

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

        if (_shopAfterUpgrade)
        {
            // 先让升级面板根据非 LevelUp 状态隐藏，再尝试进店
            State = GameState.InRun;
            EventBus.Instance.EmitUpgradeChosen(option);
            RequestShop();
            return;
        }

        State = GameState.InRun;
        GetTree().Paused = false;
        // 必须在 State 切回 InRun 之后再发事件，UI 才能正确判断并隐藏面板
        EventBus.Instance.EmitUpgradeChosen(option);
    }

    /// <summary>回合结束：先清场，有待选升级则弹选择，全部选完再进商店。</summary>
    void RequestShop()
    {
        if (!Run.IsAlive || State == GameState.GameOver || State == GameState.Shop)
        {
            return;
        }

        PauseAndClearRound();

        if (State == GameState.LevelUp || Run.PendingLevelUps > 0)
        {
            _shopAfterUpgrade = true;
            if (State != GameState.LevelUp)
            {
                OfferUpgrade();
            }

            return;
        }

        OpenShop();
    }

    /// <summary>停战斗并清场。升级选择期间怪会冻在原地，不如先清掉。</summary>
    void PauseAndClearRound()
    {
        IsUserPaused = false;
        GetTree().Paused = true;
        EventBus.Instance.EmitPauseChanged(false);
        if (_roundFieldCleared)
        {
            return;
        }

        _roundFieldCleared = true;
        EventBus.Instance.EmitCombatRoundEnded();
    }

    void OpenShop()
    {
        if (!Run.IsAlive || State == GameState.GameOver || State == GameState.Shop)
        {
            return;
        }

        if (State == GameState.LevelUp || Run.PendingLevelUps > 0)
        {
            RequestShop();
            return;
        }

        PauseAndClearRound();
        _shopAfterUpgrade = false;
        State = GameState.Shop;
        _shop.OpenNewVisit(Run);
        EventBus.Instance.EmitShopOpened(VisibleShopStock());
    }

    /// <summary>用本局金币买当前货架上的一件商品。</summary>
    public void BuyShopItem(int slotIndex)
    {
        if (State != GameState.Shop)
        {
            return;
        }

        var pending = _shop.Peek(slotIndex);
        if (pending?.Weapon != null && (_player == null || !_player.CanGrantWeapon(pending.Weapon)))
        {
            return;
        }

        if (pending?.GrantsEquipment == true && (_player?.Loadout == null || !_player.Loadout.CanGrant(pending.Equipment)))
        {
            return;
        }

        var item = _shop.TryBuy(slotIndex, _wallet, Run);
        if (item == null)
        {
            return;
        }

        if (item.Weapon != null)
        {
            if (_player != null && _player.TryGrantWeapon(item.Weapon) && !Run.OwnedWeaponIds.Contains(item.Weapon.Id))
            {
                Run.OwnedWeaponIds.Add(item.Weapon.Id);
            }
        }
        else if (item.GrantsEquipment && item.Equipment != null)
        {
            GrantEquipment(item.Equipment);
        }
        else
        {
            _shopPurchaseSerial += 1;
            _player?.ApplyStatModifier(item.ToModifier($"shop_{Run.CombatRound}_{_shopPurchaseSerial}_{item.Id}"));
        }

        EventBus.Instance.EmitShopChanged(VisibleShopStock());
    }

    /// <summary>花费金币刷新四个槽位。</summary>
    public void RefreshShop()
    {
        if (State != GameState.Shop)
        {
            return;
        }

        if (!_shop.TryRefresh(_wallet, Run))
        {
            return;
        }

        EventBus.Instance.EmitShopChanged(VisibleShopStock());
    }

    /// <summary>战斗中打开攻击方式选择。选择期间暂停，避免玩家被 UI 操作惩罚。</summary>
    public void OpenAttackModeSelection()
    {
        if (State != GameState.InRun || !Run.IsAlive)
        {
            return;
        }

        IsUserPaused = false;
        State = GameState.AttackModeSelect;
        GetTree().Paused = true;
        EventBus.Instance.EmitPauseChanged(false);
        EventBus.Instance.EmitAttackModeSelectionOpened(BuildAttackModeStock());
    }

    /// <summary>选择当前攻击方式：必须是本局已拥有的武器。</summary>
    public void ChooseAttackMode(string modeId)
    {
        if (State != GameState.AttackModeSelect || !CanSelectAttackMode(modeId))
        {
            return;
        }

        if (_player != null && !_player.TryEquipWeapon(modeId))
        {
            return;
        }

        Run.SelectedAttackModeId = modeId;
        State = GameState.InRun;
        GetTree().Paused = false;
        EventBus.Instance.EmitAttackModeChanged(modeId);
        EventBus.Instance.EmitAttackModeSelectionClosed();
    }

    /// <summary>商店背包换装：护甲走 Loadout，武器走现有攻击切换。</summary>
    public void EquipFromBackpack(string itemId, bool isWeapon)
    {
        if (State != GameState.Shop || _player == null || string.IsNullOrEmpty(itemId))
        {
            return;
        }

        if (isWeapon)
        {
            if (!_player.TryEquipWeapon(itemId))
            {
                return;
            }

            Run.SelectedAttackModeId = itemId;
            EventBus.Instance.EmitAttackModeChanged(itemId);
        }
        else if (!(_player.Loadout?.TryEquip(itemId) ?? false))
        {
            return;
        }
        else
        {
            SyncEquippedEquipmentIds();
        }

        EventBus.Instance.EmitShopChanged(VisibleShopStock());
    }

    public void CancelAttackModeSelection()
    {
        if (State != GameState.AttackModeSelect)
        {
            return;
        }

        State = GameState.InRun;
        GetTree().Paused = false;
        EventBus.Instance.EmitAttackModeSelectionClosed();
    }

    AttackModeStock BuildAttackModeStock()
    {
        var options = _player?.OwnedWeapons
            .Where(weapon => !string.IsNullOrEmpty(weapon.Id))
            .Select(weapon => new AttackModeOption
            {
                Id = weapon.Id,
                DisplayName = weapon.DisplayName,
                Description = weapon.Description,
                Weapon = weapon,
            })
            .ToList() ?? [];

        return new AttackModeStock
        {
            SelectedModeId = Run.SelectedAttackModeId,
            Options = options,
        };
    }

    bool CanSelectAttackMode(string modeId) =>
        Run.OwnedWeaponIds.Contains(modeId);

    void GrantEquipment(EquipmentData equipment)
    {
        var loadout = _player?.Loadout;
        if (loadout == null || !loadout.TryGrant(equipment))
        {
            return;
        }

        if (!Run.OwnedEquipmentIds.Contains(equipment.Id))
        {
            Run.OwnedEquipmentIds.Add(equipment.Id);
        }

        loadout.TryAutoEquipIfEmpty(equipment);
        SyncEquippedEquipmentIds();
    }

    void SyncEquippedEquipmentIds()
    {
        Run.EquippedEquipmentIds.Clear();
        var loadout = _player?.Loadout;
        if (loadout == null)
        {
            return;
        }

        foreach (var slot in EquipmentSlots.All)
        {
            if (!EquipmentSlots.IsArmor(slot))
            {
                continue;
            }

            var item = loadout.GetEquippedArmor(slot);
            if (item != null)
            {
                Run.EquippedEquipmentIds[slot] = item.Id;
            }
        }
    }

    ShopStock VisibleShopStock()
    {
        var stock = _shop.Snapshot(Run);
        stock.EquipmentSlots = BuildEquipmentSlotViews();
        stock.Backpack = BuildBackpackViews();
        return stock;
    }

    List<EquipmentSlotView> BuildEquipmentSlotViews()
    {
        var views = new List<EquipmentSlotView>();
        var loadout = _player?.Loadout;
        foreach (var slot in EquipmentSlots.All)
        {
            if (slot == EquipmentSlotKind.Weapon)
            {
                var weapon = _player?.OwnedWeapons.FirstOrDefault(item => item.Id == Run.SelectedAttackModeId);
                views.Add(new EquipmentSlotView
                {
                    Slot = slot,
                    SlotName = EquipmentSlots.DisplayName(slot),
                    ItemId = weapon?.Id ?? "",
                    DisplayName = weapon?.DisplayName ?? "空",
                    Description = weapon?.Description ?? "",
                });
                continue;
            }

            var armor = loadout?.GetEquippedArmor(slot);
            views.Add(new EquipmentSlotView
            {
                Slot = slot,
                SlotName = EquipmentSlots.DisplayName(slot),
                ItemId = armor?.Id ?? "",
                DisplayName = armor?.DisplayName ?? "空",
                Description = armor?.Description ?? "",
            });
        }

        return views;
    }

    List<BackpackItemView> BuildBackpackViews()
    {
        var views = new List<BackpackItemView>();
        if (_player != null)
        {
            foreach (var weapon in _player.OwnedWeapons)
            {
                if (string.IsNullOrEmpty(weapon.Id))
                {
                    continue;
                }

                views.Add(new BackpackItemView
                {
                    Id = weapon.Id,
                    IsWeapon = true,
                    Slot = EquipmentSlotKind.Weapon,
                    SlotName = EquipmentSlots.DisplayName(EquipmentSlotKind.Weapon),
                    DisplayName = weapon.DisplayName,
                    Description = weapon.Description,
                    IsEquipped = weapon.Id == Run.SelectedAttackModeId,
                });
            }
        }

        var loadout = _player?.Loadout;
        if (loadout != null)
        {
            foreach (var armor in loadout.Owned)
            {
                var equipped = loadout.GetEquippedArmor(armor.Slot);
                views.Add(new BackpackItemView
                {
                    Id = armor.Id,
                    IsWeapon = false,
                    Slot = armor.Slot,
                    SlotName = EquipmentSlots.DisplayName(armor.Slot),
                    DisplayName = armor.DisplayName,
                    Description = armor.Description,
                    IsEquipped = equipped?.Id == armor.Id,
                });
            }
        }

        return views;
    }

    /// <summary>离开商店，清零回合计时并开始下一回合战斗。</summary>
    public void LeaveShop()
    {
        if (State != GameState.Shop || !Run.IsAlive)
        {
            return;
        }

        Run.CombatRound += 1;  // 回合数增加
        Run.RoundElapsedSeconds = 0f; // 回合已战斗时间清零
        _roundTimeUp = false; // 回合时间未到清零
        _shopAfterUpgrade = false; // 商店后升级清零
        _roundFieldCleared = false; // 回合结束清场清零
        State = GameState.InRun; // 进入下一回合战斗状态 
        if (_player != null)
        {
            _player.Health.HealFull(); // 重置玩家生命为满血
            _player.GlobalPosition = Vector2.Zero; // 与 Main 出生点一致：竞技场中心
            _player.Velocity = Vector2.Zero; // 避免残留移动速度
        }

        GetTree().Paused = false; // 解除暂停
        EventBus.Instance.EmitShopClosed(); // 触发商店关闭事件
        EventBus.Instance.EmitCombatRoundStarted(); // 触发下一回合战斗开始事件
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
