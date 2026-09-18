namespace Rebirth.Combat;

/// <summary>
/// 扇形挥砍表现：Sweep/Blade 负责剑刃贴图摆动，根节点不旋转以便画出与判定一致的扇面。
/// 替换素材：给 Blade 换 Texture，或在攻击模式资源上指定 AttackVfxTexture；有 Trail 贴图时不再画程序扇面。
/// </summary>
public partial class SlashVfx : AttackVfx
{
    Sprite2D? _blade;
    Sprite2D? _trail;
    Node2D? _sweep;
    float _innerRadius = 8f;
    float _outerRadius = 80f;
    float _startAngle;
    Color _trailColor = new(0.72f, 0.82f, 1f, 0.38f);
    bool _drawProceduralTrail = true;

    public override void Play(AttackVfxParams p)
    {
        _sweep = GetNodeOrNull<Node2D>("Sweep") ?? this;
        _blade = GetNodeOrNull<Sprite2D>("Sweep/Blade") ?? GetNodeOrNull<Sprite2D>("Blade");
        _trail = GetNodeOrNull<Sprite2D>("Trail");

        _innerRadius = Math.Max(p.InnerRadius, 4f);
        _outerRadius = Math.Max(p.Radius, _innerRadius + 8f);

        if (p.Texture != null && _blade != null)
        {
            _blade.Texture = p.Texture;
        }

        var dir = p.Direction.LengthSquared() < 0.0001f ? Vector2.Right : p.Direction;
        var mid = dir.Angle();
        var half = Mathf.DegToRad(Mathf.Max(p.ArcDegrees, 8f)) * 0.5f;
        _startAngle = p.Clockwise ? mid + half : mid - half;
        var endAngle = p.Clockwise ? mid - half : mid + half;

        LayoutBlade(_innerRadius, _outerRadius);
        LayoutTrailSprite(mid, p.ArcDegrees);

        _drawProceduralTrail = _trail?.Texture == null;
        _sweep.Rotation = _startAngle;
        GetNodeOrNull<SweepHitbox>("Sweep/Hitbox")?.Arm(p, _innerRadius, _outerRadius);

        var duration = Math.Max(p.Duration, 0.04f);
        var tween = CreateTween();
        tween.SetProcessMode(Tween.TweenProcessMode.Physics); // 旋转与物理判定同一拍，避免刀还没到就先结算
        tween.TweenProperty(_sweep, "rotation", endAngle, duration)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);
        if (_blade != null)
        {
            // 挥砍过程中保持贴图可见，收刀时再淡出
            tween.Parallel().TweenProperty(_blade, "modulate:a", 0f, duration * 0.35f)
                .SetDelay(duration * 0.65f)
                .SetEase(Tween.EaseType.In);
        }

        if (_trail != null && _trail.Texture != null)
        {
            _trail.Modulate = new Color(1f, 1f, 1f, 0.85f);
            tween.Parallel().TweenProperty(_trail, "modulate:a", 0f, duration)
                .SetEase(Tween.EaseType.In);
        }

        tween.TweenCallback(Callable.From(QueueFree));
    }

    public override void _Process(double _delta)
    {
        if (_drawProceduralTrail)
        {
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (!_drawProceduralTrail || _sweep == null)
        {
            return;
        }

        var current = _sweep.Rotation;
        if (Mathf.Abs(current - _startAngle) < 0.04f)
        {
            return;
        }

        DrawColoredPolygon(
            BuildRingSector(_innerRadius, _outerRadius, _startAngle, current, 16),
            _trailColor);
        DrawArc(
            Vector2.Zero,
            _outerRadius,
            _startAngle,
            current,
            16,
            new Color(0.95f, 0.97f, 1f, 0.45f),
            2.5f,
            true);
    }

    /// <summary>
    /// 贴图轴心放在柄上，长度拉到攻击半径，保持宽高比。
    /// 立绘（尖朝上）先转到 +X；横绘（尖朝右）直接用。
    /// </summary>
    void LayoutBlade(float inner, float outer)
    {
        if (_blade?.Texture == null)
        {
            return;
        }

        var tex = _blade.Texture.GetSize();
        var length = Math.Max(outer - inner, 8f);
        var isUpright = tex.Y >= tex.X;
        var along = isUpright ? tex.Y : tex.X;
        var scale = length / Math.Max(along, 1f);

        _blade.Centered = true;
        _blade.Position = new Vector2(inner, 0f);
        _blade.Scale = new Vector2(scale, scale);
        if (isUpright)
        {
            // 柄在图底部：把原点移到柄，再顺时针 90°，剑尖朝向 Sweep 的 +X
            _blade.Offset = new Vector2(0f, -tex.Y * 0.5f);
            _blade.Rotation = Mathf.Pi * 0.5f;
        }
        else
        {
            _blade.Offset = new Vector2(tex.X * 0.5f, 0f);
            _blade.Rotation = 0f;
        }
    }

    /// <summary>若提供扇形/刀光贴图，按瞄准朝向铺开，方便直接换成美术资源。</summary>
    void LayoutTrailSprite(float midAngle, float arcDegrees)
    {
        if (_trail?.Texture == null)
        {
            return;
        }

        var tex = _trail.Texture.GetSize();
        _trail.Centered = false;
        _trail.Offset = new Vector2(0f, -tex.Y * 0.5f);
        _trail.Rotation = midAngle - Mathf.DegToRad(Mathf.Max(arcDegrees, 8f)) * 0.5f;
        _trail.Scale = new Vector2(
            _outerRadius / Math.Max(tex.X, 1f),
            _outerRadius * Mathf.DegToRad(Mathf.Max(arcDegrees, 8f)) / Math.Max(tex.Y, 1f));
    }

    static Vector2[] BuildRingSector(float inner, float outer, float from, float to, int steps)
    {
        steps = Math.Max(steps, 2);
        var points = new Vector2[steps * 2];
        for (var i = 0; i < steps; i++)
        {
            var t = i / (float)(steps - 1);
            points[i] = Vector2.FromAngle(Mathf.Lerp(from, to, t)) * outer;
        }

        for (var i = 0; i < steps; i++)
        {
            var t = i / (float)(steps - 1);
            points[steps + i] = Vector2.FromAngle(Mathf.Lerp(to, from, t)) * inner;
        }

        return points;
    }
}
