namespace Rebirth.Character;

/// <summary>玩家、敌人、未来队友的共同战斗实体。不要按职业再拆 Player 子类。</summary>
public partial class Combatant : CharacterBody2D
{
    public CharacterStats Stats { get; protected set; } = new(); // 属性集合（基础值 + 修饰器）
    public Health Health { get; private set; } = null!; // 生命值组件
    public float Radius { get; protected set; } = 16f; // 碰撞/视觉半径

    Polygon2D? _visual; // 圆形外观
    CollisionShape2D? _collision; // 圆形碰撞体

    /// <summary>确保 Health 节点存在并订阅死亡事件；缓存视觉与碰撞引用。</summary>
    protected void EnsureHealth()
    {
        Health = GetNodeOrNull<Health>("Health") ?? new Health { Name = "Health" };
        if (Health.GetParent() == null)
        {
            AddChild(Health);
        }

        Health.Died -= OnHealthDied;
        Health.Died += OnHealthDied;
        _visual = GetNodeOrNull<Polygon2D>("Visual");
        _collision = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
    }

    /// <summary>同步更新外观多边形与碰撞圆半径。</summary>
    protected void ApplyVisual(Color color, float radius)
    {
        Radius = radius;
        if (_visual != null)
        {
            _visual.Color = color;
            _visual.Polygon = MakeCircle(radius);
        }

        if (_collision?.Shape is CircleShape2D circle)
        {
            circle.Radius = radius;
        }
    }

    /// <summary>用正多边形近似圆形，用于 Polygon2D 渲染。</summary>
    static Vector2[] MakeCircle(float radius)
    {
        const int steps = 16; // 边数，16 足够平滑且轻量
        var points = new Vector2[steps];
        for (var i = 0; i < steps; i++)
        {
            var angle = Mathf.Tau * i / steps;
            points[i] = Vector2.FromAngle(angle) * radius;
        }

        return points;
    }

    void OnHealthDied(Node? source)
    {
        EventBus.Instance.EmitActorDied(this, source);
    }
}
