namespace Rebirth.Core;

/// <summary>攻击方式选择面板的只读快照。</summary>
public sealed class AttackModeStock
{
    public string SelectedModeId { get; init; } = RunState.BasicAttackModeId;
    public IReadOnlyList<AttackModeOption> Options { get; init; } = [];
}

/// <summary>一个可选攻击方式：本局已拥有的武器。</summary>
public sealed class AttackModeOption
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public WeaponData? Weapon { get; init; }
}
