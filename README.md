# Rebirth

剑与魔法 Roguelike 动作游戏。玩家控制移动与走位，攻击与成长自动执行；长期目标是「转生 + 世界事件」，当前处于 **Phase 3（60 秒战斗回合 + 商店）**。

- 引擎：Godot 4.4 + C#
- 玩法逻辑：C#（`src/`）
- 数值与内容：Resource 数据（`content/*.tres`）

详细设计见 [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md)、[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md)、[`docs/ROADMAP.md`](docs/ROADMAP.md)。

---

## 快速开始

**环境要求：** Godot 4.4（含 .NET / C# 支持）、.NET 8 SDK

```bash
# 命令行编译（可选）
dotnet build Rebirth.csproj
```

在 Godot 编辑器中打开项目，运行主场景 `scenes/main.tscn`。

**操作：** WASD / 方向键移动 · 升级时点击选项 · 每回合 60 秒后进商店花金币购买/刷新 · 死亡后按 R 或点击按钮重开

---

## 架构原则

1. **模块边界清晰** — 玩法按目录拆分，不堆在 `Player` / `Main` 里
2. **数据驱动** — 新武器、新敌人、新升级项优先加 `.tres`，而不是改 if/else
3. **统一管道** — 扣血走 `DamageSystem`，随机走 `GameRng`，目标选择走 `TargetingSystem`
4. **本世 vs 跨世分离** — `RunState` 只管这一局，`PersistentState` 留给转生（Phase 1 仅占位）
5. **战斗与剧情解耦** — 无限模式应能只跑 Combat + Content，不依赖 World / Story

---

## 目录结构

```
rebirth/
├── src/                    # C# 源码
│   ├── app/                # 场景入口（Main：加载资源、创建玩家、启动本局）
│   ├── core/               # 运行时：GameManager、EventBus
│   │   ├── state/          # 流程枚举与本局/跨世数据
│   │   └── utils/          # RNG、日志、输入绑定
│   ├── combat/
│   │   ├── damage/         # 伤害与生命
│   │   ├── attack/         # 武器、自动攻击、目标选择
│   │   ├── spawn/          # 刷怪
│   │   └── ai/             # 敌人追逐 / 接触伤害
│   ├── character/          # Combatant、Player、Enemy
│   │   └── stats/          # 属性系统
│   ├── progression/        # 成长：经验、金币、升级选项、商店
│   ├── content/            # Resource 数据类型定义（CharacterData 等）
│   └── ui/                 # HUD、升级、商店、结算（只读 RunState）
├── content/                # .tres 内容数据（角色、武器、敌人、升级项、刷怪配置）
├── scenes/                 # Godot 场景（main / player / enemy）
└── docs/                   # 设计、架构、路线文档
```

未来模块预留目录（Phase 10+ 再实现）：

| 目录 | 用途 |
| --- | --- |
| `src/world/` | 世界状态 |
| `src/meta/` | 转生、羁绊、解锁 |

---

## 核心运行时

Autoload 全局单例（见 `project.godot`）：

| 对象 | 职责 |
| --- | --- |
| `GameManager` | 开局、回合计时、升级、商店、死亡结算、重开；不包含具体攻击或 AI |
| `EventBus` | 系统间 C# event 解耦（伤害、死亡、升级、金币等） |
| `GameRng` | 唯一随机源（seed / 加权抽取 / 洗牌） |

状态与数据：

| 类型 | 说明 |
| --- | --- |
| `GameState` | 流程枚举：`InRun` / `LevelUp` / `GameOver`（预留 Shop、Encounter、Reincarnation） |
| `RunState` | **仅这一世**的可变进度：等级、经验、金币、击杀、存活时间 |
| `PersistentState` | 跨转生存档边界（Phase 1 空壳，不读写） |
| `RunResult` | 一局结束时的只读结算快照 |

---

## 数据流概览

```
Main
 ├── 加载 .tres 资源（角色 / 武器 / 敌人 / 升级池 / 刷怪配置）
 ├── 实例化 Player + SpawnDirector
 ├── 挂载 Hud / LevelUpPanel / GameOverPanel
 └── GameManager.BeginRun()
         │
         ├── SpawnDirector：按间隔在玩家周围刷怪
         ├── Player：输入移动 → AttackController 自动攻击
         ├── Enemy：ChaseMovement 追逐 + ContactAttack 接触伤害
         │
         └── 敌人死亡
                 ├── Wallet → RunState.Gold
                 ├── ExperienceTracker → 可能升级
                 └── 升级 → 暂停 → UpgradeService 抽选项 → LevelUpPanel
```

---

## 战斗管道

所有扣血（含武器攻击与敌人接触）统一走同一条链路：

```
Player
  → AttackController          # 自动攻击循环（攻速冷却）
  → Weapon                    # 读 WeaponData，不内置找敌
  → TargetingSystem           # 按策略选目标（Phase 1：最近）
  → DamageRequest             # 伤害请求（来源、目标、数值、标签）
  → DamageSystem              # 唯一扣血入口（预留暴击/防御等）
  → Health                    # 生命值组件
  → EventBus.ActorDied        # 死亡通知 → GameManager 结算
```

敌人侧：

```
Enemy
  → ChaseMovement             # 朝玩家移动
  → ContactAttack             # 进入接触范围 → DamageSystem
```

---

## 属性系统

`StatType` + `StatModifier` + `CharacterStats`

```
最终值 = (base + flat) × (1 + percent)
```

升级、未来装备/技能/宝物都通过 `StatModifier` 叠加，禁止各系统直接改散落字段。

Phase 1 实际使用：`MaxHp`、`Attack`、`AttackSpeed`、`MoveSpeed`、`AttackRange`。

---

## 内容扩展方式

新增内容时，优先改数据而非改代码：

| 想加什么 | 做法 |
| --- | --- |
| 新武器 | 新建 `content/weapons/*.tres`（`WeaponData`） |
| 新敌人 | 新建 `content/enemies/*.tres`（`EnemyData`） |
| 新升级项 | 新建 `content/upgrades/*.tres`（`UpgradeOptionData`），加入 Main 的升级池 |
| 新刷怪规则 | 新建 `content/spawn/*.tres`（`SpawnConfig`） |

---

## 当前 Phase 1 范围

**已有：**

- 玩家移动、HP、死亡
- 铁剑自动攻击（范围、攻速、伤害）
- 一种近战敌人（追踪 + 接触伤害）
- 持续刷怪、击杀掉 XP 与金币
- 升级暂停 + 3 个动态属性选项
- Game Over 结算 + 重开

**不做（等后续 Phase）：** 魔法、技能、装备、商店、队友、转生、魔王、剧情

完整路线见 [`docs/ROADMAP.md`](docs/ROADMAP.md)。

---

## 开发约束（摘要）

- 使用 C#，不新增 GDScript 玩法代码
- 在现有 `core / combat / character / progression / content` 边界上扩展
- 禁止把商店/技能/装备塞进 `Player`
- 禁止各处直接 `GD.Randf()`，统一用 `GameRng`
- UI 只展示/请求 `RunState`，不保存进度

完整约束见 [`.cursor/rules/rebirth-architecture.mdc`](.cursor/rules/rebirth-architecture.mdc)。
