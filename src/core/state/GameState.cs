namespace Rebirth.Core;

/// <summary>游戏全局状态机。Phase 2+ 才会用到 Shop / Encounter / Reincarnation。</summary>
public enum GameState
{
    Boot, // 启动/重载场景
    InRun, // 本局战斗进行中
    LevelUp, // 升级选择（游戏暂停）
    GameOver, // 本局结束
    Shop, // Phase 2：商店
    Encounter, // Phase 2：奇遇
    Reincarnation, // Phase 2：转生
}
