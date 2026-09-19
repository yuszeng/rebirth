# Rebirth — 技术架构

引擎：**Godot 4.4.1 + C#**（`Godot.NET.Sdk`）。**2D**（`Node2D` / `CharacterBody2D`）。逻辑用 C#，内容用 Resource（`.tres`）。

画面定案：2D **3/4 构图 + 房间锁定相机**，物理仍在 XY 平面。禁止改成 Godot 3D / `CharacterBody3D` 来模仿哈迪斯。

**进度：** Phase 1–4 已落地（俯视自动战斗 + 计时回合）；Phase 5a（装备栏 + 背包）进行中。房间战 / 3/4 相机 / 手动攻击未开工。不要为 Phase 6+ 或未批准的房间 Phase 提前写完整系统。

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
- 把项目迁到 3D，或用 3D 相机/网格角色「更像哈迪斯」

## 目录边界

```
src/core/              引擎级运行时（GameManager、EventBus）
  state/               流程与本局数据：GameState、RunState、PersistentState、RunResult
  utils/               基础设施：GameRng、GameLog、InputBindings
src/combat/            战斗
  damage/              伤害请求、结算、生命
  attack/              武器、攻击控制、目标选择（代码仍为自动攻击；转向后改为按键触发）
  skill/               技能冷却与释放（代码仍可自动放；转向后改为按键）
  spawn/               刷怪（代码仍为持续刷；转向后改为房间波次，仍走本入口）
  ai/                  敌人移动 / 接触攻击（房间战需可躲前摇，待对应 Phase）
src/character/         角色实体（Combatant、Player、Enemy）
  stats/               属性：StatType、StatModifier、CharacterStats
  equipment/           装备栏与护甲外观（不解析商店、不负责攻击）
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
| `RunState` | **仅这一世**的可变进度（现含回合计时、待选升级、金币、已拥有武器/护甲 Id、各槽已装备 Id；房间进度待房间 Phase） |
| `PersistentState` | 跨转生；仍只保留空壳类型，不实现存档 |
| `EventBus` | 全局 C# event。系统之间优先事件，而不是互相找节点硬引用 |
| `GameRng` | 唯一随机源：seed / weighted / choice / shuffle |
| `GameManager` | 开局、升级、商店、死亡结算、重开。当前还管回合计时；房间 Phase 改为管进房/清场/选门。不包含具体攻击或 AI；购买后把属性、武器或护甲交给对应模块 |

战斗与剧情解耦：无限模式应能只跑 Combat + Content，不依赖 World/Story。

## 属性

`StatType` + `StatModifier` + `CharacterStats`。

公式：`(base + flat) * (1 + percent)`。

所有来源（升级、装备、未来技能/加护/队友）只能通过 Modifier 改属性，禁止各系统直接改散落字段。

战斗实际生效：`MaxHp` `Attack` `AttackSpeed` `MoveSpeed` `AttackRange`，以及伤害结算中的 `Defense` `CritRate` `CritDamage` `Dodge` `Lifesteal` `DamageBonus`。  
`Area` `ProjectileSpeed` `Duration` 会放大对应攻击表现。法力、幸运、冷却缩减仍无独立系统。

## 攻击与伤害管道

```
Player 输入（转向后：攻击键 / 冲刺）
       → AttackController → Weapon → AttackPattern / TargetingSystem
       → DamageRequest → DamageSystem → Health → Death → EventBus

Player 输入（转向后：副技能键）
       → SkillController → AttackPattern / TargetingSystem / AreaHitSystem
       → DamageRequest → DamageSystem → Health → Death → EventBus
```

当前代码仍是 `AttackController` / `SkillController` 在 `_PhysicsProcess` 里自动放。转向后改触发条件，**不另起一套伤害管道**。

- Player **不知道**当前是不是剑，也不负责解析技能名
- 玩家普攻朝向用面向；需要索敌时问 TargetingSystem（范围伤走 AreaHitSystem），禁止在 Weapon / Skill 里写死「找最近」
- 任何扣血（含敌人接触伤害）都走 DamageSystem
- 暴击 / 防御 / 闪避 / 吸血 / 伤害加成在 `DamageSystem.Resolve` 结算；护盾与元素异常仍未做
- 冲刺无敌、受击硬直、房间相机属于角色/关卡，不要塞进 `DamageSystem`

## 数据驱动

内容用 Resource（`WeaponData`、`EquipmentData`、`EnemyData`、`UpgradeOptionData`、`CharacterData`、`ShopItemData`、`ShopConfig`、`CombatLoopConfig`、`SkillData`）。

新增一种剑、一种怪、一个升级选项：优先加 `.tres`，而不是改 match 字符串。

## 敌人与刷怪

- `Enemy` 读 `EnemyData`，AI 用可替换的 Movement / Attack behavior（Phase 1 为追逐 + 接触攻击）
- `SpawnDirector` 是唯一刷怪入口。当前按间隔持续刷；房间 Phase 改为本房 Wave，不要在 `Main` 另写生成器
- 3/4 表现：角色与装饰放在可 Y 排序的节点下；碰撞仍用 2D 形状，不要用 3D 碰撞「模拟深度」

## 相机与房间（待对应 Phase，不要先做空系统）

- 相机继续 `Camera2D`：对准当前房间，整房入画
- 房间数据用 Resource + 场景；`GameManager` 只切状态（进房 / 战斗中 / 清场 / 选门 / 商店），不画墙
- 未开工前不要创建完整 `RoomSystem` / `ChamberGraph` 空架

## 升级 UI

选项由数据池动态生成按钮。禁止三个写死按钮绑死 Attack/AttackSpeed/MoveSpeed。

## 扩展接口（有类型、无空系统）

以下类型可以存在，但 **没有完整玩法实现**，直到对应 Phase：

- `PersistentState`：字段与注释，不写存档读写
- `GameState` 预留状态值
- `TargetingSystem.Strategy` 预留策略枚举，Phase 1 只实现最近目标
- `DamageRequest.tags`：给未来元素/技能识别，当前不做分支

不要提前实现 Companion / Encounter / DemonKing 的运转逻辑。商店卖抽象可购买项（属性 Modifier、`WeaponData` 或 `EquipmentData` 引用）。护甲穿戴由 `EquipmentLoadout` 处理，武器攻击仍走 `AttackController`。
