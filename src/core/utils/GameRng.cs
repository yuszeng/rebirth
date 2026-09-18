namespace Rebirth.Core;

/// <summary>Roguelike 的唯一随机入口。禁止在业务里直接用 GD.Randf。</summary>
public partial class GameRng : Node
{
    public static GameRng Instance { get; private set; } = null!; // 单例引用

    readonly RandomNumberGenerator _rng = new(); // Godot 随机数生成器
    public ulong SeedValue { get; private set; } // 当前种子（便于调试复现）

    public override void _EnterTree()
    {
        Instance = this;
        Reseed(0);
    }

    /// <summary>重置随机种子。seed 为 0 时使用系统随机种子。</summary>
    public void Reseed(ulong requestedSeed)
    {
        if (requestedSeed == 0)
        {
            _rng.Randomize();
            SeedValue = _rng.Seed;
            return;
        }

        SeedValue = requestedSeed;
        _rng.Seed = requestedSeed;
    }

    public float NextFloat() => _rng.Randf(); // [0, 1) 浮点随机

    public float Range(float from, float to) => _rng.RandfRange(from, to); // 浮点区间

    public int Range(int from, int to) => _rng.RandiRange(from, to); // 整数闭区间

    /// <summary>从列表中均匀随机选取一项。</summary>
    public T Pick<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cannot pick from an empty list.");
        }

        return items[Range(0, items.Count - 1)];
    }

    /// <summary>Fisher-Yates 洗牌，返回新列表（不修改原集合）。</summary>
    public List<T> Shuffle<T>(IEnumerable<T> items)
    {
        var copy = items.ToList(); // 副本，避免修改原数据
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var j = Range(0, i); // 随机交换下标
            (copy[i], copy[j]) = (copy[j], copy[i]);
        }

        return copy;
    }

    /// <summary>按权重随机选取。权重为 0 的项不参与累加。</summary>
    public T WeightedPick<T>(IReadOnlyList<T> items, IReadOnlyList<float> weights)
    {
        if (items.Count == 0 || items.Count != weights.Count)
        {
            throw new InvalidOperationException("Weighted pick requires aligned items and weights.");
        }

        var total = weights.Sum(w => Math.Max(w, 0f)); // 有效权重总和
        if (total <= 0f)
        {
            return Pick(items); // 全部权重无效时退化为均匀随机
        }

        var roll = NextFloat() * total; // 掷骰区间 [0, total)
        var cursor = 0f; // 累加指针
        for (var i = 0; i < items.Count; i++)
        {
            cursor += Math.Max(weights[i], 0f);
            if (roll <= cursor)
            {
                return items[i];
            }
        }

        return items[^1]; // 浮点误差兜底
    }
}
