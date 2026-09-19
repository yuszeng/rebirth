namespace Rebirth.Combat;

/// <summary>固定环绕在施放者周围的伤害单位；它不是队友实体，每个伤害 tick 对同一目标最多命中一次。</summary>
public partial class OrbitingSatelliteAttack : Node2D
{
    const string GroupName = "orbiting_satellite_attacks";

    Combatant? _source;
    int _count;
    float _orbitRadius;
    float _satelliteRadius;
    float _angularSpeed;
    float _angle;
    float _hitInterval;
    float _untilHit;
    float _damagePerHit;
    string[] _tags = [];
    bool _configured;
    ulong _sourceInstanceId;

    public static void SpawnOrRefresh(
        Node parent,
        Combatant source,
        AttackPatternData pattern,
        float damage,
        string[] tags)
    {
        foreach (var node in parent.GetTree().GetNodesInGroup(GroupName))
        {
            if (node is OrbitingSatelliteAttack existing
                && existing._sourceInstanceId == source.GetInstanceId())
            {
                existing.Configure(source, pattern, damage, tags);
                return;
            }
        }

        var attack = new OrbitingSatelliteAttack
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        parent.AddChild(attack);
        attack.Configure(source, pattern, damage, tags);
    }

    void Configure(Combatant source, AttackPatternData pattern, float damage, string[] tags)
    {
        _source = source;
        _sourceInstanceId = source.GetInstanceId();
        _count = Math.Max(pattern.OrbitCount, 1);
        _orbitRadius = Math.Max(CombatStatScale.Range(source, pattern.OrbitRadius), 0f);
        _satelliteRadius = Math.Max(CombatStatScale.Range(source, pattern.SatelliteRadius), 1f);
        _angularSpeed = Mathf.DegToRad(pattern.OrbitSpeedDegrees);
        _hitInterval = Math.Max(pattern.OrbitHitInterval, 0.02f);
        _damagePerHit = damage * Math.Max(pattern.OrbitHitDamageScale, 0f);
        _tags = tags;
        _configured = true;
        GlobalPosition = source.GlobalPosition;
        AddToGroup(GroupName);
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

        if (_source == null || !GodotObject.IsInstanceValid(_source) || _source.Health.IsDead)
        {
            QueueFree();
            return;
        }

        var dt = (float)delta;
        GlobalPosition = _source.GlobalPosition;
        _angle += _angularSpeed * dt;
        _untilHit -= dt;
        while (_untilHit <= 0f)
        {
            ApplyHitTick();
            _untilHit += _hitInterval;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawArc(
            Vector2.Zero,
            _orbitRadius,
            0f,
            Mathf.Tau,
            48,
            new Color(0.55f, 0.72f, 1f, 0.28f),
            2f,
            true);
        for (var i = 0; i < _count; i++)
        {
            var position = SatelliteOffset(i);
            DrawCircle(position, _satelliteRadius, new Color(0.58f, 0.82f, 1f, 0.9f));
            DrawCircle(position, _satelliteRadius * 0.4f, new Color(0.95f, 0.99f, 1f, 0.95f));
        }
    }

    void ApplyHitTick()
    {
        if (_source == null || _damagePerHit <= 0f)
        {
            return;
        }

        var uniqueHits = new List<Combatant>();
        var hitIds = new HashSet<ulong>();
        var candidates = GetTree().GetNodesInGroup("enemies");
        for (var i = 0; i < _count; i++)
        {
            var hits = AreaHitSystem.Query(
                AreaShape.Circle(GlobalPosition + SatelliteOffset(i), 0f, _satelliteRadius),
                candidates,
                _source);
            foreach (var target in hits)
            {
                if (hitIds.Add(target.GetInstanceId()))
                {
                    uniqueHits.Add(target);
                }
            }
        }

        AreaHitSystem.Apply(new DamageRequest
        {
            Source = _source,
            Amount = _damagePerHit,
            Tags = _tags,
        }, uniqueHits);
    }

    Vector2 SatelliteOffset(int index)
    {
        var angle = _angle + Mathf.Tau * index / _count;
        return Vector2.FromAngle(angle) * _orbitRadius;
    }
}
