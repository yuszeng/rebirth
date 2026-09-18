# Rebirth — 开发路线

当前阶段：**Phase 4 已完成**（商店购买技能 + 按 E 切换攻击方式）。  
下一阶段：**Phase 5（装备 + 模块化外观）** — 未接到明确指令前不要开始。

规则：

1. 只实现当前被批准的 Phase 范围。
2. 完成后必须可运行，再等待人工指令进入下一 Phase。
3. 新 Phase 不能破坏旧 Phase（无技能也能打、无装备也能跑、无队友也能战、无剧情也能打）。
4. 开始新 Phase 时：先读 `GAME_DESIGN.md`、`ARCHITECTURE.md`、本文，再在现有架构上扩展。

## Phase 1 — 基础战斗 MVP（已完成）

- 玩家移动、HP、死亡
- 剑：自动攻击、范围、攻速、伤害
- 一种数据驱动近战敌人：追踪、接触伤害
- 持续刷怪
- 击杀 → XP、金币
- 升级选项动态生成（Phase 1 为攻 / 攻速 / 移速；战斗中弹窗后改为回合结束再选）
- Game Over 结算（存活时间、击杀、等级、金币）+ 重开

验收：Godot 打开 `scenes/main.tscn` 可完整进行一局。

## Phase 2 — XP + 属性成长（已完成）

在现有升级管道上扩展选项池（生命、暴击、闪避、范围等），不另写一套升级 UI。  
选项由 `content/upgrades/*.tres` 扫描加载。多数新属性已能作为 Modifier 挂上角色；`DamageSystem` 仍以直伤为主，暴击/防御/闪避等结算留给后续战斗扩展。

## Phase 3 — 商店 + 金币（已完成）

独立 `ShopService` + `ShopPanel`。战斗回合结束 → 清场 → 本回合待选升级一次选完 → 进店。  
商品是抽象可购买项（当前为属性修饰），商店不解析技能内部。金币走 `RunState` / `Wallet`。支持购买、刷新、空手离开后进入下一回合。战斗中升级只记账、不弹窗。玩家死亡立即结算，不进商店。

验收：Godot 打开 `scenes/main.tscn`，打完一回合可进店买/刷/离开，下一回合 HP 回满且等级与已购加成保留。

## Phase 4 — 技能系统（已完成）

数据驱动 `SkillData` + `SkillController`，已购技能可作为当前攻击方式按冷却自动执行。技能只从商店购买，升级池仍只出属性。  
技能与武器共用 `AttackPattern` 底层模式（回旋 / 点射 / 扇形 / 投掷物），禁止用技能 Id 字符串判断。无技能时剑的普攻不变。不做 QWER 技能栏、法力、命中触发、进化。

验收：商店买到技能后可按 E 切换为该攻击方式；同一技能不可重复购买；没买技能的一局仍可打完。

## Phase 5 — 装备 + 模块化外观（未开始）

Equipment 改属性和外观插槽。无装备时默认中立外观仍可用。

## Phase 6 — 魔法系统

与技能并列的自动施法管道，走同一套 Targeting / Damage。

## Phase 7 — 宝物系统

Treasure 作为效果来源（Modifier + 事件监听），不是新战斗循环。

## Phase 8 — 队友系统

独立 Companion 实体，自动战斗。没有队友时战斗不变。

## Phase 9 — 奇遇系统

数据驱动 Encounter。不要把事件写进 GameManager 的巨大 match。

## Phase 10 — 转生 + PersistentState

RunResult → PersistentState → NewRunState。严格分离本世与跨世数据。

## Phase 11 — 羁绊 + Companion Story

BondLevel / BondXP / 故事解锁。可作开局队友，但不锁死战斗。

## Phase 12 — 世界状态

事件驱动 WorldState（魔王、同伴、剧情旗标），不做全图 NPC 模拟。

## Phase 13 — 魔王系统

DemonKingSystem + State + Host，不是 `FinalBossDemonKing`。

## Phase 14 — 主线剧情

Story 与 Combat 解耦。无剧情时战斗仍可运行。

## Phase 15 — 无限模式

解锁后复用 Combat / Content，不依赖完整剧情。
