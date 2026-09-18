namespace Rebirth.Combat;

/// <summary>接触伤害同样走 DamageSystem。</summary>
public sealed class ContactAttack
{
    float _cooldown; // 攻击冷却剩余时间（秒）

    /// <summary>每帧检测距离，进入范围且冷却完毕时造成伤害。</summary>
    public void Tick(float delta, Combatant attacker, Combatant victim, float interval, float reach)
    {
        _cooldown = Math.Max(_cooldown - delta, 0f); // 冷却剩余时间
        if (attacker.Health.IsDead || victim.Health.IsDead) // 如果攻击者或受害者已死亡则直接返回
        {
            return; // 直接返回
        }

        if (attacker.GlobalPosition.DistanceTo(victim.GlobalPosition) > reach) // 如果攻击者与受害者距离大于攻击范围则直接返回
        {
            return; // 直接返回
        }

        if (_cooldown > 0f) // 如果冷却剩余时间大于0则直接返回
        {
            return; // 直接返回
        }

        DamageSystem.Apply(new DamageRequest // 造成伤害
        {
            Source = attacker, // 攻击者
            Target = victim, // 受害者
            Amount = attacker.Stats.GetValue(StatType.Attack), // 攻击力
            Tags = ["contact", "melee"], // 伤害标签
        });
        GD.Print(attacker);
   
        AttackFlash.Play(
            attacker, 
            attacker.GlobalPosition, 
            victim.GlobalPosition, 
            color: new Color(1f, 0.35f, 0.35f, 0.9f) // 红色闪光
        ); // 播放攻击闪光
        _cooldown = interval; // 重置攻击间隔
    }
}
