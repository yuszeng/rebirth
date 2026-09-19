namespace Rebirth.Combat;

/// <summary>固定世界落点的预警攻击；倒计时结束时只进行一次圆形直伤结算。</summary>
public partial class DelayedExplosionAttack : Node2D
{
    Combatant? _source;
    float _delay;
    float _radius;
    float _damage;
    float _flashRemaining;
    string[] _tags = [];
    bool _configured;
    bool _exploded;

    public static void Spawn(
        Node parent,
        Combatant source,
        Vector2 position,
        AttackPatternData pattern,
        float damage,
        string[] tags)
    {
        var explosion = new DelayedExplosionAttack
        {
            ProcessMode = ProcessModeEnum.Always,
        };
        parent.AddChild(explosion);
        explosion.GlobalPosition = position;
        explosion.Configure(source, pattern, damage, tags);
    }

    void Configure(Combatant source, AttackPatternData pattern, float damage, string[] tags)
    {
        _source = source;
        _delay = CombatStatScale.Duration(source, Math.Max(pattern.Delay, 0f));
        _radius = Math.Max(CombatStatScale.Range(source, pattern.EffectRadius), 1f);
        _damage = damage;
        _tags = tags;
        _configured = true;
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
        if (!_exploded)
        {
            _delay -= dt;
            QueueRedraw();
            if (_delay <= 0f)
            {
                Explode();
            }
            return;
        }

        _flashRemaining -= dt;
        if (_flashRemaining <= 0f)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        var color = _exploded
            ? new Color(1f, 0.72f, 0.24f, 0.85f)
            : new Color(1f, 0.42f, 0.2f, 0.75f);
        DrawArc(Vector2.Zero, _radius, 0f, Mathf.Tau, 40, color, _exploded ? 8f : 3f, true);

        if (_exploded)
        {
            DrawCircle(Vector2.Zero, _radius * 0.9f, new Color(1f, 0.55f, 0.15f, 0.22f));
        }
    }

    void Explode()
    {
        if (_source == null)
        {
            return;
        }

        _exploded = true;
        _flashRemaining = 0.14f;
        var hits = AreaHitSystem.Query(
            AreaShape.Circle(GlobalPosition, 0f, _radius),
            GetTree().GetNodesInGroup("enemies"),
            _source);
        AreaHitSystem.Apply(new DamageRequest
        {
            Source = _source,
            Amount = _damage,
            Tags = _tags,
        }, hits);
        QueueRedraw();
    }
}
