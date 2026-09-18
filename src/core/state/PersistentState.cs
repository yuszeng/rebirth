namespace Rebirth.Core;

/// <summary>跨转生数据。Phase 1 不读写存档，仅保留边界。</summary>
public partial class PersistentState : RefCounted
{
    public int ReincarnationCount { get; set; } // 转生次数
    public Godot.Collections.Array<string> Unlocks { get; set; } = []; // 已解锁内容 ID 列表
}
