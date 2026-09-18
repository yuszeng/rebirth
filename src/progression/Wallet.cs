namespace Rebirth.Progression;

/// <summary>本局金币增减。跨转生持久化在 Phase 2 处理。</summary>
public sealed class Wallet
{
    /// <summary>向本局 RunState 增加金币（忽略非正数）。</summary>
    public void Add(RunState run, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        run.Gold += amount;
    }
}
