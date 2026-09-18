namespace Rebirth.Combat;

/// <summary>
/// 挂在挥砍 Sweep 下的判定盒：随剑刃旋转，用物理重叠扣血。
/// 同一挥砍对同一目标只结算一次；每帧再用扫过的四边形做查询，避免转太快穿透。
/// </summary>
public partial class SweepHitbox : Area2D
{
    [Export] public float Thickness { get; set; } = 16f;

    readonly HashSet<ulong> _alreadyHit = []; // 已命中目标集合
    RectangleShape2D _bladeRect = new(); // 剑刃矩形
    CollisionShape2D? _shapeNode; // 碰撞形状节点
    Combatant? _source; // 源
    float _innerRadius; // 内半径
    float _outerRadius; // 外半径
    float _prevAngle; // 上一帧角度
    float _damage; // 伤害
    string[] _tags = []; // 伤害标签
    bool _armed; // 是否已准备好

    public override void _Ready() // 准备时设置碰撞形状和事件
    {
        CollisionLayer = 0; // 碰撞层为 0，不与其他物体碰撞
        CollisionMask = 2; // 与 Enemy.CollisionLayer 一致，只扫敌人
        Monitoring = true; // 监控为 true，可以检测碰撞
        Monitorable = false; // 可监控为 false，不会被其他物体监控
        // 获取碰撞形状节点，如果不存在则创建一个
        _shapeNode = GetNodeOrNull<CollisionShape2D>("CollisionShape2D") ?? new CollisionShape2D { Name = "CollisionShape2D" }; // 获取碰撞形状节点
        if (_shapeNode.GetParent() == null)
        {
            AddChild(_shapeNode); // 添加碰撞形状节点
        }

        _shapeNode.Shape = _bladeRect; // 设置碰撞形状
        BodyEntered += OnBodyEntered; // 添加碰撞进入事件
    }

    /// <summary>开始一次挥砍判定。damage 为 0 时只显示不打人。</summary>
    public void Arm(AttackVfxParams p, float innerRadius, float outerRadius)
    {
        _source = p.Source; // 设置源
        _damage = p.Damage; // 设置伤害
        _tags = p.DamageTags ?? [];
        _innerRadius = innerRadius; // 设置内半径
        _outerRadius = outerRadius; // 设置外半径
        _alreadyHit.Clear(); // 清除已命中目标
        LayoutBladeRect();
        _prevAngle = GetParent<Node2D>()?.Rotation ?? Rotation;
        _armed = p.Source != null && p.Damage > 0f;
    }

    public override void _PhysicsProcess(double _delta) // 物理处理
    {
        if (!_armed || GameManager.Instance.State != GameState.InRun)
        {
            return; // 如果未准备好或游戏状态不是进行中则直接返回
        }

        foreach (var body in GetOverlappingBodies()) // 获取碰撞体  
        {
            TryHit(body);
        }

        QuerySweptVolume();
    }

    void OnBodyEntered(Node2D body) => TryHit(body); // 尝试命中目标

    void LayoutBladeRect()
    {
        var length = Math.Max(_outerRadius - _innerRadius, 8f);
        _bladeRect.Size = new Vector2(length, Thickness);
        Position = new Vector2(_innerRadius + length * 0.5f, 0f);
    }

    /// <summary>用上一帧到当前帧剑刃扫过的四边形做物理查询，补上 Area2D 可能漏掉的穿透。</summary>
    void QuerySweptVolume()
    {
        var sweep = GetParent<Node2D>();
        var root = sweep?.GetParent<Node2D>();
        if (sweep == null || root == null)
        {
            return;
        }

        var current = sweep.Rotation;
        var span = Mathf.Abs(Mathf.AngleDifference(_prevAngle, current));
        if (span >= 0.02f)
        {
            var shape = new ConvexPolygonShape2D
            {
                Points =
                [
                    Vector2.FromAngle(_prevAngle) * _innerRadius,
                    Vector2.FromAngle(_prevAngle) * _outerRadius,
                    Vector2.FromAngle(current) * _outerRadius,
                    Vector2.FromAngle(current) * _innerRadius,
                ],
            };
            QueryShape(shape, root.GlobalTransform);
        }

        _prevAngle = current;
    }

    void QueryShape(Shape2D shape, Transform2D transform)
    {
        var exclude = new Godot.Collections.Array<Rid>();
        if (_source != null && GodotObject.IsInstanceValid(_source))
        {
            exclude.Add(_source.GetRid());
        }

        var query = new PhysicsShapeQueryParameters2D
        {
            Shape = shape,
            Transform = transform,
            CollisionMask = CollisionMask,
            CollideWithBodies = true,
            CollideWithAreas = false,
            Exclude = exclude,
        };
        foreach (var hit in GetWorld2D().DirectSpaceState.IntersectShape(query, 32))
        {
            if (hit.TryGetValue("collider", out var collider) && collider.AsGodotObject() is Node node)
            {
                TryHit(node);
            }
        }
    }

    void TryHit(Node body) // 尝试命中目标
    {
        if (!_armed || body is not Combatant target)
        {
            return; // 没有武器数据则直接返回
        }

        if (target == _source || !GodotObject.IsInstanceValid(target)) // 如果目标与源相同或目标无效则直接返回
        {
            return;
        }

        var id = target.GetInstanceId(); // 获取目标实例 ID
        if (!_alreadyHit.Add(id))
        {
            return; // 如果已命中目标则直接返回
        }

        DamageSystem.Apply(new DamageRequest // 应用伤害
        {
            Source = _source, // 设置源
            Target = target, // 设置目标
            Amount = _damage, // 设置伤害
            Tags = _tags, // 设置伤害标签
        }); // 应用伤害
    }
}
