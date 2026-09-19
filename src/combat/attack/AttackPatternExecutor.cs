namespace Rebirth.Combat;

/// <summary>攻击模式执行器。武器、技能、未来魔法都从这里复用命中与表现逻辑。</summary>
public static class AttackPatternExecutor
{
    public static bool TryExecute(AttackPatternRequest request)
    {
        if (request.Source.Health == null || request.Source.Health.IsDead)
        {
            return false;
        }

        var descriptor = AttackPatternDescriptor.Resolve(request.Pattern);
        if (request.PatternModifier != null)
        {
            descriptor = request.PatternModifier(descriptor);
        }

        return Execute(request, descriptor);
    }

    static bool Execute(AttackPatternRequest request, AttackPatternDescriptor descriptor) =>
        descriptor.Delivery switch
        {
            AttackDeliveryKind.Instant => ExecuteInstant(request, descriptor),
            AttackDeliveryKind.Projectile => ExecuteProjectile(request, descriptor),
            AttackDeliveryKind.AttachedToSource => ExecuteAttachedToSource(request, descriptor),
            AttackDeliveryKind.DelayedAtTarget => ExecuteDelayedExplosion(request),
            AttackDeliveryKind.PersistentOrbit => ExecuteOrbitingSatellites(request),
            AttackDeliveryKind.Chain => ExecuteChainJump(request),
            _ => false,
        };

    static float RangeOf(AttackPatternRequest request) =>
        CombatStatScale.Range(request.Source, Math.Max(request.RangeOverride ?? request.Pattern.Range, 0f));

    static float DurationOf(AttackPatternRequest request) =>
        CombatStatScale.Duration(request.Source, request.Pattern.Duration);

    static IEnumerable<Node> CandidatesOf(AttackPatternRequest request) =>
        request.Candidates ?? request.Host.GetTree().GetNodesInGroup("enemies");

    static bool ExecuteInstant(AttackPatternRequest request, AttackPatternDescriptor descriptor) =>
        descriptor.HitShape switch
        {
            AttackHitShapeKind.PointTargets => ExecutePointTarget(request),
            AttackHitShapeKind.Circle => ExecuteInstantCircle(request),
            AttackHitShapeKind.Sector => ExecuteInstantSector(request),
            _ => false,
        };

    static bool ExecuteAttachedToSource(AttackPatternRequest request, AttackPatternDescriptor descriptor) =>
        (descriptor.HitShape, descriptor.HitPolicy) switch
        {
            (AttackHitShapeKind.Circle, AttackHitPolicyKind.PerTargetOnce) => ExecuteSweptCircle(request),
            (AttackHitShapeKind.Sector, AttackHitPolicyKind.PerTargetOnce) => ExecuteSweptSector(request),
            (AttackHitShapeKind.Capsule, AttackHitPolicyKind.Pierce) => ExecutePiercingCollision(request, descriptor),
            (AttackHitShapeKind.Capsule, AttackHitPolicyKind.Tick) => ExecuteSustainedBeam(request),
            _ => false,
        };

    static bool ExecuteInstantCircle(AttackPatternRequest request)
    {
        var range = RangeOf(request);
        var hits = AreaHitSystem.Query(
            AreaShape.Circle(request.Source.GlobalPosition, request.Source.Radius, range),
            CandidatesOf(request),
            request.Source);
        if (hits.Count == 0)
        {
            return false;
        }

        AreaHitSystem.Apply(ToDamageRequest(request), hits);
        AttackPatternFlash.PlayRing(request.Host, range + request.Source.Radius * 0.35f);
        return true;
    }

    static bool ExecuteSweptCircle(AttackPatternRequest request)
    {
        var range = RangeOf(request);
        if (request.Pattern.AttackVfx == null)
        {
            return ExecuteInstantCircle(request);
        }

        AttackVfxPlayer.Play(request.Pattern.AttackVfx, new AttackVfxParams
        {
            Host = request.Host,
            Origin = request.Source.GlobalPosition,
            Direction = Vector2.Right,
            InnerRadius = request.Source.Radius * 0.25f,
            Radius = range + request.Source.Radius,
            ArcDegrees = 360f,
            Duration = DurationOf(request),
            Clockwise = request.Clockwise,
            Texture = request.Pattern.AttackVfxTexture,
            Source = request.Source,
            Damage = request.Damage,
            DamageTags = request.DamageTags,
        });
        return true;
    }

    static bool ExecutePointTarget(AttackPatternRequest request)
    {
        var targets = SelectTargets(request, RangeOf(request));
        if (targets.Count == 0)
        {
            return false;
        }

        AreaHitSystem.Apply(ToDamageRequest(request), targets);
        foreach (var target in targets)
        {
            AttackFlash.Play(request.Host, request.Source.GlobalPosition, target.GlobalPosition);
        }

        return true;
    }

