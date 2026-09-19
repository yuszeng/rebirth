namespace Rebirth.Combat;

/// <summary>前方长剑突刺：短时间内用胶囊碰撞检测贯穿命中，非飞行投射物。</summary>
public partial class PiercingThrustAttack : Node2D
{
    Combatant? _source;
    Vector2 _direction = Vector2.Right;
    float _range;
    float _halfWidth;
    float _remaining;
    float _damage;
    string[] _tags = [];
    readonly HashSet<ulong> _alreadyHit = [];
    int _maxHits = 1;
    bool _configured;

    public static void Spawn(
        Node parent,
        Combatant source,
        Vector2 targetPoint,
        AttackPatternData pattern,
        float range,
        float damage,
        string[] tags,
        int maxHits)
    {
        var direction = targetPoint - source.GlobalPosition;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector2.Right;
        }

        var thrust = new PiercingThrustAttack
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        parent.AddChild(thrust);
        thrust.Configure(source, direction, pattern, range, damage, tags, maxHits);
    }

    void Configure(
        Combatant source,
        Vector2 direction,
        AttackPatternData pattern,
        float range,
        float damage,
        string[] tags,
        int maxHits)
    {
        _source = source;
        _direction = direction.Normalized();
        _range = Math.Max(range, 1f);
        _halfWidth = Math.Max(CombatStatScale.Range(source, pattern.ProjectileRadius), 1f);
        _remaining = Math.Max(CombatStatScale.Duration(source, pattern.Duration), 0.05f);
        _damage = damage;
        _tags = tags;
        _maxHits = maxHits <= 0 ? int.MaxValue : maxHits;
        _alreadyHit.Clear();
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

        if (_source == null || !GodotObject.IsInstanceValid(_source) || _source.Health?.IsDead == true)
        {
            QueueFree();
            return;
        }

        UpdateTransform();
        ApplyHits();

        _remaining -= (float)delta;
        if (_remaining <= 0f)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawLine(
            Vector2.Zero,
            Vector2.Right * _range,
            new Color(0.9f, 0.92f, 1f, 0.9f),
            _halfWidth * 1.4f,
            antialiased: true);
        DrawLine(
            Vector2.Right * _range * 0.15f,
            Vector2.Right * _range,
            new Color(1f, 1f, 1f, 0.95f),
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
        Rotation = _direction.Angle();
        QueueRedraw();
    }

    void ApplyHits()
    {
        if (_source == null || _damage <= 0f || _alreadyHit.Count >= _maxHits)
        {
            return;
        }

        var hits = AreaHitSystem.Query(
                AreaShape.Capsule(GlobalPosition, _range, _halfWidth, _direction),
                GetTree().GetNodesInGroup("enemies"),
                _source)
            .Where(target => !_alreadyHit.Contains(target.GetInstanceId()))
            .OrderBy(target => (target.GlobalPosition - GlobalPosition).Dot(_direction))
            .Take(_maxHits - _alreadyHit.Count)
            .ToList();

        foreach (var target in hits)
        {
            DamageSystem.Apply(new DamageRequest
            {
                Source = _source,
                Target = target,
                Amount = _damage,
                Tags = _tags,
            });
            _alreadyHit.Add(target.GetInstanceId());
        }
    }
}
