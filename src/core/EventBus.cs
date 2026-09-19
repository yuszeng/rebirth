namespace Rebirth.Core;

/// <summary>系统间解耦。用 C# event 才能安全传递 RunState 等自定义类型。</summary>
public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; } = null!; // 单例引用

    public event Action<RunState>? RunStarted; // 本局开始
    public event Action<RunResult>? RunEnded; // 本局结束
    public event Action<DamageResult>? DamageApplied; // 伤害已结算（含暴击/闪避）
    public event Action<Combatant, Node?>? ActorDied; // 战斗单位死亡（受害者 + 伤害来源）
    public event Action<int, int>? XpGained; // 获得经验（本次增量 + 当前总量）
    public event Action<int, int>? GoldGained; // 获得金币（本次增量 + 当前总量）
    public event Action<int>? LevelUp; // 升级（新等级）
    public event Action<IReadOnlyList<UpgradeOptionData>>? UpgradeOffered; // 弹出升级选项
    public event Action<UpgradeOptionData>? UpgradeChosen; // 玩家已选择升级
    public event Action<bool>? PauseChanged; // 玩家暂停菜单开关
    public event Action<ShopStock>? ShopOpened; // 进入商店并展示货架
    public event Action<ShopStock>? ShopChanged; // 购买或刷新后更新货架
    public event Action? ShopClosed; // 离开商店
    public event Action? CombatRoundEnded; // 回合结束清场（不发击杀奖励）
    public event Action? CombatRoundStarted; // 下一回合战斗开始
    public event Action<AttackModeStock>? AttackModeSelectionOpened; // 打开攻击方式选择
    public event Action? AttackModeSelectionClosed; // 关闭攻击方式选择
    public event Action<string>? AttackModeChanged; // 当前攻击方式已切换

    public override void _EnterTree()
    {
        Instance = this;
    }

    /// <summary>
    /// 场景节点会订阅本对象，但 Autoload 在重开时不会销毁。
    /// 必须清空回调，否则已释放的 HUD 一收到伤害/金币事件就会抛异常，后续经验与战斗回调被打断。
    /// </summary>
    public void ClearSceneSubscriptions()
    {
        RunStarted = null;
        RunEnded = null;
        DamageApplied = null;
        ActorDied = null;
        XpGained = null;
        GoldGained = null;
        LevelUp = null;
        UpgradeOffered = null;
        UpgradeChosen = null;
        PauseChanged = null;
        ShopOpened = null;
        ShopChanged = null;
        ShopClosed = null;
        CombatRoundEnded = null;
        CombatRoundStarted = null;
        AttackModeSelectionOpened = null;
        AttackModeSelectionClosed = null;
        AttackModeChanged = null;
    }

    public void EmitRunStarted(RunState run) => InvokeSafe(RunStarted, run); // 触发本局开始事件
    public void EmitRunEnded(RunResult result) => InvokeSafe(RunEnded, result); // 触发本局结束事件
    public void EmitDamageApplied(DamageResult result) => InvokeSafe(DamageApplied, result);
    public void EmitActorDied(Combatant victim, Node? source) => InvokeSafe(ActorDied, victim, source); // 触发战斗单位死亡事件
    public void EmitXpGained(int amount, int total) => InvokeSafe(XpGained, amount, total); // 触发获得经验事件
    public void EmitGoldGained(int amount, int total) => InvokeSafe(GoldGained, amount, total); // 触发获得金币事件
    public void EmitLevelUp(int newLevel) => InvokeSafe(LevelUp, newLevel); // 触发升级事件
    public void EmitUpgradeOffered(IReadOnlyList<UpgradeOptionData> options) => InvokeSafe(UpgradeOffered, options); // 触发弹出升级选项事件
    public void EmitUpgradeChosen(UpgradeOptionData option) => InvokeSafe(UpgradeChosen, option); // 触发玩家选择升级事件
    public void EmitPauseChanged(bool paused) => InvokeSafe(PauseChanged, paused); // 触发玩家暂停菜单开关事件
    public void EmitShopOpened(ShopStock stock) => InvokeSafe(ShopOpened, stock);
    public void EmitShopChanged(ShopStock stock) => InvokeSafe(ShopChanged, stock);
    public void EmitShopClosed() => InvokeSafe(ShopClosed);
    public void EmitCombatRoundEnded() => InvokeSafe(CombatRoundEnded);
    public void EmitCombatRoundStarted() => InvokeSafe(CombatRoundStarted);
    public void EmitAttackModeSelectionOpened(AttackModeStock stock) => InvokeSafe(AttackModeSelectionOpened, stock);
    public void EmitAttackModeSelectionClosed() => InvokeSafe(AttackModeSelectionClosed);
    public void EmitAttackModeChanged(string modeId) => InvokeSafe(AttackModeChanged, modeId);

    static void InvokeSafe(Action? handler)
    {
        if (handler == null)
        {
            return;
        }

        foreach (var d in handler.GetInvocationList())
        {
            try
            {
                ((Action)d)();
            }
            catch (Exception ex)
            {
                GameLog.Error($"EventBus 回调失败: {ex.Message}");
            }
        }
    }

    static void InvokeSafe<T>(Action<T>? handler, T arg) // 安全调用事件回调
    {
        if (handler == null)
        {
            return;
        }

        foreach (var d in handler.GetInvocationList())
        {
            try
            {
                ((Action<T>)d)(arg);
            }
            catch (Exception ex)
            {
                GameLog.Error($"EventBus 回调失败: {ex.Message}");
            }
        }
    }

    static void InvokeSafe<T1, T2>(Action<T1, T2>? handler, T1 arg1, T2 arg2)  
    {
        if (handler == null) // 事件回调为空时直接返回
        {
            return; // 直接返回
        }

        foreach (var d in handler.GetInvocationList()) // 遍历事件回调列表
        {
            try
            {
                ((Action<T1, T2>)d)(arg1, arg2);
            }
            catch (Exception ex)
            {
                GameLog.Error($"EventBus 回调失败: {ex.Message}"); // 记录错误日志
            }
        }
    }
}
