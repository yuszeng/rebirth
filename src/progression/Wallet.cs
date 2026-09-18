namespace Rebirth.Progression;

/// <summary>本局金币增减。跨转生持久化仍走 PersistentState，不写在这里。</summary>
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

    /// <summary>扣款。金币不足则失败且不改余额。</summary>
    public bool TrySpend(RunState run, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (run.Gold < amount)
        {
            return false;
        }

        run.Gold -= amount;
        return true;
    }
}