    static bool ExecuteSweptSector(AttackPatternRequest request)
    {
        var range = RangeOf(request);
        var aim = SelectTargets(request, range, maxTargets: 1);
        if (aim.Count == 0)
        {
            return false;
        }

        var direction = aim[0].GlobalPosition - request.Source.GlobalPosition;
        if (request.Pattern.AttackVfx != null)
        {
            AttackVfxPlayer.Play(request.Pattern.AttackVfx, new AttackVfxParams
            {
                Host = request.Host,
                Origin = request.Source.GlobalPosition,
                Direction = direction,
                InnerRadius = request.Source.Radius * 0.2f,
                Radius = range + request.Source.Radius * 0.35f,
                ArcDegrees = request.Pattern.ArcDegrees,
                Duration = DurationOf(request),
                Clockwise = request.Clockwise,
                Texture = request.Pattern.AttackVfxTexture,
                Source = request.Source,
                Damage = request.Damage,
                DamageTags = request.DamageTags,
            });
            return true;
        }

        return ExecuteInstantSector(request, direction);
    }

    static bool ExecuteInstantSector(AttackPatternRequest request)
    {
        var range = RangeOf(request);
        var aim = SelectTargets(request, range, maxTargets: 1);
        if (aim.Count == 0)
        {
            return false;
        }

        return ExecuteInstantSector(request, aim[0].GlobalPosition - request.Source.GlobalPosition);
    }

    static bool ExecuteInstantSector(AttackPatternRequest request, Vector2 direction)
    {
        var range = RangeOf(request);
        var hits = AreaHitSystem.Query(
            AreaShape.Sector(
                request.Source.GlobalPosition,
                request.Source.Radius,
                range,
                direction,
                request.Pattern.ArcDegrees),
            CandidatesOf(request),
            request.Source);
        if (hits.Count == 0)
        {
            return false;
        }

        AreaHitSystem.Apply(ToDamageRequest(request), hits);
        if (request.Pattern.AttackVfx != null)
        {
            PlayVisualOnlyVfx(request, direction, range);
        }
        else
        {
            foreach (var target in hits)
            {
                AttackFlash.Play(request.Host, request.Source.GlobalPosition, target.GlobalPosition);
            }
        }

        return true;
    }

    static void PlayVisualOnlyVfx(AttackPatternRequest request, Vector2 direction, float range)
    {
        AttackVfxPlayer.Play(request.Pattern.AttackVfx!, new AttackVfxParams
        {
            Host = request.Host,
            Origin = request.Source.GlobalPosition,
            Direction = direction,
            InnerRadius = request.Source.Radius * 0.2f,
            Radius = range + request.Source.Radius * 0.35f,
            ArcDegrees = request.Pattern.ArcDegrees,
            Duration = DurationOf(request),
            Clockwise = request.Clockwise,
            Texture = request.Pattern.AttackVfxTexture,
            Source = request.Source,
            Damage = 0f,
            DamageTags = request.DamageTags,
        });
    }

    static bool ExecuteProjectile(AttackPatternRequest request, AttackPatternDescriptor descriptor)
    {
        var targets = SelectTargets(request, RangeOf(request), maxTargets: 1);
        if (targets.Count == 0)
        {
            return false;
        }

        var direction = targets[0].GlobalPosition - request.Source.GlobalPosition;
        var centerAngle = direction.Angle();
        var count = Math.Max(descriptor.ProjectileCount, 1);
        var arc = Mathf.DegToRad(Math.Max(descriptor.ProjectileArcDegrees, 0f));
        var maxHits = ProjectileMaxHits(request, descriptor);
        var parent = request.Host.GetTree().CurrentScene ?? request.Host;
        for (var i = 0; i < count; i++)
        {
            var offset = count == 1 ? 0f : Mathf.Lerp(-arc * 0.5f, arc * 0.5f, i / (float)(count - 1));
            ProjectileAttack.SpawnDirected(
                parent,
                request.Source,
                request.Source.GlobalPosition,
                Vector2.FromAngle(centerAngle + offset),
                request.Pattern,
                RangeOf(request),
                request.Damage,
                request.DamageTags,
                maxHits);
        }

        return true;
    }

    static int ProjectileMaxHits(AttackPatternRequest request, AttackPatternDescriptor descriptor)
    {
        if (descriptor.HitPolicy != AttackHitPolicyKind.Pierce)
        {
            return 1;
        }

        return request.Pattern.PierceCount <= 0
            ? int.MaxValue
            : Math.Max(request.Pattern.PierceCount, Math.Max(descriptor.MaxHits, 1));
    }

    static bool ExecutePiercingCollision(AttackPatternRequest request, AttackPatternDescriptor descriptor)
    {
        var targets = SelectTargets(request, RangeOf(request), maxTargets: 1);
        if (targets.Count == 0)
        {
            return false;
        }

        var parent = request.Host.GetTree().CurrentScene ?? request.Host;
        PiercingThrustAttack.Spawn(
            parent,
            request.Source,
            targets[0].GlobalPosition,
            request.Pattern,
            RangeOf(request),
            request.Damage,
            request.DamageTags,
            MaxHitsForPierce(request, descriptor));
        return true;
    }

