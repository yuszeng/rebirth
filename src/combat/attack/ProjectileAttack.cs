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
    readonly HashSet<ulong> _alreadyHit = [];
    int _maxHits = 1;
    int _hitCount;
    bool _configured;

    public static void Spawn(
        Node parent,
        Combatant source,
        Vector2 origin,
        Vector2 targetPoint,
        AttackPatternData pattern,
        float maxDistance,
        float damage,
        string[] tags,
        int maxHits = 1)
    {
        var direction = targetPoint - origin;
        if (direction.LengthSquared() < 0.0001f)
        {
            direction = Vector2.Right;
        }

        SpawnDirected(parent, source, origin, direction, pattern, maxDistance, damage, tags, maxHits);
    }

    public static void SpawnDirected(
        Node parent,
        Combatant source,
        Vector2 origin,
        Vector2 direction,
        AttackPatternData pattern,
        float maxDistance,
        float damage,
        string[] tags,
        int maxHits = 1)
    {
        var projectile = new ProjectileAttack
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        parent.AddChild(projectile);
        projectile.GlobalPosition = origin;
        projectile.Configure(source, direction, pattern, maxDistance, damage, tags, maxHits);
    }

    void Configure(
        Combatant source,
        Vector2 direction,
        AttackPatternData pattern,
        float maxDistance,
        float damage,
        string[] tags,
        int maxHits)
    {
        _source = source;
        _direction = direction.LengthSquared() < 0.0001f ? Vector2.Right : direction.Normalized();
        _speed = CombatStatScale.ProjectileSpeed(source, Math.Max(pattern.ProjectileSpeed, 1f));
        _radius = Math.Max(CombatStatScale.Range(source, pattern.ProjectileRadius), 1f);
        _remainingDistance = Math.Max(maxDistance, _radius);
        _damage = damage;
        _tags = tags;
        _maxHits = maxHits <= 0 ? int.MaxValue : maxHits;
        _alreadyHit.Clear();
        _hitCount = 0;
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

        if (GetTree().Paused)
        {
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

    /// <summary>返回 true 表示投射物达到命中上限并已排队销毁。</summary>
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
            UnhitCandidates(),
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
        _alreadyHit.Add(targets[0].GetInstanceId());
        _hitCount++;
        if (_hitCount < _maxHits)
        {
            return false;
        }

        QueueFree();
        return true;
    }

    IEnumerable<Node> UnhitCandidates()
    {
        foreach (var node in GetTree().GetNodesInGroup("enemies"))
        {
            if (node is not Combatant target || !GodotObject.IsInstanceValid(target))
            {
                continue;
            }

            if (!_alreadyHit.Contains(target.GetInstanceId()))
            {
                yield return target;
            }
        }
    }
}
