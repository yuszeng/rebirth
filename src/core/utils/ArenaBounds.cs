namespace Rebirth.Core;

/// <summary>
/// 竞技场内沿几何。绘制、静态墙和刷怪必须共用，否则怪会刷在墙外。
/// </summary>
public static class ArenaBounds
{
    public const float HalfExtent = 1200f; // 可走区域半边长（中心到内沿）
    public const float WallThickness = 32f; // 物理墙厚度，只挡人，不负责外观
    public const uint WallLayer = 4; // 碰撞层 bitmask：第 3 层

    /// <summary>把点夹进场内，预留 radius 以免实体卡进墙。</summary>
    public static Vector2 ClampInside(Vector2 position, float radius)
    {
        var limit = HalfExtent - radius;
        return new Vector2(
            Mathf.Clamp(position.X, -limit, limit),
            Mathf.Clamp(position.Y, -limit, limit));
    }

    public static bool Contains(Vector2 position, float radius)
    {
        var limit = HalfExtent - radius;
        return Mathf.Abs(position.X) <= limit && Mathf.Abs(position.Y) <= limit;
    }

    /// <summary>在玩家周围采样；贴边时多数方向会出界，重试后再夹紧。</summary>
    public static Vector2 PickSpawnAround(Vector2 origin, float minDistance, float maxDistance, float radius)
    {
        var candidate = origin;
        for (var i = 0; i < 16; i++)
        {
            var angle = GameRng.Instance.Range(0f, Mathf.Tau);
            var distance = GameRng.Instance.Range(minDistance, maxDistance);
            candidate = origin + Vector2.FromAngle(angle) * distance;
            if (Contains(candidate, radius))
            {
                return candidate;
            }
        }

        return ClampInside(candidate, radius);
    }
}
