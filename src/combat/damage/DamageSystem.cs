namespace Rebirth.Combat;

/// <summary>所有扣血的唯一入口。暴击/防御/闪避/吸血/伤害加成只在这里算。</summary>
public static class DamageSystem
{
    const float MinHitDamage = 1f;

    /// <summary>结算并应用伤害，返回最终伤害值（闪避或无效目标返回 0）。</summary>
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

        var resolved = Resolve(request);
        var result = new DamageResult
        {
            Request = request,
            Amount = resolved.Amount,
            IsCrit = resolved.IsCrit,
            IsDodged = resolved.IsDodged,
        };

        if (resolved.IsDodged)
        {
            EventBus.Instance.EmitDamageApplied(result);
            return 0f;
        }

        if (resolved.Amount <= 0f)
        {
            return 0f;
        }

        request.Target.Health.ApplyDamage(resolved.Amount, request.Source);
        TryStealLife(FindCombatant(request.Source), resolved.Amount);
        EventBus.Instance.EmitDamageApplied(result);
        return resolved.Amount;
    }

    static Resolved Resolve(DamageRequest request)
    {
        var amount = Math.Max(request.Amount, 0f);
        if (amount <= 0f)
        {
            return default;
        }

        var attacker = FindCombatant(request.Source);
        var defender = request.Target;
        if (defender != null && GameRng.Instance.Chance(CombatStatScale.Rate(defender, StatType.Dodge)))
        {
            return new Resolved { IsDodged = true };
        }

        if (attacker != null)
        {
            amount *= CombatStatScale.Multiplier(attacker, StatType.DamageBonus);
            var critRate = CombatStatScale.Rate(attacker, StatType.CritRate);
            if (GameRng.Instance.Chance(critRate))
            {
                var critDamage = Math.Max(attacker.Stats.GetValue(StatType.CritDamage), 1f);
                amount *= critDamage;
                var afterDefense = ApplyDefense(amount, defender);
                return new Resolved { Amount = afterDefense, IsCrit = true };
            }
        }

        return new Resolved { Amount = ApplyDefense(amount, defender) };
    }

    static float ApplyDefense(float amount, Combatant? defender)
    {
        if (defender == null)
        {
            return amount;
        }

        var defense = Math.Max(defender.Stats.GetValue(StatType.Defense), 0f);
        return Math.Max(amount - defense, MinHitDamage);
    }

    static void TryStealLife(Combatant? attacker, float dealt)
    {
        if (attacker?.Health == null || attacker.Health.IsDead || dealt <= 0f)
        {
            return;
        }

        var steal = CombatStatScale.Rate(attacker, StatType.Lifesteal);
        if (steal <= 0f)
        {
            return;
        }

        attacker.Health.Heal(dealt * steal);
    }

    static Combatant? FindCombatant(Node? node)
    {
        while (node != null)
        {
            if (node is Combatant combatant)
            {
                return combatant;
            }

            node = node.GetParent();
        }

        return null;
    }

    readonly struct Resolved
    {
        public float Amount { get; init; }
        public bool IsCrit { get; init; }
        public bool IsDodged { get; init; }
    }
}
