namespace Rebirth.Combat;

/// <summary>简单追逐移动：朝目标方向以固定速度移动。</summary>
public sealed class ChaseMovement
{
    /// <summary>驱动 CharacterBody2D 朝 destination 移动。</summary>
    public void Tick(CharacterBody2D body, Vector2 destination, float speed)
    {
        var direction = body.GlobalPosition.DirectionTo(destination); // 指向目标的单位向量
        body.Velocity = direction.LengthSquared() < 0.0001f ? Vector2.Zero : direction * speed;
        body.MoveAndSlide();
    }
}
