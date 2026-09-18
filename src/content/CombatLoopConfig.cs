namespace Rebirth.Content;

/// <summary>战斗回合循环参数。一回合打完进入商店，离开后再开下一回合。</summary>
[GlobalClass]
public partial class CombatLoopConfig : Resource
{
    [Export] public float RoundDurationSeconds { get; set; } = 60f; // 每回合战斗时长
}
