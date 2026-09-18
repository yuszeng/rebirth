namespace Rebirth.Combat;

/// <summary>圆形攻击的过程判定。同一次攻击中，每个目标只受一次伤害。</summary>
public partial class CircleHitbox : Area2D
{
    readonly HashSet<ulong> _alreadyHit = [];
    CircleShape2D _circle = new();
    CollisionShape2D? _shapeNode;
    Combatant? _source;
    float _damage;
    string[] _tags = [];
    bool _armed;

    public override void _Ready()
    {
        CollisionLayer = 0;
        CollisionMask = 2;
        Monitoring = true;
        Monitorable = false;

        _shapeNode = GetNodeOrNull<CollisionShape2D>("CollisionShape2D") ?? new CollisionShape2D { Name = "CollisionShape2D" };
        if (_shapeNode.GetParent() == null)
        {
            AddChild(_shapeNode);
        }

        _shapeNode.Shape = _circle;
        BodyEntered += OnBodyEntered;
    }

    public void Arm(AttackVfxParams p)
    {
        _source = p.Source;
        _damage = p.Damage;
        _tags = p.DamageTags ?? [];
        _circle.Radius = Math.Max(p.Radius, 1f);
        _alreadyHit.Clear();
        _armed = p.Source != null && p.Damage > 0f;
    }

    public override void _PhysicsProcess(double _delta)
    {
        if (!_armed || GameManager.Instance.State != GameState.InRun)
        {
            return;
        }

        foreach (var body in GetOverlappingBodies())
        {
            TryHit(body);
        }
    }

    void OnBodyEntered(Node2D body) => TryHit(body);

    void TryHit(Node body)
    {
        if (!_armed || body is not Combatant target)
        {
            return;
        }

        if (target == _source || !GodotObject.IsInstanceValid(target))
        {
            return;
        }

        if (target.Health == null || target.Health.IsDead)
        {
            return;
        }

        if (!_alreadyHit.Add(target.GetInstanceId()))
        {
            return;
        }

        DamageSystem.Apply(new DamageRequest
        {
            Source = _source,
            Target = target,
            Amount = _damage,
            Tags = _tags,
        });
    }
}
