namespace Rebirth.Combat;

/// <summary>武器只负责提供普攻入口与伤害，具体命中方式交给 AttackPatternExecutor。</summary>
public partial class Weapon : Node2D
{
    public WeaponData? Data { get; private set; }
    int _slashDirection = 1; // 左右交替挥砍

    public void Configure(WeaponData? data) => Data = data;

    /// <summary>尝试攻击。扇形挥砍只负责出刀，伤害等剑刃碰撞到敌人才结算。</summary>
    public bool TryAttack(Combatant user)
    {
        if (Data?.Pattern == null)
        {
            return false;
        }

        var attackRange = user.Stats.GetValue(StatType.AttackRange);
        if (attackRange <= 0f)
        {
            attackRange = Data.Pattern.Range;
        }

        var amount = user.Stats.GetValue(StatType.Attack) + Data.BonusDamage;
        var didAttack = AttackPatternExecutor.TryExecute(new AttackPatternRequest
        {
            Host = this,
            Source = user,
            Pattern = Data.Pattern,
            Damage = amount,
            DamageTags = Data.DamageTags,
            Candidates = GetTree().GetNodesInGroup("enemies"),
            RangeOverride = attackRange,
            Clockwise = _slashDirection > 0,
        });
        if (didAttack && Data.Pattern.Kind == AttackPatternKind.SweptSector)
        {
            _slashDirection *= -1; // 左右交替挥砍，避免每次都同一方向
        }

        return didAttack;
    }
}
