namespace Rebirth.Combat;

/// <summary>生命值组件：扣血、治疗、死亡通知。</summary>
public partial class Health : Node
{
    public event Action<Node?>? Died; // 血量归零时触发（携带伤害来源）
    public event Action<float, float>? Changed; // 血量变化（当前值, 最大值）

    public float Current { get; private set; } // 当前血量
    public float Maximum { get; set; } // 最大血量
    public bool IsDead { get; private set; } // 是否已死亡

    /// <summary>初始化满血状态。</summary>
    public void Setup(float maxHp)
    {
        Maximum = maxHp;
        Current = maxHp;
        IsDead = false;
        Changed?.Invoke(Current, Maximum);
    }

    /// <summary>受到伤害，归零时标记死亡并触发 Died。</summary>
    public void ApplyDamage(float amount, Node? source)
    {
        if (IsDead)
        {
            return;
        }

        Current = Math.Max(Current - amount, 0f);
        Changed?.Invoke(Current, Maximum);
        if (Current <= 0f)
        {
            IsDead = true;
            Died?.Invoke(source);
        }
    }

    public void Heal(float amount)
    {
        if (IsDead)
        {
            return;
        }

        Current = Math.Min(Current + amount, Maximum);
        Changed?.Invoke(Current, Maximum);
    }

    /// <summary>回满血，用于回合间重置。</summary>
    public void HealFull()
    {
        if (IsDead)
        {
            return;
        }

        Current = Maximum;
        Changed?.Invoke(Current, Maximum);
    }

    /// <summary>调整上限；preserveRatio 为 true 时按原比例缩放当前血量。</summary>
    public void RetargetMaximum(float newMaximum, bool preserveRatio)
    {
        var ratio = Maximum <= 0f ? 1f : Current / Maximum; // 当前血量占比
        Maximum = newMaximum;
        Current = preserveRatio ? Math.Min(Maximum, Maximum * ratio) : Math.Min(Current, Maximum);
        Changed?.Invoke(Current, Maximum);
    }
}
