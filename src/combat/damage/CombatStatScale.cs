namespace Rebirth.Combat;

/// <summary>把 Area / Duration / ProjectileSpeed 加成换成倍率。0 基础 + Flat 表示 +10%。</summary>
public static class CombatStatScale
{
    public static float Multiplier(Combatant user, StatType stat) =>
        1f + Math.Max(user.Stats.GetValue(stat), 0f);

    public static float Range(Combatant user, float value) =>
        Math.Max(value, 0f) * Multiplier(user, StatType.Area);

    public static float Duration(Combatant user, float value) =>
        Math.Max(value, 0f) * Multiplier(user, StatType.Duration);

    public static float ProjectileSpeed(Combatant user, float value) =>
        Math.Max(value, 0f) * Multiplier(user, StatType.ProjectileSpeed);

    public static float Rate(Combatant user, StatType stat) =>
        Mathf.Clamp(user.Stats.GetValue(stat), 0f, 1f);
}
