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

        return request.Pattern.Kind switch
        {
            AttackPatternKind.InstantCircleAroundSelf => ExecuteInstantCircle(request),
            AttackPatternKind.PointTarget => ExecutePointTarget(request),
            AttackPatternKind.SweptSector => ExecuteSweptSector(request),
            AttackPatternKind.Projectile => ExecuteProjectile(request),
            AttackPatternKind.SweptCircleAroundSelf => ExecuteSweptCircle(request),
            AttackPatternKind.InstantSector => ExecuteInstantSector(request),
            _ => false,
        };
    }

    static float RangeOf(AttackPatternRequest request) =>
        Math.Max(request.RangeOverride ?? request.Pattern.Range, 0f);

    static IEnumerable<Node> CandidatesOf(AttackPatternRequest request) =>
        request.Candidates ?? request.Host.GetTree().GetNodesInGroup("enemies");

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
            Duration = request.Pattern.Duration,
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
                Duration = request.Pattern.Duration,
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
            Duration = request.Pattern.Duration,
            Clockwise = request.Clockwise,
            Texture = request.Pattern.AttackVfxTexture,
            Source = request.Source,
            Damage = 0f,
            DamageTags = request.DamageTags,
        });
    }

    static bool ExecuteProjectile(AttackPatternRequest request)
    {
        var targets = SelectTargets(request, RangeOf(request), maxTargets: 1);
        if (targets.Count == 0)
        {
            return false;
        }

        var parent = request.Host.GetTree().CurrentScene ?? request.Host;
        ProjectileAttack.Spawn(
            parent,
            request.Source,
            request.Source.GlobalPosition,
            targets[0].GlobalPosition,
            request.Pattern,
            RangeOf(request),
            request.Damage,
            request.DamageTags);
        return true;
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
