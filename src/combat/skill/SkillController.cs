namespace Rebirth.Combat;

/// <summary>本局已购技能的冷却；只有当前选中的技能会自动执行。不解析技能名字符串。</summary>
public partial class SkillController : Node2D
{
    const float RetrySeconds = 0.2f;

    readonly List<SkillRuntime> _skills = [];
    Combatant? _owner;

    public IReadOnlyList<SkillRuntime> Skills => _skills;

    public override void _Ready()
    {
        _owner = GetParent() as Combatant;
        EventBus.Instance.AttackModeChanged += OnAttackModeChanged;
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance == null)
        {
            return;
        }

        EventBus.Instance.AttackModeChanged -= OnAttackModeChanged;
    }

    public void Clear() => _skills.Clear();

    public bool Has(string skillId) =>
        !string.IsNullOrEmpty(skillId) && _skills.Any(skill => skill.Data.Id == skillId);

    /// <summary>切到技能时从完整冷却开始，避免玩家频繁切换来绕过冷却。</summary>
    void OnAttackModeChanged(string modeId)
    {
        foreach (var skill in _skills)
        {
            skill.Remaining = skill.Data.Id == modeId
                ? Math.Max(skill.Data.Cooldown, 0.05f)
                : 0f;
        }
    }

    /// <summary>授予技能。同一 Id 不可重复持有。</summary>
    public bool TryGrant(SkillData? skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.Id) || Has(skill.Id))
        {
            return false;
        }

        _skills.Add(new SkillRuntime { Data = skill, Remaining = 0f });
        return true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (GameManager.Instance.State != GameState.InRun)
        {
            return;
        }

        var selectedModeId = GameManager.Instance.Run.SelectedAttackModeId;
        if (selectedModeId == RunState.BasicAttackModeId)
        {
            return; // 普通攻击模式下不释放技能
        }

        _owner ??= GetParent() as Combatant;
        if (_owner?.Health == null || _owner.Health.IsDead)
        {
            return;
        }

        var runtime = _skills.FirstOrDefault(skill => skill.Data.Id == selectedModeId);
        if (runtime == null)
        {
            return;
        }

        var dt = (float)delta;
        runtime.Remaining = Math.Max(runtime.Remaining - dt, 0f);
        if (runtime.Remaining > 0f)
        {
            return;
        }

        runtime.Remaining = TryCast(runtime.Data)
            ? Math.Max(runtime.Data.Cooldown, 0.05f)
            : RetrySeconds;
    }

    bool TryCast(SkillData data)
    {
        if (_owner == null || data.Pattern == null)
        {
            return false;
        }

        return AttackPatternExecutor.TryExecute(new AttackPatternRequest
        {
            Host = this,
            Source = _owner,
            Pattern = data.Pattern,
            Damage = ResolveDamage(_owner, data),
            DamageTags = data.DamageTags,
            Candidates = GetTree().GetNodesInGroup("enemies"),
        });
    }

    static float ResolveDamage(Combatant user, SkillData data) =>
        user.Stats.GetValue(StatType.Attack) * data.AttackScale + data.Damage;
}

/// <summary>运行时冷却。Data 来自 Resource，不可写回 .tres。</summary>
public sealed class SkillRuntime
{
    public required SkillData Data { get; init; }
    public float Remaining { get; set; }
}
