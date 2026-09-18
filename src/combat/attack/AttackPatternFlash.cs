namespace Rebirth.Combat;

/// <summary>通用攻击模式的轻量占位表现。不参与伤害计算。</summary>
public static class AttackPatternFlash
{
    public static void PlayRing(Node2D host, float radius, Color? color = null, float duration = 0.16f)
    {
        const int segments = 24;
        var points = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = Mathf.Tau * i / segments;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        var line = new Line2D
        {
            Width = 5f,
            DefaultColor = color ?? new Color(0.55f, 0.85f, 1f, 0.85f),
            Closed = true,
            Points = points,
        };
        host.AddChild(line);

        var tween = host.CreateTween();
        tween.TweenInterval(duration);
        tween.TweenCallback(Callable.From(line.QueueFree));
    }
}
