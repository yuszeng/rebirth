namespace Rebirth.Combat;

/// <summary>所有扣血的唯一入口。暴击/防御/护盾在 Resolve 里加，不要在 Player/Enemy 里算。</summary>
public static class DamageSystem
{
    /// <summary>结算并应用伤害，返回最终伤害值（无效目标返回 0）。</summary>
    public static float Apply(DamageRequest request)
    {
        if (request.Target == null || !GodotObject.IsInstanceValid(request.Target))
        {
            return 0f;
        }

        if (request.Target.Health == null || request.Target.Health.IsDead)
        {
            return 0f;
        }

        var finalAmount = Resolve(request); // 经防御/暴击等修正后的数值
        if (finalAmount <= 0f)
        {
            return 0f;
        }

        request.Target.Health.ApplyDamage(finalAmount, request.Source);
        EventBus.Instance.EmitDamageApplied(request, finalAmount);
        return finalAmount;
    }

    /// <summary>伤害修正管道。Phase 1 仅做下限截断，后续在此扩展。</summary>
    static float Resolve(DamageRequest request)
    {
        return Math.Max(request.Amount, 0f);
    }
}
