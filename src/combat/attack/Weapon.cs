namespace Rebirth.Combat;

/// <summary>武器只负责发动攻击：点选走 TargetingSystem，范围走 AreaHitSystem，挥砍命中走碰撞盒。</summary>
public partial class Weapon : Node2D
{
    public WeaponData? Data { get; private set; }
    int _slashDirection = 1; // 左右交替挥砍

    public void Configure(WeaponData? data) => Data = data;

    /// <summary>尝试攻击。扇形挥砍只负责出刀，伤害等剑刃碰撞到敌人才结算。</summary>
    public bool TryAttack(Combatant user)
    {
        if (Data == null)
        {
            return false;
        }

        var attackRange = user.Stats.GetValue(StatType.AttackRange);
        if (attackRange <= 0f)
        {
            attackRange = Data.AttackRange;
        }

        var amount = user.Stats.GetValue(StatType.Attack) + Data.BonusDamage;
        var candidates = GetTree().GetNodesInGroup("enemies");

        if (Data.HitShape == AreaHitKind.Sector && Data.AttackVfx != null)  // 扇形挥砍走这里
        {
            var aim = TargetingSystem.Select(
                user.GlobalPosition,
                user.Radius,
                candidates,
                attackRange,
                Data.Targeting,
                maxTargets: 1); // 最多命中一个目标
            if (aim.Count == 0)
            {
                return false; // 没有目标则直接返回 false
            }

            PlayAttackVisual(user, aim[0].GlobalPosition, attackRange, [], amount);
            return true;
        }

        var targets = ResolveHits(user, candidates, attackRange, out var aimPoint); // 解析命中目标
        if (targets.Count == 0)
        {
            return false; // 没有目标则直接返回 false
        }

        AreaHitSystem.Apply( // 应用伤害
            new DamageRequest
            {
                Source = user,
                Amount = amount,
                Tags = Data.DamageTags,
            },
            targets);

        PlayAttackVisual(user, aimPoint, attackRange, targets, amount); // 播放攻击视觉
        return true;
    }

    /// <summary>圆形用范围查询；点选用 TargetingSystem。扇形出刀不走这里。</summary>
    List<Combatant> ResolveHits(
        Combatant user,
        IEnumerable<Node> candidates,
        float attackRange,
        out Vector2 aimPoint)
    {
        aimPoint = user.GlobalPosition + Vector2.Right;
        if (Data == null)
        {
            return [];
        }

        if (Data.HitShape == AreaHitKind.Circle)
        {
            var hits = AreaHitSystem.Query(
                AreaShape.Circle(user.GlobalPosition, user.Radius, attackRange),
                candidates,
                user);
            if (hits.Count > 0)
            {
                hits.Sort((a, b) =>
                    user.GlobalPosition.DistanceSquaredTo(a.GlobalPosition)
                        .CompareTo(user.GlobalPosition.DistanceSquaredTo(b.GlobalPosition)));
                aimPoint = hits[0].GlobalPosition;
            }

            return hits;
        }

        var selected = TargetingSystem.Select(
            user.GlobalPosition,
            user.Radius,
            candidates,
            attackRange,
            Data.Targeting,
            Math.Max(1, Data.MaxTargets));
        if (selected.Count > 0)
        {
            aimPoint = selected[0].GlobalPosition;
        }

        return selected;
    }

    /// <summary>播放攻击视觉。</summary>
    void PlayAttackVisual(Combatant user, Vector2 aimPoint, float attackRange, List<Combatant> targets, float damage)
    {
        if (Data == null) // 如果武器数据不存在则直接返回
        {
            return; // 没有武器数据则直接返回
        }

        if (Data.AttackVfx != null) // 如果武器数据有攻击特效则播放攻击特效
        {
            AttackVfxPlayer.Play(Data.AttackVfx, new AttackVfxParams // 播放攻击特效
            {
                Host = this, // 主机
                Origin = user.GlobalPosition,
                Direction = aimPoint - user.GlobalPosition, // 方向
                InnerRadius = user.Radius * 0.2f,
                Radius = attackRange + user.Radius * 0.35f, // 半径
                ArcDegrees = Data.SlashArcDegrees,
                Duration = Data.SlashDuration, // 持续时间
                Clockwise = _slashDirection > 0,
                Texture = Data.AttackVfxTexture, // 贴图
                Source = user,
                Damage = Data.HitShape == AreaHitKind.Sector ? damage : 0f, // 伤害
                DamageTags = Data.DamageTags, // 伤害标签
            });
            _slashDirection *= -1; // 左右交替挥砍，避免每次都同一方向          
            return;
        }

        foreach (var target in targets)
        {
            AttackFlash.Play(this, user.GlobalPosition, target.GlobalPosition);
        }
    }
}
