namespace Rebirth.Combat;

/// <summary>持续光束运行时：跟随攻击者与初始目标，按固定间隔沿胶囊区域结算伤害。</summary>
public partial class SustainedBeamAttack : Node2D
{
    Combatant? _source;
    Combatant? _target;
    Vector2 _direction = Vector2.Right;
    float _range;
    float _halfWidth;
    float _remaining;
    float _tickInterval;
    float _untilTick;
    float _damagePerTick;
    string[] _tags = [];
    bool _configured;

    public static void Spawn(
        Node parent,
        Combatant source,
        Combatant target,
        AttackPatternData pattern,
        float range,
        float damage,
        string[] tags)
    {
        var beam = new SustainedBeamAttack
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        parent.AddChild(beam);
        beam.Configure(source, target, pattern, range, damage, tags);
    }

    void Configure(
        Combatant source,
        Combatant target,
        AttackPatternData pattern,
        float range,
        float damage,
        string[] tags)
    {
        _source = source;
        _target = target;
        _range = Math.Max(range, 1f);
        _halfWidth = Math.Max(CombatStatScale.Range(source, pattern.BeamWidth) * 0.5f, 1f);
        _remaining = Math.Max(CombatStatScale.Duration(source, pattern.Duration), 0.05f);
        _tickInterval = Math.Max(pattern.BeamTickInterval, 0.02f);
        _untilTick = 0f;
        _damagePerTick = damage * Math.Max(pattern.BeamTickDamageScale, 0f);
        _tags = tags;
        _configured = true;
        UpdateTransform();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_configured || GameManager.Instance.State != GameState.InRun)
        {
            QueueFree();
            return;
        }

        if (GetTree().Paused)
        {
            return;
        }

        if (_source == null || !GodotObject.IsInstanceValid(_source) || _source.Health.IsDead)
        {
            QueueFree();
            return;
        }

        if (TargetIsGone())
        {
            QueueFree();
            return;
        }

        UpdateTransform();

        var dt = (float)delta;
        _remaining -= dt;
        _untilTick -= dt;
        while (_untilTick <= 0f && _remaining >= 0f)
        {
            ApplyTick();
            _untilTick += _tickInterval;
        }

        if (_remaining <= 0f || TargetIsGone())
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawLine(
            Vector2.Zero,
            Vector2.Right * _range,
            new Color(0.45f, 0.85f, 1f, 0.9f),
            _halfWidth * 2f,
            antialiased: true);
        DrawLine(
            Vector2.Zero,
            Vector2.Right * _range,
            new Color(0.9f, 0.98f, 1f, 0.95f),
            Math.Max(_halfWidth * 0.45f, 1f),
            antialiased: true);
    }

    void UpdateTransform()
    {
        if (_source == null)
        {
            return;
        }

        GlobalPosition = _source.GlobalPosition;
        if (_target != null
            && GodotObject.IsInstanceValid(_target)
            && _target.Health != null
            && !_target.Health.IsDead)
        {
            var towardTarget = _target.GlobalPosition - GlobalPosition;
            if (towardTarget.LengthSquared() > 0.0001f)
            {
                _direction = towardTarget.Normalized();
            }
        }

        Rotation = _direction.Angle();
    }

    void ApplyTick()
    {
        if (_source == null || _damagePerTick <= 0f)
        {
            return;
        }

        var hits = AreaHitSystem.Query(
            AreaShape.Capsule(GlobalPosition, _range, _halfWidth, _direction),
            GetTree().GetNodesInGroup("enemies"),
            _source);
        AreaHitSystem.Apply(new DamageRequest
        {
            Source = _source,
            Amount = _damagePerTick,
            Tags = _tags,
        }, hits);
    }

    bool TargetIsGone() =>
        _target == null
        || !GodotObject.IsInstanceValid(_target)
        || _target.Health == null
        || _target.Health.IsDead;
}
