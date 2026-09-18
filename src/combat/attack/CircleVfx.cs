namespace Rebirth.Combat;

/// <summary>圆形攻击表现：贴图绕玩家旋转，Hitbox 在持续时间内负责过程命中。</summary>
public partial class CircleVfx : AttackVfx
{
    Node2D? _spin;
    CircleHitbox? _hitbox;
    readonly List<Sprite2D> _blades = [];
    float _innerRadius = 8f;
    float _outerRadius = 80f;
    bool _drawProceduralRing = true;
    Color _ringColor = new(0.55f, 0.85f, 1f, 0.26f);

    public override void Play(AttackVfxParams p)
    {
        _spin = GetNodeOrNull<Node2D>("Spin") ?? this;
        _hitbox = GetNodeOrNull<CircleHitbox>("Hitbox");
        _innerRadius = Math.Max(p.InnerRadius, 4f);
        _outerRadius = Math.Max(p.Radius, _innerRadius + 8f);
        _drawProceduralRing = p.Texture == null;

        LayoutBlades(p.Texture);
        _hitbox?.Arm(p);

        var duration = Math.Max(p.Duration, 0.08f);
        var spin = _spin ?? this;
        var tween = CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Physics);
        tween.TweenProperty(spin, "rotation", p.Clockwise ? Mathf.Tau : -Mathf.Tau, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        foreach (var blade in _blades)
        {
            tween.Parallel().TweenProperty(blade, "modulate:a", 0f, duration * 0.35f)
                .SetDelay(duration * 0.65f)
                .SetEase(Tween.EaseType.In);
        }

        tween.TweenCallback(Callable.From(QueueFree));
    }

    public override void _Process(double _delta)
    {
        if (_drawProceduralRing)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!_drawProceduralRing)
        {
            return;
        }

        DrawCircle(Vector2.Zero, _outerRadius, _ringColor);
        DrawArc(Vector2.Zero, _outerRadius, 0f, Mathf.Tau, 48, new Color(0.95f, 0.97f, 1f, 0.48f), 3f, true);
        DrawArc(Vector2.Zero, _innerRadius, 0f, Mathf.Tau, 32, new Color(0.95f, 0.97f, 1f, 0.28f), 2f, true);
    }

    void LayoutBlades(Texture2D? texture)
    {
        if (_spin == null || texture == null)
        {
            return;
        }

        _blades.Clear();
        foreach (var child in _spin.GetChildren())
        {
            if (child is Sprite2D sprite)
            {
                _blades.Add(sprite);
            }
        }

        while (_blades.Count < 3)
        {
            var blade = new Sprite2D { Name = $"Blade{_blades.Count + 1}" };
            _spin.AddChild(blade);
            _blades.Add(blade);
        }

        var tex = texture.GetSize();
        var isUpright = tex.Y >= tex.X;
        var along = isUpright ? tex.Y : tex.X;
        var length = Math.Max(_outerRadius - _innerRadius, 8f);
        var scale = length / Math.Max(along, 1f);
        var centerRadius = (_innerRadius + _outerRadius) * 0.5f;

        for (var i = 0; i < _blades.Count; i++)
        {
            var angle = Mathf.Tau * i / _blades.Count;
            var blade = _blades[i];
            blade.Texture = texture;
            blade.Centered = true;
            blade.Modulate = Colors.White;
            blade.Position = Vector2.FromAngle(angle) * centerRadius;
            blade.Scale = new Vector2(scale, scale);
            if (isUpright)
            {
                blade.Offset = new Vector2(0f, -tex.Y * 0.5f);
                blade.Rotation = angle + Mathf.Pi * 0.5f;
            }
            else
            {
                blade.Offset = new Vector2(tex.X * 0.5f, 0f);
                blade.Rotation = angle;
            }
        }
    }
}
