namespace Rebirth.Combat;

/// <summary>AttackPatternData 的运行时展开结果，用于把“飞行/形状/命中策略”拆开组合。</summary>
public readonly record struct AttackPatternDescriptor
{
    public AttackDeliveryKind Delivery { get; init; }
    public AttackHitShapeKind HitShape { get; init; }
    public AttackHitPolicyKind HitPolicy { get; init; }
    public bool RequiresTarget { get; init; }
    public int ProjectileCount { get; init; }
    public float ProjectileArcDegrees { get; init; }
    public int MaxHits { get; init; }

    public AttackPatternDescriptor WithPierce(int maxHits) => this with
    {
        HitPolicy = AttackHitPolicyKind.Pierce,
        MaxHits = maxHits <= 0 ? int.MaxValue : maxHits,
    };

    public static AttackPatternDescriptor Resolve(AttackPatternData pattern)
    {
        var preset = FromKind(pattern);
        return preset with
        {
            Delivery = pattern.Delivery == AttackDeliveryKind.Preset ? preset.Delivery : pattern.Delivery,
            HitShape = pattern.HitShape == AttackHitShapeKind.Preset ? preset.HitShape : pattern.HitShape,
            HitPolicy = pattern.HitPolicy == AttackHitPolicyKind.Preset ? preset.HitPolicy : pattern.HitPolicy,
        };
    }

    static AttackPatternDescriptor FromKind(AttackPatternData pattern)
    {
        var pierceCount = pattern.PierceCount <= 0 ? int.MaxValue : pattern.PierceCount;
        return pattern.Kind switch
        {
            AttackPatternKind.InstantCircleAroundSelf => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.Instant,
                HitShape = AttackHitShapeKind.Circle,
                HitPolicy = AttackHitPolicyKind.MultiHit,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.PointTarget => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.Instant,
                HitShape = AttackHitShapeKind.PointTargets,
                HitPolicy = AttackHitPolicyKind.SingleHit,
                RequiresTarget = true,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.SweptSector => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.AttachedToSource,
                HitShape = AttackHitShapeKind.Sector,
                HitPolicy = AttackHitPolicyKind.PerTargetOnce,
                RequiresTarget = true,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.Projectile => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.Projectile,
                HitShape = AttackHitShapeKind.Circle,
                HitPolicy = AttackHitPolicyKind.SingleHit,
                RequiresTarget = true,
                ProjectileCount = 1,
                MaxHits = 1,
            },
            AttackPatternKind.SweptCircleAroundSelf => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.AttachedToSource,
                HitShape = AttackHitShapeKind.Circle,
                HitPolicy = AttackHitPolicyKind.PerTargetOnce,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.InstantSector => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.Instant,
                HitShape = AttackHitShapeKind.Sector,
                HitPolicy = AttackHitPolicyKind.MultiHit,
                RequiresTarget = true,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.PiercingProjectile => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.AttachedToSource,
                HitShape = AttackHitShapeKind.Capsule,
                HitPolicy = AttackHitPolicyKind.Pierce,
                RequiresTarget = true,
                MaxHits = pierceCount,
            },
            AttackPatternKind.ScatterProjectile => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.Projectile,
                HitShape = AttackHitShapeKind.Circle,
                HitPolicy = AttackHitPolicyKind.SingleHit,
                RequiresTarget = true,
                ProjectileCount = Math.Max(pattern.ScatterCount, 1),
                ProjectileArcDegrees = Math.Max(pattern.ScatterArcDegrees, 0f),
                MaxHits = 1,
            },
            AttackPatternKind.ChainJump => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.Chain,
                HitShape = AttackHitShapeKind.PointTargets,
                HitPolicy = AttackHitPolicyKind.ChainJump,
                RequiresTarget = true,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.SustainedBeam => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.AttachedToSource,
                HitShape = AttackHitShapeKind.Capsule,
                HitPolicy = AttackHitPolicyKind.Tick,
                RequiresTarget = true,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.DelayedExplosion => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.DelayedAtTarget,
                HitShape = AttackHitShapeKind.Circle,
                HitPolicy = AttackHitPolicyKind.SingleHit,
                RequiresTarget = true,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            AttackPatternKind.OrbitingSatellites => new AttackPatternDescriptor
            {
                Delivery = AttackDeliveryKind.PersistentOrbit,
                HitShape = AttackHitShapeKind.SatelliteCircles,
                HitPolicy = AttackHitPolicyKind.Tick,
                MaxHits = Math.Max(pattern.MaxTargets, 1),
            },
            _ => default,
        };
    }
}