    static int MaxHitsForPierce(AttackPatternRequest request, AttackPatternDescriptor descriptor)
    {
        if (request.Pattern.PierceCount <= 0 || descriptor.MaxHits <= 0)
        {
            return int.MaxValue;
        }

        return Math.Max(request.Pattern.PierceCount, descriptor.MaxHits);
    }

    static bool ExecuteChainJump(AttackPatternRequest request)
    {
        var candidates = CandidatesOf(request).ToArray();
        var currentWave = TargetingSystem.Select(
            request.Source.GlobalPosition,
            request.Source.Radius,
            candidates,
            RangeOf(request),
            request.Pattern.Targeting,
            Math.Max(request.Pattern.MaxTargets, 1));
        if (currentWave.Count == 0)
        {
            return false;
        }

        var hitIds = new HashSet<ulong>();
        foreach (var target in currentWave)
        {
            hitIds.Add(target.GetInstanceId());
            ApplyDirectDamage(request, target, request.Damage);
            AttackFlash.Play(request.Host, request.Source.GlobalPosition, target.GlobalPosition);
        }

        var jumpRange = CombatStatScale.Range(request.Source, Math.Max(request.Pattern.ChainJumpRange, 0f));
        var targetsPerJump = Math.Max(request.Pattern.ChainTargetsPerJump, 1);
        var multiplier = Math.Max(request.Pattern.ChainDamageMultiplier, 0f);
        for (var jump = 0; jump < Math.Max(request.Pattern.ChainJumps, 0); jump++)
        {
            var nextWave = new List<Combatant>();
            foreach (var previous in currentWave)
            {
                if (!GodotObject.IsInstanceValid(previous))
                {
                    continue;
                }

                var targets = TargetingSystem.Select(
                    previous.GlobalPosition,
                    previous.Radius,
                    Excluding(candidates, hitIds),
                    jumpRange,
                    request.Pattern.Targeting,
                    targetsPerJump);
                foreach (var target in targets)
                {
                    hitIds.Add(target.GetInstanceId());
                    var jumpDamage = request.Damage * Mathf.Pow(multiplier, jump + 1);
                    ApplyDirectDamage(request, target, jumpDamage);
                    AttackFlash.Play(request.Host, previous.GlobalPosition, target.GlobalPosition);
                    nextWave.Add(target);
                }
            }

            if (nextWave.Count == 0)
            {
                break;
            }

            currentWave = nextWave;
        }

        return true;
    }

    static bool ExecuteSustainedBeam(AttackPatternRequest request)
    {
        var targets = SelectTargets(request, RangeOf(request), maxTargets: 1);
        if (targets.Count == 0)
        {
            return false;
        }

        SustainedBeamAttack.Spawn(
            request.Host.GetTree().CurrentScene ?? request.Host,
            request.Source,
            targets[0],
            request.Pattern,
            RangeOf(request),
            request.Damage,
            request.DamageTags);
        return true;
    }

    static bool ExecuteDelayedExplosion(AttackPatternRequest request)
    {
        var targets = SelectTargets(request, RangeOf(request), maxTargets: 1);
        if (targets.Count == 0)
        {
            return false;
        }

        DelayedExplosionAttack.Spawn(
            request.Host.GetTree().CurrentScene ?? request.Host,
            request.Source,
            targets[0].GlobalPosition,
            request.Pattern,
            request.Damage,
            request.DamageTags);
        return true;
    }

    static bool ExecuteOrbitingSatellites(AttackPatternRequest request)
    {
        OrbitingSatelliteAttack.SpawnOrRefresh(
            request.Host.GetTree().CurrentScene ?? request.Host,
            request.Source,
            request.Pattern,
            request.Damage,
            request.DamageTags);
        return true;
    }

    static IEnumerable<Node> Excluding(IEnumerable<Node> candidates, HashSet<ulong> excluded)
    {
        foreach (var node in candidates)
        {
            if (node is not Combatant target || !GodotObject.IsInstanceValid(target))
            {
                continue;
            }

            if (!excluded.Contains(target.GetInstanceId()))
            {
                yield return target;
            }
        }
    }

    static void ApplyDirectDamage(AttackPatternRequest request, Combatant target, float amount)
    {
        DamageSystem.Apply(new DamageRequest
        {
            Source = request.Source,
            Target = target,
            Amount = amount,
            Tags = request.DamageTags,
        });
    }

    static List<Combatant> SelectTargets(
        AttackPatternRequest request,
        float range,
        int? maxTargets = null) =>
        TargetingSystem.Select(
            request.Source.GlobalPosition,
            request.Source.Radius,
            CandidatesOf(request),
            range,
            request.Pattern.Targeting,
            maxTargets ?? Math.Max(1, request.Pattern.MaxTargets));

    static DamageRequest ToDamageRequest(AttackPatternRequest request) => new()
    {
        Source = request.Source,
        Amount = request.Damage,
        Tags = request.DamageTags,
    };
}
