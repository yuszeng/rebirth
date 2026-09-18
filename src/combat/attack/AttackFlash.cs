namespace Rebirth.Combat;

/// <summary>短暂显示从攻击者到目标的连线，玩家武器与敌人接触攻击共用。</summary>
public static class AttackFlash
{
    /// <summary>在 host 上绘制一条短寿命连线。</summary>
    public static void Play(
        Node2D host,
        Vector2 fromGlobal,
        Vector2 toGlobal,
        Color? color = null,
        float width = 6f,
        float duration = 0.08f)
    {
        var line = new Line2D
        {
            Width = width,
            DefaultColor = color ?? new Color(0.85f, 0.9f, 1f, 0.9f),
            Points = [Vector2.Zero, toGlobal - fromGlobal],
        };
        host.AddChild(line);

        var tween = host.CreateTween();
        tween.TweenInterval(duration);
        tween.TweenCallback(Callable.From(line.QueueFree));
    }
}
