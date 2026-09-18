namespace Rebirth.Core;

/// <summary>游戏全局状态机。Encounter / Reincarnation 仍未实现。</summary>
public enum GameState
{
    Boot, // 启动/重载场景
    InRun, // 本局战斗进行中
    LevelUp, // 升级选择（游戏暂停）
    AttackModeSelect, // 攻击方式选择（游戏暂停）
    GameOver, // 本局结束
    Shop, // 回合结束后的商店
    Encounter, // 预留：奇遇
    Reincarnation, // 预留：转生
}
