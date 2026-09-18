namespace Rebirth.Combat;

/// <summary>一次攻击表现的播放参数。逻辑层只填这些，具体动画由场景自己解释。</summary>
public readonly struct AttackVfxParams
{
    public Node2D Host { get; init; }
    public Vector2 Origin { get; init; }
    public Vector2 Direction { get; init; }
    public float InnerRadius { get; init; }
    public float Radius { get; init; }
    public float ArcDegrees { get; init; }
    public float Duration { get; init; }
    public bool Clockwise { get; init; }
    public Texture2D? Texture { get; init; }
    public Combatant? Source { get; init; }
    public float Damage { get; init; }
    public string[] DamageTags { get; init; }
}

/// <summary>
/// 攻击特效场景的约定入口。换素材时优先换 PackedScene / 贴图，而不是改 Weapon。
/// 无脚本的场景也能播：朝向瞄准方向后按 Duration 自动销毁。
/// </summary>
public partial class AttackVfx : Node2D
{
    public virtual void Play(AttackVfxParams p)
    {
        Rotation = p.Direction.Angle();
        var tween = CreateTween();
        tween.TweenInterval(Math.Max(p.Duration, 0.04f));
        tween.TweenCallback(Callable.From(QueueFree));
    }
}

/// <summary>实例化武器上配置的特效场景并播放。</summary>
public static class AttackVfxPlayer
{
    public static void Play(PackedScene scene, AttackVfxParams p)
    {
        var node = scene.Instantiate<Node2D>();
        p.Host.AddChild(node);
        node.GlobalPosition = p.Origin;
        if (node is AttackVfx vfx)
        {
            vfx.Play(p);
            return;
        }

        node.Rotation = p.Direction.Angle();
        var tween = node.CreateTween();
        tween.TweenInterval(Math.Max(p.Duration, 0.04f));
        tween.TweenCallback(Callable.From(node.QueueFree));
    }
}
