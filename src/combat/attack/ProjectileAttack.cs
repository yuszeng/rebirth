namespace Rebirth.Combat;

/// <summary>通用投掷物攻击：只负责飞行与命中，伤害结算仍交给 DamageSystem。</summary>
public partial class ProjectileAttack : Node2D
{
    Combatant? _source;
    Vector2 _direction = Vector2.Right;
    float _speed = 360f;
    float _radius = 8f;
    float _remainingDistance = 160f;
    float _damage;
    string[] _tags = [];
    bool _configured;

    public static void Spawn(
        Node parent,
        Combatant source,
        Vector2 origin,
        Vector2 targetPoint,
        AttackPatternData pattern,
        float maxDistance,
        float damage,
        string[] tags)
    {
        var direction = targetPoint - origin;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector2.Right;
        }

        var projectile = new ProjectileAttack();
        parent.AddChild(projectile);
        projectile.GlobalPosition = origin;
        projectile.Configure(source, direction, pattern, maxDistance, damage, tags);
    }

    void Configure(Combatant source, Vector2 direction, AttackPatternData pattern, float maxDistance, float damage, string[] tags)
    {
        _source = source;
        _direction = direction.Normalized();
        _speed = Math.Max(pattern.ProjectileSpeed, 1f);
        _radius = Math.Max(pattern.ProjectileRadius, 1f);
        _remainingDistance = Math.Max(maxDistance, _radius);
        _damage = damage;
        _tags = tags;
        _configured = true;

        Rotation = _direction.Angle();
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_configured || GameManager.Instance.State != GameState.InRun)
        {
            QueueFree();
            return;
        }

        if (_source == null || !GodotObject.IsInstanceValid(_source) || _source.Health?.IsDead == true)
        {
            QueueFree();
            return;
        }

        var travel = Math.Min(_speed * (float)delta, _remainingDistance);
        if (TryHitAlong(travel))
        {
            return;
        }

        GlobalPosition += _direction * travel;
        _remainingDistance -= travel;
        if (_remainingDistance <= 0f)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, _radius, new Color(0.95f, 0.82f, 0.35f, 0.95f));
        DrawLine(Vector2.Zero, Vector2.Left * _radius * 1.6f, new Color(1f, 0.95f, 0.65f, 0.7f), _radius * 0.6f);
    }

    bool TryHitAlong(float travel)
    {
        var step = Math.Max(_radius * 0.75f, 2f);
        var checks = Math.Max(1, Mathf.CeilToInt(travel / step));
        for (var i = 1; i <= checks; i++)
        {
            var t = travel * i / checks;
            if (TryHitAt(GlobalPosition + _direction * t))
            {
                return true;
            }
        }

        return false;
    }

    bool TryHitAt(Vector2 position)
    {
        var targets = TargetingSystem.Select(
            position,
            0f,
            GetTree().GetNodesInGroup("enemies"),
            _radius,
            TargetingStrategy.Nearest,
            maxTargets: 1);
        if (targets.Count == 0)
        {
            return false;
        }

        DamageSystem.Apply(new DamageRequest
        {
            Source = _source,
            Target = targets[0],
            Amount = _damage,
            Tags = _tags,
        });
        QueueFree();
        return true;
    }
}
