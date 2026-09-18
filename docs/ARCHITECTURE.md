# Rebirth — 技术架构

引擎：**Godot 4.4.1 + C#**（`Godot.NET.Sdk`）。2D。逻辑用 C#，内容用 Resource（`.tres`）。

**进度：** Phase 1–4 已落地（战斗 MVP、升级池、回合商店、攻击方式切换）。不要为 Phase 5+ 提前写完整系统。

原则：**当前内容极简，模块边界清晰。**  
不为未来提前写完整系统；但当前系统若必然扩展，则留下稳定接口。

实现任何功能前：先读 `GAME_DESIGN.md`、本文、`ROADMAP.md`，在**现有目录和类型上扩展**，不要另起一套。

## 禁止事项

- 把玩法写死在 Player / Enemy / Main 里
- 用大量 if/else 判断具体技能名、角色类型字符串
- 把商店/技能/装备/队友逻辑塞进 Player
- 把 Weapon 写死成只能是剑
- 把 Enemy 写死成只能是一种怪
- 直接 `enemy.hp -= 10`
- 各处直接调用全局 `randf()`（使用 `GameRng`）
- 让 UI 节点本身成为存档数据
- 创建 `SwordmanPlayer` / `FinalBossDemonKing` 这类写死身份的类型
- 为未开始的 Phase 创建大量空类

## 目录边界

```
src/core/              引擎级运行时（GameManager、EventBus）
  state/               流程与本局数据：GameState、RunState、PersistentState、RunResult
  utils/               基础设施：GameRng、GameLog、InputBindings
src/combat/            战斗
  damage/              伤害请求、结算、生命
  attack/              武器、自动攻击、目标选择
  skill/               技能攻击方式与冷却
  spawn/               刷怪
  ai/                  敌人移动 / 接触攻击
src/character/         角色实体（Combatant、Player、Enemy）
  stats/               属性：StatType、StatModifier、CharacterStats
src/progression/       成长：经验、金币、升级选项、商店
src/content/           数据资源类型（SkillData 等按 Phase 增加）
src/world/             世界状态（Phase 12 前不要堆实现）
src/meta/              转生/羁绊/解锁（Phase 10+）
src/ui/                HUD / 升级 / 商店 / 结算，只读 RunState
content/               .tres 内容数据
scenes/                场景
```

武器脚本在 `combat/attack`（攻击管道），不是挂在 Player 上。不要建顶层 `Utils/` / `Variables/` 杂物目录。

未来系统按模块进目录，而不是挂到 Player 上。

## 核心运行时

| 对象 | 职责 |
| --- | --- |
| `GameState` | 流程枚举。当前使用 `InRun` / `LevelUp` / `Shop` / `GameOver`。预留 `Encounter` / `Reincarnation` |
| `RunState` | **仅这一世**的可变进度（含回合计时、待选升级、金币、已购技能 Id） |
| `PersistentState` | 跨转生；仍只保留空壳类型，不实现存档 |
| `EventBus` | 全局 C# event。系统之间优先事件，而不是互相找节点硬引用 |
| `GameRng` | 唯一随机源：seed / weighted / choice / shuffle |
| `GameManager` | 开局、回合计时、升级、商店、死亡结算、重开。不包含具体攻击或 AI；购买后把属性或技能交给对应模块 |

战斗与剧情解耦：无限模式应能只跑 Combat + Content，不依赖 World/Story。

## 属性

`StatType` + `StatModifier` + `CharacterStats`。

公式：`(base + flat) * (1 + percent)`。

所有来源（升级、未来装备/技能/加护/队友）只能通过 Modifier 改属性，禁止各系统直接改散落字段。

战斗实际生效：`MaxHp` `Attack` `AttackSpeed` `MoveSpeed` `AttackRange`。  
升级/商店池已可挂更多 `StatType`（防御、暴击、闪避等），但 `DamageSystem` 仍只做直伤下限截断，不要提前写完整暴击/护盾系统。

## 攻击与伤害管道

```
Player → AttackController → Weapon → TargetingSystem
       → DamageRequest → DamageSystem → Health → Death → EventBus

Player → SkillController → TargetingSystem / AreaHitSystem
       → DamageRequest → DamageSystem → Health → Death → EventBus
```

- Player **不知道**当前是不是剑，也不负责技能释放
- Weapon / Skill **不内置**“找最近敌人”，一律问 TargetingSystem（范围伤走 AreaHitSystem）
- 任何扣血（含敌人接触伤害）都走 DamageSystem
- 暴击/防御/元素/护盾/吸血/异常在 DamageSystem **预留计算插口**，当前仍只做直伤

## 数据驱动

内容用 Resource（`WeaponData`、`EnemyData`、`UpgradeOptionData`、`CharacterData`、`ShopItemData`、`ShopConfig`、`CombatLoopConfig`、`SkillData`）。

新增一种剑、一种怪、一个升级选项：优先加 `.tres`，而不是改 match 字符串。

## 敌人与刷怪

- `Enemy` 读 `EnemyData`，AI 用可替换的 Movement / Attack behavior（Phase 1 为追逐 + 接触攻击）
- `SpawnDirector` 是最小刷怪器。未来升级为 Wave，而不是另写一套生成入口

## 升级 UI

选项由数据池动态生成按钮。禁止三个写死按钮绑死 Attack/AttackSpeed/MoveSpeed。

## 扩展接口（有类型、无空系统）

以下类型可以存在，但 **没有完整玩法实现**，直到对应 Phase：

- `PersistentState`：字段与注释，不写存档读写
- `GameState` 预留状态值
- `TargetingSystem.Strategy` 预留策略枚举，Phase 1 只实现最近目标
- `DamageRequest.tags`：给未来元素/技能识别，当前不做分支

不要提前实现 Equipment / Companion / Encounter / DemonKing 的运转逻辑。商店卖抽象可购买项（属性 Modifier 或 SkillData 引用）。
